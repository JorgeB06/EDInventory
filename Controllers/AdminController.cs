using EDInventory.Data;
using EDInventory.Models;
using EDInventory.Models.Entities;
using EDInventory.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Controlador de administración del sistema. Requiere rol <c>Administrador</c>.
    /// Gestiona Empleados, Usuarios, Roles de Sistema, Departamentos internos y Puestos de trabajo.
    /// Todas las acciones de escritura validan el token CSRF.
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>Inicializa el controlador con el contexto de base de datos.</summary>
        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== EMPLEADOS =====================

        /// <summary>Muestra el listado completo de empleados con su puesto y sede.</summary>
        public async Task<IActionResult> Employees()
        {
            var list = await _context.Employees
                .Include(e => e.Role)
                    .ThenInclude(r => r!.Department)
                        .ThenInclude(d => d!.Site)
                .Include(e => e.DocumentTypeEmployee)
                .OrderBy(e => e.EmpSurname).ThenBy(e => e.EmpName)
                .ToListAsync();
            return View(list);
        }

        /// <summary>[GET] Muestra el formulario de creación de un nuevo empleado.</summary>
        public async Task<IActionResult> EmployeeCreate()
        {
            return View(new EmployeeViewModel
            {
                DocumentTypes = await GetDocTypeEmpSelectList(),
                Roles = await GetRoleSelectList()
            });
        }

        /// <summary>[POST] Persiste el nuevo empleado en la base de datos.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeCreate(EmployeeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.DocumentTypes = await GetDocTypeEmpSelectList();
                vm.Roles = await GetRoleSelectList();
                return View(vm);
            }

            _context.Employees.Add(new Employee
            {
                EmpName = vm.EmpName,
                EmpSurname = vm.EmpSurname,
                DoctypeCodeEmp = vm.DoctypeCodeEmp,
                EmpDocnum = vm.EmpDocnum,
                EmpEmail = vm.EmpEmail,
                RoleCode = vm.RoleCode,
                EmpAddata = vm.EmpAddata,
                EmpHighDate = vm.EmpHighDate,
                EmpLeaveDate = vm.EmpLeaveDate,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Empleado creado correctamente.";
            return RedirectToAction(nameof(Employees));
        }

        /// <summary>[GET] Muestra el formulario de edición del empleado indicado por <paramref name="id"/>.</summary>
        public async Task<IActionResult> EmployeeEdit(int id)
        {
            var entity = await _context.Employees.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new EmployeeViewModel
            {
                EmpCode = entity.EmpCode,
                EmpName = entity.EmpName ?? string.Empty,
                EmpSurname = entity.EmpSurname ?? string.Empty,
                DoctypeCodeEmp = entity.DoctypeCodeEmp,
                EmpDocnum = entity.EmpDocnum,
                EmpEmail = entity.EmpEmail,
                RoleCode = entity.RoleCode,
                EmpAddata = entity.EmpAddata,
                EmpHighDate = entity.EmpHighDate,
                EmpLeaveDate = entity.EmpLeaveDate,
                Active = entity.Active,
                DocumentTypes = await GetDocTypeEmpSelectList(),
                Roles = await GetRoleSelectList()
            });
        }

        /// <summary>[POST] Guarda los cambios del empleado en la base de datos.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeEdit(EmployeeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.DocumentTypes = await GetDocTypeEmpSelectList();
                vm.Roles = await GetRoleSelectList();
                return View(vm);
            }

            var entity = await _context.Employees.FindAsync(vm.EmpCode);
            if (entity == null) return NotFound();

            entity.EmpName = vm.EmpName;
            entity.EmpSurname = vm.EmpSurname;
            entity.DoctypeCodeEmp = vm.DoctypeCodeEmp;
            entity.EmpDocnum = vm.EmpDocnum;
            entity.EmpEmail = vm.EmpEmail;
            entity.RoleCode = vm.RoleCode;
            entity.EmpAddata = vm.EmpAddata;
            entity.EmpHighDate = vm.EmpHighDate;
            entity.EmpLeaveDate = vm.EmpLeaveDate;
            entity.Active = vm.Active;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Empleado actualizado correctamente.";
            return RedirectToAction(nameof(Employees));
        }

        /// <summary>[POST] Alterna el estado activo/inactivo del empleado indicado por <paramref name="id"/>.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeToggle(int id)
        {
            var entity = await _context.Employees.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Empleado {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Employees));
        }

        // ===================== ROLES DE SISTEMA (URoles — solo lectura) =====================

        /// <summary>Muestra el catálogo de roles de sistema (solo lectura, la creación se hace desde BD).</summary>
        public async Task<IActionResult> SystemRoles()
        {
            var list = await _context.UserRoles.OrderBy(r => r.UroleDesc).ToListAsync();
            return View(list);
        }

        // ===================== USUARIOS =====================

        /// <summary>Muestra el listado de cuentas de usuario del sistema con su empleado y rol asignados.</summary>
        public async Task<IActionResult> Users()
        {
            var list = await _context.Users
                .Include(u => u.Employee)
                .Include(u => u.UserRole)
                .OrderBy(u => u.UserLogin)
                .ToListAsync();
            return View(list);
        }

        /// <summary>[GET] Muestra el formulario de creación de un nuevo usuario del sistema.</summary>
        public async Task<IActionResult> UserCreate()
        {
            return View(new UserViewModel
            {
                Employees = await GetEmployeeSelectList(),
                UserRoles = await GetUserRoleSelectList()
            });
        }

        /// <summary>[POST] Crea el usuario del sistema. Valida unicidad de login y hashea la contraseña con BCrypt.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCreate(UserViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Password))
                ModelState.AddModelError("Password", "La contrasena es requerida para usuarios nuevos.");

            var exists = await _context.Users.AnyAsync(u => u.UserLogin == vm.UserLogin);
            if (exists)
                ModelState.AddModelError("UserLogin", "Este nombre de usuario ya existe.");

            if (!ModelState.IsValid)
            {
                vm.Employees = await GetEmployeeSelectList();
                vm.UserRoles = await GetUserRoleSelectList();
                return View(vm);
            }

            _context.Users.Add(new User
            {
                UserLogin = vm.UserLogin,
                UserPassword = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                EmpCode = vm.EmpCode,
                UroleCode = vm.UroleCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Usuario creado correctamente.";
            return RedirectToAction(nameof(Users));
        }

        /// <summary>[GET] Muestra el formulario de edición del usuario indicado por <paramref name="id"/>.</summary>
        public async Task<IActionResult> UserEdit(int id)
        {
            var entity = await _context.Users.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new UserViewModel
            {
                UserCode = entity.UserCode,
                UserLogin = entity.UserLogin ?? string.Empty,
                EmpCode = entity.EmpCode,
                UroleCode = entity.UroleCode,
                Active = entity.Active,
                Employees = await GetEmployeeSelectList(),
                UserRoles = await GetUserRoleSelectList()
            });
        }

        /// <summary>[POST] Actualiza el usuario. El campo contraseña es opcional; si se envía, se rehashea con BCrypt.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEdit(UserViewModel vm)
        {
            // Password es opcional en edicion
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            var exists = await _context.Users
                .AnyAsync(u => u.UserLogin == vm.UserLogin && u.UserCode != vm.UserCode);
            if (exists)
                ModelState.AddModelError("UserLogin", "Este nombre de usuario ya existe.");

            if (!ModelState.IsValid)
            {
                vm.Employees = await GetEmployeeSelectList();
                vm.UserRoles = await GetUserRoleSelectList();
                return View(vm);
            }

            var entity = await _context.Users.FindAsync(vm.UserCode);
            if (entity == null) return NotFound();

            entity.UserLogin = vm.UserLogin;
            entity.EmpCode = vm.EmpCode;
            entity.UroleCode = vm.UroleCode;
            entity.Active = vm.Active;

            if (!string.IsNullOrWhiteSpace(vm.Password))
                entity.UserPassword = BCrypt.Net.BCrypt.HashPassword(vm.Password);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Users));
        }

        /// <summary>[POST] Alterna el estado activo/inactivo de la cuenta de usuario indicada por <paramref name="id"/>.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserToggle(int id)
        {
            var entity = await _context.Users.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Usuario {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Users));
        }

        // ===================== HELPERS =====================

        private async Task<IEnumerable<SelectListItem>> GetDocTypeEmpSelectList() =>
            (await _context.DocumentTypeEmployees.Where(d => d.Active).OrderBy(d => d.DoctypeDesEmp).ToListAsync())
            .Select(d => new SelectListItem(d.DoctypeDesEmp, d.DoctypeCodeEmp.ToString()));

        // ===================== DEPARTAMENTOS =====================

        /// <summary>Muestra el listado de departamentos internos con su sede asociada.</summary>
        public async Task<IActionResult> Departments()
        {
            var list = await _context.Departments
                .Include(d => d.Site)
                .OrderBy(d => d.DepName)
                .ToListAsync();
            return View(list);
        }

        /// <summary>[GET] Muestra el formulario de creación de un nuevo departamento interno.</summary>
        public async Task<IActionResult> DepartmentCreate()
        {
            return View(new DepartmentViewModel { Sites = await GetSiteSelectList() });
        }

        /// <summary>[POST] Persiste el nuevo departamento interno en la base de datos.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartmentCreate(DepartmentViewModel vm)
        {
            if (!ModelState.IsValid) { vm.Sites = await GetSiteSelectList(); return View(vm); }
            _context.Departments.Add(new Department { DepName = vm.DepName, SiteCode = vm.SiteCode, Active = vm.Active });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Departamento creado correctamente.";
            return RedirectToAction(nameof(Departments));
        }

        /// <summary>[GET] Muestra el formulario de edición del departamento indicado por <paramref name="id"/>.</summary>
        public async Task<IActionResult> DepartmentEdit(int id)
        {
            var entity = await _context.Departments.FindAsync(id);
            if (entity == null) return NotFound();
            return View(new DepartmentViewModel
            {
                DepCode = entity.DepCode,
                DepName = entity.DepName ?? string.Empty,
                SiteCode = entity.SiteCode,
                Active = entity.Active,
                Sites = await GetSiteSelectList()
            });
        }

        /// <summary>[POST] Guarda los cambios del departamento en la base de datos.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartmentEdit(DepartmentViewModel vm)
        {
            if (!ModelState.IsValid) { vm.Sites = await GetSiteSelectList(); return View(vm); }
            var entity = await _context.Departments.FindAsync(vm.DepCode);
            if (entity == null) return NotFound();
            entity.DepName = vm.DepName;
            entity.SiteCode = vm.SiteCode;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Departamento actualizado correctamente.";
            return RedirectToAction(nameof(Departments));
        }

        /// <summary>[POST] Alterna el estado activo/inactivo del departamento indicado por <paramref name="id"/>.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartmentToggle(int id)
        {
            var entity = await _context.Departments.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Departments));
        }

        // ===================== PUESTOS =====================

        /// <summary>Muestra el listado de puestos de trabajo internos con su departamento y sede.</summary>
        public async Task<IActionResult> Positions()
        {
            var list = await _context.Roles
                .Include(r => r.Department).ThenInclude(d => d!.Site)
                .OrderBy(r => r.RoleName)
                .ToListAsync();
            return View(list);
        }

        /// <summary>[GET] Muestra el formulario de creación de un nuevo puesto de trabajo.</summary>
        public async Task<IActionResult> PositionCreate()
        {
            return View(new PositionViewModel { Departments = await GetDepartmentSelectList() });
        }

        /// <summary>[POST] Persiste el nuevo puesto de trabajo en la base de datos.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PositionCreate(PositionViewModel vm)
        {
            if (!ModelState.IsValid) { vm.Departments = await GetDepartmentSelectList(); return View(vm); }
            _context.Roles.Add(new Role { RoleName = vm.RoleName, DepCode = vm.DepCode, Active = vm.Active });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Puesto creado correctamente.";
            return RedirectToAction(nameof(Positions));
        }

        /// <summary>[GET] Muestra el formulario de edición del puesto indicado por <paramref name="id"/>.</summary>
        public async Task<IActionResult> PositionEdit(int id)
        {
            var entity = await _context.Roles.FindAsync(id);
            if (entity == null) return NotFound();
            return View(new PositionViewModel
            {
                RoleCode = entity.RoleCode,
                RoleName = entity.RoleName ?? string.Empty,
                DepCode = entity.DepCode,
                Active = entity.Active,
                Departments = await GetDepartmentSelectList()
            });
        }

        /// <summary>[POST] Guarda los cambios del puesto de trabajo en la base de datos.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PositionEdit(PositionViewModel vm)
        {
            if (!ModelState.IsValid) { vm.Departments = await GetDepartmentSelectList(); return View(vm); }
            var entity = await _context.Roles.FindAsync(vm.RoleCode);
            if (entity == null) return NotFound();
            entity.RoleName = vm.RoleName;
            entity.DepCode = vm.DepCode;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Puesto actualizado correctamente.";
            return RedirectToAction(nameof(Positions));
        }

        /// <summary>[POST] Alterna el estado activo/inactivo del puesto indicado por <paramref name="id"/>.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PositionToggle(int id)
        {
            var entity = await _context.Roles.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Positions));
        }

        private async Task<IEnumerable<SelectListItem>> GetRoleSelectList() =>
            (await _context.Roles
                .Include(r => r.Department).ThenInclude(d => d!.Site)
                .Where(r => r.Active).ToListAsync())
            .Select(r => new SelectListItem(
                $"{r.RoleName} - {r.Department?.DepName} ({r.Department?.Site?.SiteName})",
                r.RoleCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetSiteSelectList() =>
            (await _context.Sites.Where(s => s.Active).OrderBy(s => s.SiteName).ToListAsync())
            .Select(s => new SelectListItem(s.SiteName, s.SiteCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetDepartmentSelectList() =>
            (await _context.Departments
                .Include(d => d.Site)
                .Where(d => d.Active).OrderBy(d => d.DepName).ToListAsync())
            .Select(d => new SelectListItem(
                $"{d.DepName}{(d.Site != null ? $" ({d.Site.SiteName})" : "")}",
                d.DepCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetEmployeeSelectList() =>
            (await _context.Employees.Where(e => e.Active).OrderBy(e => e.EmpSurname).ToListAsync())
            .Select(e => new SelectListItem($"{e.EmpSurname} {e.EmpName}", e.EmpCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetUserRoleSelectList() =>
            (await _context.UserRoles.Where(r => r.Active).OrderBy(r => r.UroleDesc).ToListAsync())
            .Select(r => new SelectListItem(r.UroleDesc, r.UroleCode.ToString()));
    }
}
