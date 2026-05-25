using EDInventory.Data;
using EDInventory.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Controlador de autenticación. Gestiona el inicio de sesión mediante
    /// cookies (<c>CookieAuth</c>), el cierre de sesión y el cambio de contraseña.
    /// Las contraseñas se verifican y almacenan con hash BCrypt.
    /// Incluye bloqueo temporal de cuenta tras 5 intentos fallidos consecutivos (15 minutos).
    /// </summary>
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache  _cache;

        // Configuración de bloqueo por intentos fallidos
        private const int MaxFailedAttempts   = 5;
        private const int LockoutMinutes      = 15;
        private const int TrackingWindowMinutes = 30;

        private string LockKey(string login) => $"loginlock:{login.ToLowerInvariant()}";

        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="cache">Caché en memoria para rastrear intentos fallidos.</param>
        public AuthController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache   = cache;
        }

        /// <summary>
        /// [GET] Muestra el formulario de inicio de sesión.
        /// Redirige al Dashboard si el usuario ya está autenticado.
        /// </summary>
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }

        /// <summary>
        /// [POST] Procesa el formulario de inicio de sesión.
        /// Verifica las credenciales contra la BD con BCrypt; crea la cookie de autenticación si son correctas.
        /// Bloquea la cuenta durante <c>LockoutMinutes</c> minutos tras <c>MaxFailedAttempts</c> intentos fallidos.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ── Verificar bloqueo activo ──────────────────────────────────
            var lockKey  = LockKey(model.UserLogin);
            var lockInfo = _cache.Get<(int Count, DateTime? LockedUntil)>(lockKey);

            if (lockInfo.LockedUntil.HasValue && lockInfo.LockedUntil.Value > DateTime.UtcNow)
            {
                var remaining = (int)Math.Ceiling((lockInfo.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
                ModelState.AddModelError(string.Empty,
                    $"Cuenta bloqueada por demasiados intentos fallidos. " +
                    $"Intente nuevamente en {remaining} minuto(s).");
                return View(model);
            }

            // ── Verificar credenciales ────────────────────────────────────
            var user = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.UserLogin == model.UserLogin && u.Active);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword))
            {
                // Incrementar contador de fallos
                var newCount   = lockInfo.Count + 1;
                var newLocked  = newCount >= MaxFailedAttempts
                    ? (DateTime?)DateTime.UtcNow.AddMinutes(LockoutMinutes)
                    : lockInfo.LockedUntil;

                _cache.Set(lockKey, (newCount, newLocked),
                    TimeSpan.FromMinutes(TrackingWindowMinutes));

                string errorMsg = newCount >= MaxFailedAttempts
                    ? $"Cuenta bloqueada {LockoutMinutes} minutos por demasiados intentos fallidos."
                    : $"Usuario o contrasena incorrectos. " +
                      $"Quedan {MaxFailedAttempts - newCount} intento(s) antes del bloqueo.";

                ModelState.AddModelError(string.Empty, errorMsg);
                return View(model);
            }

            // ── Login exitoso: limpiar contador y crear cookie ────────────
            _cache.Remove(lockKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserCode.ToString()),
                new(ClaimTypes.Name, user.UserLogin!),
                new("FullName", $"{user.Employee?.EmpName} {user.Employee?.EmpSurname}"),
                new(ClaimTypes.Role, user.UserRole?.UroleDesc ?? "User")
            };

            var identity  = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal);

            return RedirectToAction("Index", "Dashboard");
        }

        /// <summary>[POST] Cierra la sesión del usuario eliminando la cookie de autenticación.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        /// <summary>Muestra la vista de acceso denegado cuando el usuario no tiene el rol necesario.</summary>
        public IActionResult AccessDenied() => View();

        /// <summary>[GET] Muestra el formulario para cambiar la contraseña del usuario autenticado.</summary>
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        /// <summary>
        /// [POST] Procesa el cambio de contraseña del usuario autenticado.
        /// Verifica la contraseña actual con BCrypt antes de guardar el nuevo hash.
        /// </summary>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);

            if (user == null || !BCrypt.Net.BCrypt.Verify(vm.CurrentPassword, user.UserPassword))
            {
                ModelState.AddModelError(nameof(vm.CurrentPassword), "La contrasena actual no es correcta.");
                return View(vm);
            }

            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Contrasena actualizada correctamente.";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
