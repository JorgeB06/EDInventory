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
    /// Controlador de hospitales y su jerarquía clínica. Requiere rol <c>AdmRead</c> como mínimo.
    /// Gestiona: Hospitales, Unidades Programáticas (UP), Departamentos hospitalarios,
    /// Cargos clínicos (Roles) y Contactos clínicos (Doctores).
    /// Las acciones de escritura requieren rol <c>AdmWrite</c> o superior.
    /// </summary>
    [Authorize(Roles = AppRoles.AdmRead)]
    public class HospitalController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>Inicializa el controlador con el contexto de base de datos.</summary>
        public HospitalController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== HOSPITALES =====================

        /// <summary>
        /// Muestra la lista paginada de hospitales con filtros por nombre y UP.
        /// </summary>
        public async Task<IActionResult> Index(string? search, int? puId, int page = 1)
        {
            const int pageSize = 12;

            var query = _context.Hospitals
                .Include(h => h.ProcessingUnit)
                .Include(h => h.HospitalDepartments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(h => h.HospName!.Contains(search) || h.HospAddress!.Contains(search));

            if (puId.HasValue)
                query = query.Where(h => h.PuCode == puId);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            ViewBag.Search     = search;
            ViewBag.PuId       = puId;
            ViewBag.Page       = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total      = total;
            ViewBag.ProcessingUnits = new SelectList(
                await _context.ProcessingUnits.Where(p => p.Active).OrderBy(p => p.PuDesc).ToListAsync(),
                "PuCode", "PuDesc");

            return View(await query
                .OrderBy(h => h.HospName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync());
        }

        /// <summary>[GET] Muestra el formulario de creación de hospital. Requiere rol <c>AdmWrite</c>.</summary>
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Create()
        {
            return View(new HospitalViewModel { ProcessingUnits = await GetPuSelectList() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Create(HospitalViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.ProcessingUnits = await GetPuSelectList();
                return View(vm);
            }

            _context.Hospitals.Add(new Hospital
            {
                HospName = vm.HospName,
                HospAddress = vm.HospAddress,
                PuCode = vm.PuCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Hospital creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.Hospitals.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new HospitalViewModel
            {
                HospCode = entity.HospCode,
                HospName = entity.HospName ?? string.Empty,
                HospAddress = entity.HospAddress,
                PuCode = entity.PuCode,
                Active = entity.Active,
                ProcessingUnits = await GetPuSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> Edit(HospitalViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.ProcessingUnits = await GetPuSelectList();
                return View(vm);
            }

            var entity = await _context.Hospitals.FindAsync(vm.HospCode);
            if (entity == null) return NotFound();

            entity.HospName = vm.HospName;
            entity.HospAddress = vm.HospAddress;
            entity.PuCode = vm.PuCode;
            entity.Active = vm.Active;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Hospital actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> Toggle(int id)
        {
            var entity = await _context.Hospitals.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Hospital {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Muestra el detalle completo del hospital: departamentos, cargos clínicos y contactos.
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            var hospital = await _context.Hospitals
                .Include(h => h.ProcessingUnit)
                .Include(h => h.HospitalDepartments)
                    .ThenInclude(d => d.HospitalRoles)
                        .ThenInclude(r => r.HospitalDoctors)
                .FirstOrDefaultAsync(h => h.HospCode == id);

            if (hospital == null) return NotFound();
            return View(hospital);
        }

        // ===================== UNIDADES DE PROCESO =====================

        public async Task<IActionResult> ProcessingUnits()
        {
            var list = await _context.ProcessingUnits
                .Include(p => p.Hospitals)
                .OrderBy(p => p.PuDesc)
                .ToListAsync();
            return View(list);
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public IActionResult PuCreate() => View(new ProcessingUnitViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> PuCreate(ProcessingUnitViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            _context.ProcessingUnits.Add(new ProcessingUnit
            {
                PuDesc = vm.PuDesc,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Unidad de proceso creada.";
            return RedirectToAction(nameof(ProcessingUnits));
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> PuEdit(int id)
        {
            var entity = await _context.ProcessingUnits.FindAsync(id);
            if (entity == null) return NotFound();
            return View(new ProcessingUnitViewModel
            {
                PuCode = entity.PuCode,
                PuDesc = entity.PuDesc ?? string.Empty,
                Active = entity.Active
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> PuEdit(ProcessingUnitViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var entity = await _context.ProcessingUnits.FindAsync(vm.PuCode);
            if (entity == null) return NotFound();

            entity.PuDesc = vm.PuDesc;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Unidad de proceso actualizada.";
            return RedirectToAction(nameof(ProcessingUnits));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> PuToggle(int id)
        {
            var entity = await _context.ProcessingUnits.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Unidad {(entity.Active ? "activada" : "desactivada")}.";
            return RedirectToAction(nameof(ProcessingUnits));
        }

        // ===================== DEPARTAMENTOS DE HOSPITAL =====================

        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> DepCreate(int hospId)
        {
            return View(new HospitalDepartmentViewModel
            {
                HospCode = hospId,
                Hospitals = await GetHospitalSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> DepCreate(HospitalDepartmentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Hospitals = await GetHospitalSelectList();
                return View(vm);
            }

            _context.HospitalDepartments.Add(new HospitalDepartment
            {
                HospDepName = vm.HospDepName,
                HospCode = vm.HospCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Departamento creado correctamente.";
            return RedirectToAction(nameof(Detail), new { id = vm.HospCode });
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> DepEdit(int id)
        {
            var entity = await _context.HospitalDepartments.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new HospitalDepartmentViewModel
            {
                HospDepCode = entity.HospDepCode,
                HospDepName = entity.HospDepName ?? string.Empty,
                HospCode = entity.HospCode,
                Active = entity.Active,
                Hospitals = await GetHospitalSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> DepEdit(HospitalDepartmentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Hospitals = await GetHospitalSelectList();
                return View(vm);
            }

            var entity = await _context.HospitalDepartments.FindAsync(vm.HospDepCode);
            if (entity == null) return NotFound();

            entity.HospDepName = vm.HospDepName;
            entity.HospCode = vm.HospCode;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Departamento actualizado.";
            return RedirectToAction(nameof(Detail), new { id = vm.HospCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> DepToggle(int id)
        {
            var entity = await _context.HospitalDepartments.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Departamento {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Detail), new { id = entity.HospCode });
        }

        // ===================== ROLES DE HOSPITAL =====================

        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> RoleCreate(int depId)
        {
            return View(new HospitalRoleViewModel
            {
                HospDepCode = depId,
                HospitalDepartments = await GetHospDepSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> RoleCreate(HospitalRoleViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.HospitalDepartments = await GetHospDepSelectList();
                return View(vm);
            }

            _context.HospitalRoles.Add(new HospitalRole
            {
                HospRoleName = vm.HospRoleName,
                HospDepCode = vm.HospDepCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cargo creado correctamente.";

            var dep = await _context.HospitalDepartments.FindAsync(vm.HospDepCode);
            return RedirectToAction(nameof(Detail), new { id = dep?.HospCode });
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> RoleEdit(int id)
        {
            var entity = await _context.HospitalRoles.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new HospitalRoleViewModel
            {
                HospRoleCode = entity.HospRoleCode,
                HospRoleName = entity.HospRoleName ?? string.Empty,
                HospDepCode = entity.HospDepCode,
                Active = entity.Active,
                HospitalDepartments = await GetHospDepSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> RoleEdit(HospitalRoleViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.HospitalDepartments = await GetHospDepSelectList();
                return View(vm);
            }

            var entity = await _context.HospitalRoles.FindAsync(vm.HospRoleCode);
            if (entity == null) return NotFound();

            entity.HospRoleName = vm.HospRoleName;
            entity.HospDepCode = vm.HospDepCode;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Cargo actualizado.";

            var dep = await _context.HospitalDepartments.FindAsync(vm.HospDepCode);
            return RedirectToAction(nameof(Detail), new { id = dep?.HospCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> RoleToggle(int id)
        {
            var entity = await _context.HospitalRoles
                .Include(r => r.HospitalDepartment)
                .FirstOrDefaultAsync(r => r.HospRoleCode == id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Cargo {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Detail), new { id = entity.HospitalDepartment?.HospCode });
        }

        // ===================== CONTACTOS (DOCTORES) =====================

        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> DocCreate(int roleId)
        {
            return View(new HospitalDoctorViewModel
            {
                HospRoleCode = roleId,
                HospitalRoles = await GetHospRoleSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> DocCreate(HospitalDoctorViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.HospitalRoles = await GetHospRoleSelectList();
                return View(vm);
            }

            _context.HospitalDoctors.Add(new HospitalDoctor
            {
                HospDocName = vm.HospDocName,
                HospDocSurname = vm.HospDocSurname,
                HospDocMail = vm.HospDocMail,
                HospRoleCode = vm.HospRoleCode,
                HospDocAddata = vm.HospDocAddata,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Contacto creado correctamente.";

            var role = await _context.HospitalRoles
                .Include(r => r.HospitalDepartment)
                .FirstOrDefaultAsync(r => r.HospRoleCode == vm.HospRoleCode);
            return RedirectToAction(nameof(Detail), new { id = role?.HospitalDepartment?.HospCode });
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> DocEdit(int id)
        {
            var entity = await _context.HospitalDoctors.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new HospitalDoctorViewModel
            {
                HospDocCode = entity.HospDocCode,
                HospDocName = entity.HospDocName ?? string.Empty,
                HospDocSurname = entity.HospDocSurname ?? string.Empty,
                HospDocMail = entity.HospDocMail,
                HospRoleCode = entity.HospRoleCode,
                HospDocAddata = entity.HospDocAddata,
                Active = entity.Active,
                HospitalRoles = await GetHospRoleSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> DocEdit(HospitalDoctorViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.HospitalRoles = await GetHospRoleSelectList();
                return View(vm);
            }

            var entity = await _context.HospitalDoctors.FindAsync(vm.HospDocCode);
            if (entity == null) return NotFound();

            entity.HospDocName = vm.HospDocName;
            entity.HospDocSurname = vm.HospDocSurname;
            entity.HospDocMail = vm.HospDocMail;
            entity.HospRoleCode = vm.HospRoleCode;
            entity.HospDocAddata = vm.HospDocAddata;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Contacto actualizado.";

            var role = await _context.HospitalRoles
                .Include(r => r.HospitalDepartment)
                .FirstOrDefaultAsync(r => r.HospRoleCode == vm.HospRoleCode);
            return RedirectToAction(nameof(Detail), new { id = role?.HospitalDepartment?.HospCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> DocToggle(int id)
        {
            var entity = await _context.HospitalDoctors
                .Include(d => d.HospitalRole)
                    .ThenInclude(r => r!.HospitalDepartment)
                .FirstOrDefaultAsync(d => d.HospDocCode == id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Contacto {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Detail),
                new { id = entity.HospitalRole?.HospitalDepartment?.HospCode });
        }

        // ===================== HELPERS =====================

        private async Task<IEnumerable<SelectListItem>> GetPuSelectList() =>
            (await _context.ProcessingUnits.Where(p => p.Active).OrderBy(p => p.PuDesc).ToListAsync())
            .Select(p => new SelectListItem(p.PuDesc, p.PuCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetHospitalSelectList() =>
            (await _context.Hospitals.Where(h => h.Active).OrderBy(h => h.HospName).ToListAsync())
            .Select(h => new SelectListItem(h.HospName, h.HospCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetHospDepSelectList() =>
            (await _context.HospitalDepartments
                .Include(d => d.Hospital)
                .Where(d => d.Active).ToListAsync())
            .Select(d => new SelectListItem($"{d.HospDepName} - {d.Hospital?.HospName}", d.HospDepCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetHospRoleSelectList() =>
            (await _context.HospitalRoles
                .Include(r => r.HospitalDepartment).ThenInclude(d => d!.Hospital)
                .Where(r => r.Active).ToListAsync())
            .Select(r => new SelectListItem(
                $"{r.HospRoleName} - {r.HospitalDepartment?.HospDepName} ({r.HospitalDepartment?.Hospital?.HospName})",
                r.HospRoleCode.ToString()));
    }
}
