using EDInventory.Data;
using EDInventory.Models;
using EDInventory.Models.Entities;
using EDInventory.Models.ViewModels;
using EDInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Módulo móvil (celular) del sistema, pensado para usarse en bodega. Permite ejecutar
    /// el conteo físico de una auditoría de inventario asignada (escaneando cajas con la
    /// cámara) y hacer retiros/devoluciones rápidas de repuestos. La pistola USB fija de
    /// bodega sigue usando <c>Engineering/ScanBox</c> sin cambios; este módulo es un
    /// complemento pensado para el celular.
    /// </summary>
    [Authorize(Roles = AppRoles.SvcRead)]
    public class MovilController : Controller
    {
        private readonly AppDbContext _context;

        public MovilController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== HOME =====================

        public IActionResult Index() => View();

        // ===================== MIS AUDITORÍAS =====================

        public async Task<IActionResult> MisAuditorias()
        {
            var isManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.SvcAdmin);

            var query = _context.EngAuditSessions
                .Include(s => s.Line)
                .Include(s => s.Box).ThenInclude(b => b!.Line)
                .Where(s => s.Status != "CERRADA" && s.Status != "CANCELADA")
                .AsQueryable();

            if (!isManage)
            {
                var uid = GetCurrentUserId();
                query = query.Where(s => s.AssignedUserCode == uid);
            }

            var sessions = await query
                .OrderBy(s => s.Status)
                .ThenByDescending(s => s.CreatedDate)
                .ToListAsync();

            return View(sessions);
        }

        public async Task<IActionResult> Auditoria(int id)
        {
            var session = await _context.EngAuditSessions
                .Include(s => s.Line)
                .Include(s => s.Box).ThenInclude(b => b!.Line)
                .Include(s => s.AuditBoxes).ThenInclude(ab => ab.Box).ThenInclude(b => b!.Line)
                .Include(s => s.AuditBoxes).ThenInclude(ab => ab.Box).ThenInclude(b => b!.Parts)
                .Include(s => s.AuditBoxes).ThenInclude(ab => ab.AuditParts).ThenInclude(ap => ap.Part)
                .FirstOrDefaultAsync(s => s.AuditCode == id);
            if (session == null) return NotFound();
            if (!CanAccessSession(session)) return Forbid();

            return View(session);
        }

        // ===================== ESCANEO =====================

        public IActionResult Scan(int? auditCode)
        {
            ViewBag.AuditCode = auditCode;
            return View();
        }

        public async Task<IActionResult> BoxByScan(string code, int? auditCode)
        {
            var box = await EngBoxScanResolver.ResolveAsync(_context, code);
            if (box == null)
            {
                TempData["ScanError"] = $"Código no reconocido: \"{code?.Trim()}\"";
                return RedirectToAction(nameof(Scan), new { auditCode });
            }

            if (auditCode.HasValue)
            {
                var session = await _context.EngAuditSessions.FindAsync(auditCode.Value);
                if (session == null) return NotFound();
                if (!CanAccessSession(session)) return Forbid();

                var auditBox = await _context.EngAuditBoxes
                    .FirstOrDefaultAsync(ab => ab.AuditCode == auditCode.Value && ab.BoxCode == box.BoxCode);
                if (auditBox == null)
                {
                    TempData["ScanError"] = "Esta caja no pertenece a la auditoría seleccionada.";
                    return RedirectToAction(nameof(Auditoria), new { id = auditCode });
                }

                return RedirectToAction(nameof(ContarCaja), new { auditBoxCode = auditBox.AuditBoxCode });
            }

            return RedirectToAction(nameof(RetiroRapido), new { boxCode = box.BoxCode });
        }

        // ===================== CONTEO DE CAJA =====================

        public async Task<IActionResult> ContarCaja(int auditBoxCode)
        {
            var auditBox = await _context.EngAuditBoxes
                .Include(ab => ab.Session)
                .Include(ab => ab.Box).ThenInclude(b => b!.Line)
                .Include(ab => ab.Box).ThenInclude(b => b!.Parts)
                .FirstOrDefaultAsync(ab => ab.AuditBoxCode == auditBoxCode);

            if (auditBox == null || auditBox.Session == null || auditBox.Box == null) return NotFound();
            if (!CanAccessSession(auditBox.Session)) return Forbid();

            if (auditBox.Session.Status == "ASIGNADA")
            {
                auditBox.Session.Status = "EN_PROGRESO";
                auditBox.Session.StartedDate = DateTime.Now;
            }

            // Crear perezosamente las filas de conteo (snapshot fresco de PartQty al momento del conteo)
            var existingPartCodes = await _context.EngAuditParts
                .Where(ap => ap.AuditBoxCode == auditBoxCode)
                .Select(ap => ap.PartCode)
                .ToListAsync();

            foreach (var part in auditBox.Box.Parts)
            {
                if (!existingPartCodes.Contains(part.PartCode))
                {
                    _context.EngAuditParts.Add(new EngAuditPart
                    {
                        AuditBoxCode     = auditBoxCode,
                        PartCode         = part.PartCode,
                        SystemQty        = part.PartQty,
                        ResolutionStatus = "PENDIENTE"
                    });
                }
            }
            await _context.SaveChangesAsync();

            var auditParts = await _context.EngAuditParts
                .Include(ap => ap.Part)
                .Where(ap => ap.AuditBoxCode == auditBoxCode)
                .OrderBy(ap => ap.Part!.PartName)
                .ToListAsync();

            var vm = new ContarCajaVM
            {
                AuditBoxCode = auditBoxCode,
                AuditCode    = auditBox.AuditCode,
                LineName     = auditBox.Box.Line?.LineName,
                BoxNumber    = auditBox.Box.BoxNumber,
                Parts = auditParts.Select(ap => new ContarCajaPartRow
                {
                    AuditPartCode = ap.AuditPartCode,
                    PartCode      = ap.PartCode,
                    PartName      = ap.Part?.PartName,
                    PartRef       = ap.Part?.PartRef,
                    SystemQty     = ap.SystemQty,
                    CountedQty    = ap.CountedQty
                }).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContarCaja(ContarCajaVM vm)
        {
            var auditBox = await _context.EngAuditBoxes
                .Include(ab => ab.Session)
                .FirstOrDefaultAsync(ab => ab.AuditBoxCode == vm.AuditBoxCode);
            if (auditBox == null || auditBox.Session == null) return NotFound();
            if (!CanAccessSession(auditBox.Session)) return Forbid();

            if (vm.Parts.Any(p => !p.CountedQty.HasValue))
            {
                ModelState.AddModelError("", "Ingrese la cantidad contada de todos los repuestos antes de guardar.");
                return View(vm);
            }

            foreach (var row in vm.Parts)
            {
                var ap = await _context.EngAuditParts.FirstOrDefaultAsync(a => a.AuditPartCode == row.AuditPartCode);
                if (ap == null) continue;
                ap.CountedQty       = row.CountedQty!.Value;
                ap.Variance         = row.CountedQty.Value - ap.SystemQty;
                ap.ResolutionStatus = ap.Variance == 0 ? "SIN_DIFERENCIA" : "PENDIENTE";
            }

            auditBox.Status            = "CONTADA";
            auditBox.CountedDate       = DateTime.Now;
            auditBox.CountedByUserCode = GetCurrentUserId();
            await _context.SaveChangesAsync();

            // Si ya no quedan cajas pendientes, la sesión pasa a revisión de discrepancias
            var stillPending = await _context.EngAuditBoxes
                .AnyAsync(ab => ab.AuditCode == auditBox.AuditCode && ab.Status != "CONTADA");
            if (!stillPending)
            {
                auditBox.Session.Status        = "PENDIENTE_APROBACION";
                auditBox.Session.CompletedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Conteo registrado correctamente.";
            return RedirectToAction(nameof(Auditoria), new { id = auditBox.AuditCode });
        }

        // ===================== RETIRO RÁPIDO =====================

        public async Task<IActionResult> RetiroRapido(int boxCode)
        {
            var box = await _context.EngBoxes
                .Include(b => b.Line)
                .Include(b => b.Parts)
                .FirstOrDefaultAsync(b => b.BoxCode == boxCode);
            if (box == null) return NotFound();
            return View(box);
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> RetiroRapidoPart(int partCode)
        {
            var part = await _context.EngParts.Include(p => p.Box).FirstOrDefaultAsync(p => p.PartCode == partCode);
            if (part == null) return NotFound();

            var vm = new MovilWithdrawVM
            {
                PartCode   = part.PartCode,
                BoxCode    = part.BoxCode,
                PartName   = part.PartName,
                PartRef    = part.PartRef,
                CurrentQty = part.PartQty,
                Hospitals  = await GetHospitalSelectList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> RetiroRapidoPart(MovilWithdrawVM vm)
        {
            var part = await _context.EngParts.Include(p => p.Box).FirstOrDefaultAsync(p => p.PartCode == vm.PartCode);
            if (part == null) return NotFound();

            vm.CurrentQty = part.PartQty;
            vm.PartName   = part.PartName;
            vm.PartRef    = part.PartRef;
            vm.BoxCode    = part.BoxCode;

            if (vm.MovType == "RETIRO" && vm.Qty > part.PartQty)
                ModelState.AddModelError(nameof(vm.Qty), $"Stock insuficiente. Disponible: {part.PartQty}");

            if (!ModelState.IsValid)
            {
                vm.Hospitals = await GetHospitalSelectList();
                return View(vm);
            }

            int resultingQty = vm.MovType == "RETIRO" ? part.PartQty - vm.Qty : part.PartQty + vm.Qty;
            bool isLowStock  = vm.MovType == "RETIRO" && resultingQty <= 2;

            if (isLowStock && !vm.Confirmado)
            {
                ViewBag.NeedsConfirm = true;
                ViewBag.ResultingQty = resultingQty;
                vm.Hospitals = await GetHospitalSelectList();
                return View(vm);
            }

            await PartMovementService.RegisterMovementAsync(
                _context, part, vm.MovType, vm.Qty, vm.HospCode, vm.Notes, GetCurrentUserId());

            var tipoLabel = vm.MovType == "RETIRO" ? "Retiro" : "Devolución";
            TempData["Success"] = $"{tipoLabel} registrado. Nuevo stock: {part.PartQty}";
            return RedirectToAction(nameof(RetiroRapido), new { boxCode = part.BoxCode });
        }

        // ===================== HELPERS =====================

        private bool CanAccessSession(EngAuditSession session)
        {
            if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.SvcAdmin)) return true;
            return session.AssignedUserCode == GetCurrentUserId();
        }

        private async Task<IEnumerable<SelectListItem>> GetHospitalSelectList() =>
            await _context.Hospitals
                .Where(h => h.Active).OrderBy(h => h.HospName)
                .Select(h => new SelectListItem(h.HospName, h.HospCode.ToString()))
                .ToListAsync();

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
