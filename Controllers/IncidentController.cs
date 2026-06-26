using EDInventory.Data;
using EDInventory.Models;
using EDInventory.Models.Entities;
using EDInventory.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Gestión de incidentes y reparaciones para equipos IT y activos clínicos.
    /// </summary>
    [Authorize(Roles = AppRoles.AdmRead)]
    public class IncidentController : Controller
    {
        private readonly AppDbContext _context;

        public IncidentController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        public async Task<IActionResult> Index(string? entityType, string? status, string? priority, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.Incidents
                .Include(i => i.ItEquip)
                .Include(i => i.EngAsset)
                .Include(i => i.Reporter).ThenInclude(u => u!.Employee)
                .Include(i => i.Assignee).ThenInclude(u => u!.Employee)
                .AsQueryable();

            if (!string.IsNullOrEmpty(entityType)) query = query.Where(i => i.EntityType == entityType);
            if (!string.IsNullOrEmpty(status))     query = query.Where(i => i.IncidentStatus == status);
            if (!string.IsNullOrEmpty(priority))   query = query.Where(i => i.IncidentPriority == priority);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            ViewBag.EntityType  = entityType;
            ViewBag.Status      = status;
            ViewBag.Priority    = priority;
            ViewBag.Page        = page;
            ViewBag.TotalPages  = totalPages;
            ViewBag.Total       = total;

            var items = await query
                .OrderByDescending(i => i.IncidentDate)
                .ThenByDescending(i => i.IncidentCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var incident = await _context.Incidents
                .Include(i => i.ItEquip).ThenInclude(e => e!.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                .Include(i => i.ItEquip).ThenInclude(e => e!.Hospital)
                .Include(i => i.EngAsset).ThenInclude(a => a!.Line)
                .Include(i => i.EngAsset).ThenInclude(a => a!.Hospital)
                .Include(i => i.Reporter).ThenInclude(u => u!.Employee)
                .Include(i => i.Assignee).ThenInclude(u => u!.Employee)
                .FirstOrDefaultAsync(i => i.IncidentCode == id);

            if (incident == null) return NotFound();
            return View(incident);
        }

        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Create(string? entityType, int? entityCode)
        {
            var vm = new IncidentViewModel
            {
                EntityType    = entityType ?? "TI",
                ItequipCode   = entityType == "TI"  ? entityCode : null,
                AssetCode     = entityType == "SVC" ? entityCode : null,
                ReportedUser  = GetCurrentUserId(),
                IncidentDate  = DateOnly.FromDateTime(DateTime.Today),
            };
            await PopulateSelectLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Create(IncidentViewModel vm)
        {
            if (vm.EntityType == "TI"  && vm.ItequipCode == null) ModelState.AddModelError(nameof(vm.ItequipCode), "Seleccione un equipo IT.");
            if (vm.EntityType == "SVC" && vm.AssetCode   == null) ModelState.AddModelError(nameof(vm.AssetCode),  "Seleccione un activo clinico.");

            if (!ModelState.IsValid)
            {
                await PopulateSelectLists(vm);
                return View(vm);
            }

            var incident = new Incident
            {
                EntityType       = vm.EntityType,
                ItequipCode      = vm.EntityType == "TI"  ? vm.ItequipCode : null,
                AssetCode        = vm.EntityType == "SVC" ? vm.AssetCode   : null,
                IncidentType     = vm.IncidentType,
                IncidentPriority = vm.IncidentPriority,
                IncidentStatus   = "ABIERTA",
                IncidentTitle    = vm.IncidentTitle,
                IncidentDesc     = vm.IncidentDesc,
                ReportedUser     = vm.ReportedUser,
                AssignedUser     = vm.AssignedUser,
                IncidentDate     = vm.IncidentDate,
                IncidentNotes    = vm.IncidentNotes,
            };

            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Incidente registrado correctamente.";
            return RedirectToAction(nameof(Detail), new { id = incident.IncidentCode });
        }

        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Edit(int id)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) return NotFound();
            if (incident.IncidentStatus == "CERRADA" || incident.IncidentStatus == "CANCELADA")
            {
                TempData["Error"] = "No se puede editar un incidente cerrado o cancelado.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var vm = new IncidentViewModel
            {
                IncidentCode     = incident.IncidentCode,
                EntityType       = incident.EntityType,
                ItequipCode      = incident.ItequipCode,
                AssetCode        = incident.AssetCode,
                IncidentType     = incident.IncidentType,
                IncidentPriority = incident.IncidentPriority,
                IncidentStatus   = incident.IncidentStatus,
                IncidentTitle    = incident.IncidentTitle,
                IncidentDesc     = incident.IncidentDesc,
                IncidentResolution = incident.IncidentResolution,
                ReportedUser     = incident.ReportedUser,
                AssignedUser     = incident.AssignedUser,
                IncidentDate     = incident.IncidentDate,
                IncidentNotes    = incident.IncidentNotes,
            };
            await PopulateSelectLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Edit(IncidentViewModel vm)
        {
            var incident = await _context.Incidents.FindAsync(vm.IncidentCode);
            if (incident == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateSelectLists(vm);
                return View(vm);
            }

            incident.IncidentType     = vm.IncidentType;
            incident.IncidentPriority = vm.IncidentPriority;
            incident.IncidentStatus   = vm.IncidentStatus;
            incident.IncidentTitle    = vm.IncidentTitle;
            incident.IncidentDesc     = vm.IncidentDesc;
            incident.IncidentResolution = vm.IncidentResolution;
            incident.AssignedUser     = vm.AssignedUser;
            incident.IncidentNotes    = vm.IncidentNotes;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Incidente actualizado.";
            return RedirectToAction(nameof(Detail), new { id = incident.IncidentCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Close(int id, string? resolution)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) return NotFound();

            incident.IncidentStatus    = "CERRADA";
            incident.IncidentResolution = resolution;
            incident.ClosedDate        = DateOnly.FromDateTime(DateTime.Today);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Incidente cerrado.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Cancel(int id, string? notes)
        {
            var incident = await _context.Incidents.FindAsync(id);
            if (incident == null) return NotFound();

            incident.IncidentStatus = "CANCELADA";
            incident.ClosedDate     = DateOnly.FromDateTime(DateTime.Today);
            if (!string.IsNullOrWhiteSpace(notes))
                incident.IncidentNotes = notes;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Incidente cancelado.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        private async Task PopulateSelectLists(IncidentViewModel vm)
        {
            vm.ItEquips = (await _context.ItEquips
                .Where(e => e.Active)
                .Include(e => e.Model).ThenInclude(m => m!.Brand)
                .OrderBy(e => e.ItequipDesc)
                .ToListAsync())
                .Select(e => new SelectListItem(
                    $"{e.ItequipDesc} ({e.ItequipSn ?? e.ItequipNum ?? "#" + e.ItequipCode})",
                    e.ItequipCode.ToString()));

            vm.EngAssets = (await _context.EngAssets
                .Where(a => a.Active)
                .Include(a => a.Line)
                .OrderBy(a => a.Line!.LineName).ThenBy(a => a.AssetDesc)
                .ToListAsync())
                .Select(a => new SelectListItem(
                    $"{a.AssetDesc} ({a.Line?.LineName ?? "—"})",
                    a.AssetCode.ToString()));

            vm.Users = (await _context.Users
                .Where(u => u.Active)
                .Include(u => u.Employee)
                .OrderBy(u => u.Employee!.EmpName)
                .ToListAsync())
                .Select(u => new SelectListItem(
                    u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                    u.UserCode.ToString()));
        }
    }
}
