using EDInventory.Data;
using EDInventory.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Controlador de autenticación. Gestiona el inicio de sesión mediante
    /// cookies (<c>CookieAuth</c>), el cierre de sesión y el cambio de contraseña.
    /// Las contraseñas se verifican y almacenan con hash BCrypt.
    /// </summary>
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>Inicializa el controlador con el contexto de base de datos.</summary>
        public AuthController(AppDbContext context)
        {
            _context = context;
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
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.UserLogin == model.UserLogin && u.Active);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.UserPassword))
            {
                ModelState.AddModelError(string.Empty, "Usuario o contrasena incorrectos.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserCode.ToString()),
                new(ClaimTypes.Name, user.UserLogin!),
                new("FullName", $"{user.Employee?.EmpName} {user.Employee?.EmpSurname}"),
                new(ClaimTypes.Role, user.UserRole?.UroleDesc ?? "User")
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
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
