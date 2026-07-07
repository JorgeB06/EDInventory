using EDInventory.Data;
using EDInventory.Models;
using EDInventory.Models.Entities;
using EDInventory.Models.ViewModels;
using EDInventory.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Auditoría de inventario (conteo físico de repuestos de Servicio). Un admin
    /// (<c>SvcManage</c>) crea la sesión, elige el alcance (caja o línea completa) y la
    /// asigna a un usuario específico. El conteo en sí se ejecuta desde el módulo móvil
    /// (<see cref="MovilController"/>). Las discrepancias siempre requieren aprobación
    /// de un admin antes de tocar el stock (<see cref="EngPart.PartQty"/>).
    /// </summary>
    [Authorize(Roles = AppRoles.SvcRead)]
    public class InventoryAuditController : Controller
    {
        private readonly AppDbContext _context;

        public InventoryAuditController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== LISTADO =====================

        public async Task<IActionResult> Index(string? status)
        {
            var isManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.SvcAdmin);

            var query = _context.EngAuditSessions
                .Include(s => s.Line)
                .Include(s => s.Box).ThenInclude(b => b!.Line)
                .Include(s => s.AssignedUser).ThenInclude(u => u!.Employee)
                .Include(s => s.CreatedByUser).ThenInclude(u => u!.Employee)
                .AsQueryable();

            if (!isManage)
            {
                var currentUserId = GetCurrentUserId();
                query = query.Where(s => s.AssignedUserCode == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(s => s.Status == status);

            var sessions = await query.OrderByDescending(s => s.CreatedDate).ToListAsync();

            ViewBag.Status    = status;
            ViewBag.IsManage  = isManage;
            return View(sessions);
        }

        // ===================== CREAR =====================

        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Create()
        {
            var vm = new AuditSessionCreateVM();
            await PopulateCreateLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Create(AuditSessionCreateVM vm)
        {
            if (vm.AuditScope == "LINEA" && !vm.LineCode.HasValue)
                ModelState.AddModelError(nameof(vm.LineCode), "Seleccione la línea a auditar.");
            if (vm.AuditScope == "CAJA" && !vm.BoxCode.HasValue)
                ModelState.AddModelError(nameof(vm.BoxCode), "Seleccione la caja a auditar.");

            List<int> boxCodes = [];
            if (ModelState.IsValid && vm.AuditScope == "LINEA")
            {
                boxCodes = await _context.EngBoxes
                    .Where(b => b.LineCode == vm.LineCode && b.Active)
                    .Select(b => b.BoxCode)
                    .ToListAsync();
                if (boxCodes.Count == 0)
                    ModelState.AddModelError(nameof(vm.LineCode), "La línea seleccionada no tiene cajas activas.");
            }
            else if (ModelState.IsValid)
            {
                boxCodes = [vm.BoxCode!.Value];
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateLists(vm);
                return View(vm);
            }

            var session = new EngAuditSession
            {
                AuditScope        = vm.AuditScope,
                LineCode          = vm.AuditScope == "LINEA" ? vm.LineCode : null,
                BoxCode           = vm.AuditScope == "CAJA" ? vm.BoxCode : null,
                AssignedUserCode  = vm.AssignedUserCode!.Value,
                CreatedByUserCode = GetCurrentUserId()!.Value,
                CreatedDate       = DateTime.Now,
                Status            = "ASIGNADA",
                Notes             = vm.Notes
            };
            _context.EngAuditSessions.Add(session);
            await _context.SaveChangesAsync();

            foreach (var bc in boxCodes)
                _context.EngAuditBoxes.Add(new EngAuditBox { AuditCode = session.AuditCode, BoxCode = bc });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Auditoría creada y asignada correctamente ({boxCodes.Count} caja(s)).";
            return RedirectToAction(nameof(Detail), new { id = session.AuditCode });
        }

        private async Task PopulateCreateLists(AuditSessionCreateVM vm)
        {
            vm.Lines = await _context.EngLines
                .Where(l => l.Active).OrderBy(l => l.LineName)
                .Select(l => new SelectListItem(l.LineName, l.LineCode.ToString()))
                .ToListAsync();

            vm.Boxes = (await _context.EngBoxes
                    .Include(b => b.Line).Where(b => b.Active)
                    .OrderBy(b => b.Line!.LineName).ThenBy(b => b.BoxNumber)
                    .ToListAsync())
                .Select(b => new SelectListItem($"{b.Line?.LineName} — Caja {b.BoxNumber}", b.BoxCode.ToString()));

            vm.Users = await GetUserSelectList();
        }

        // ===================== DETALLE / PROGRESO =====================

        public async Task<IActionResult> Detail(int id)
        {
            var session = await LoadSessionWithBoxes(id);
            if (session == null) return NotFound();
            if (!CanAccessSession(session)) return Forbid();

            ViewBag.IsManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.SvcAdmin);
            return View(session);
        }

        // ===================== REVISIÓN DE DISCREPANCIAS =====================

        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Review(int id)
        {
            var session = await LoadSessionWithBoxes(id);
            if (session == null) return NotFound();

            var auditBoxCodes = session.AuditBoxes.Select(ab => ab.AuditBoxCode).ToList();
            var auditParts = await _context.EngAuditParts
                .Include(ap => ap.Part)
                .Include(ap => ap.AuditBox).ThenInclude(ab => ab!.Box)
                .Where(ap => auditBoxCodes.Contains(ap.AuditBoxCode) && ap.CountedQty != null)
                .OrderBy(ap => ap.AuditBox!.Box!.BoxNumber).ThenBy(ap => ap.Part!.PartName)
                .ToListAsync();

            ViewBag.Session = session;
            return View(auditParts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> ApproveAdjustment(int auditPartCode)
        {
            var ap = await _context.EngAuditParts
                .Include(a => a.Part)
                .Include(a => a.AuditBox)
                .FirstOrDefaultAsync(a => a.AuditPartCode == auditPartCode);
            if (ap == null || ap.Part == null || ap.AuditBox == null) return NotFound();

            if (ap.ResolutionStatus != "PENDIENTE")
            {
                TempData["Error"] = "Esta discrepancia ya fue resuelta.";
                return RedirectToAction(nameof(Review), new { id = ap.AuditBox.AuditCode });
            }

            var movement = await PartMovementService.RegisterAdjustmentAsync(
                _context, ap.Part, ap.Variance ?? 0,
                $"Ajuste por auditoría #{ap.AuditBox.AuditCode}", GetCurrentUserId());

            ap.ResolutionStatus = "APROBADO_AJUSTE";
            ap.MovCode = movement.MovCode;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ajuste aprobado y aplicado al stock.";
            return RedirectToAction(nameof(Review), new { id = ap.AuditBox.AuditCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> RejectAdjustment(int auditPartCode, string? notes)
        {
            var ap = await _context.EngAuditParts
                .Include(a => a.AuditBox)
                .FirstOrDefaultAsync(a => a.AuditPartCode == auditPartCode);
            if (ap == null || ap.AuditBox == null) return NotFound();

            ap.ResolutionStatus = "RECHAZADO";
            ap.ResolutionNotes  = notes;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Discrepancia descartada sin ajustar el stock.";
            return RedirectToAction(nameof(Review), new { id = ap.AuditBox.AuditCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> ApproveAllPending(int auditCode)
        {
            var auditBoxCodes = await _context.EngAuditBoxes
                .Where(ab => ab.AuditCode == auditCode)
                .Select(ab => ab.AuditBoxCode)
                .ToListAsync();

            var pending = await _context.EngAuditParts
                .Include(ap => ap.Part)
                .Where(ap => auditBoxCodes.Contains(ap.AuditBoxCode) && ap.ResolutionStatus == "PENDIENTE")
                .ToListAsync();

            var userId = GetCurrentUserId();
            foreach (var ap in pending)
            {
                if (ap.Part == null) continue;
                var movement = await PartMovementService.RegisterAdjustmentAsync(
                    _context, ap.Part, ap.Variance ?? 0, $"Ajuste por auditoría #{auditCode}", userId);
                ap.ResolutionStatus = "APROBADO_AJUSTE";
                ap.MovCode = movement.MovCode;
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{pending.Count} ajuste(s) aprobados y aplicados.";
            return RedirectToAction(nameof(Review), new { id = auditCode });
        }

        // ===================== EXPORTACIÓN PDF =====================

        /// <summary>Exporta el reporte de conteo de una sesión de auditoría (por caja/repuesto) con su resultado.</summary>
        public async Task<IActionResult> ExportPdf(int id)
        {
            var session = await LoadSessionWithBoxes(id);
            if (session == null) return NotFound();
            if (!CanAccessSession(session)) return Forbid();

            QuestPDF.Settings.License = LicenseType.Community;

            var auditBoxCodes = session.AuditBoxes.Select(ab => ab.AuditBoxCode).ToList();
            var auditParts = await _context.EngAuditParts
                .Include(ap => ap.Part)
                .Include(ap => ap.AuditBox).ThenInclude(ab => ab!.Box)
                .Where(ap => auditBoxCodes.Contains(ap.AuditBoxCode) && ap.CountedQty != null)
                .OrderBy(ap => ap.AuditBox!.Box!.BoxNumber).ThenBy(ap => ap.Part!.PartName)
                .ToListAsync();

            string scopeLabel = ScopeLabel(session);
            string asignado = session.AssignedUser?.Employee != null
                ? $"{session.AssignedUser.Employee.EmpName} {session.AssignedUser.Employee.EmpSurname}"
                : session.AssignedUser?.UserLogin ?? "—";

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Item().Text($"Auditoría de Inventario #{session.AuditCode}").FontSize(14).Bold();
                            col.Item().Text($"Alcance: {scopeLabel}    Asignado a: {asignado}    Estado: {session.Status.Replace("_", " ")}").FontSize(8);
                            col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(4);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(3);
                            });

                            table.Header(hdr =>
                            {
                                void HdrCell(string txt) =>
                                    hdr.Cell().Background(Colors.Green.Darken2).Padding(3).Text(txt).FontColor(Colors.White).Bold().FontSize(7);
                                HdrCell("Caja"); HdrCell("Repuesto"); HdrCell("Sistema"); HdrCell("Contado");
                                HdrCell("Varianza"); HdrCell("Resultado");
                            });

                            bool altRow = false;
                            foreach (var ap in auditParts)
                            {
                                altRow = !altRow;
                                var bg = ap.Variance != 0 ? Colors.Orange.Lighten4 : (altRow ? Colors.Grey.Lighten4 : Colors.White);
                                void Cell(string txt) => table.Cell().Background(bg).Padding(2).Text(txt ?? "—").FontSize(7);

                                string resultado = ap.ResolutionStatus switch
                                {
                                    "SIN_DIFERENCIA"   => "Coincide",
                                    "APROBADO_AJUSTE"  => "Ajustado",
                                    "RECHAZADO"        => "Descartado",
                                    _                  => "Pendiente"
                                };

                                Cell($"Caja {ap.AuditBox?.Box?.BoxNumber}");
                                Cell(ap.Part?.PartName ?? "—");
                                Cell(ap.SystemQty.ToString());
                                Cell(ap.CountedQty?.ToString() ?? "—");
                                Cell(ap.Variance.HasValue ? (ap.Variance > 0 ? $"+{ap.Variance}" : ap.Variance.ToString()!) : "—");
                                Cell(resultado);
                            }

                            if (auditParts.Count == 0)
                            {
                                table.Cell().ColumnSpan(6).Padding(8).AlignCenter().Text("Aún no hay conteos registrados en esta auditoría.").FontColor(Colors.Grey.Medium);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Pagina ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            var bytes = doc.GeneratePdf();
            return File(bytes, "application/pdf", $"Auditoria_{session.AuditCode}_{DateTime.Today:yyyyMMdd}.pdf");
        }

        // ===================== CIERRE / CANCELACIÓN =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Close(int id)
        {
            var session = await _context.EngAuditSessions
                .Include(s => s.AuditBoxes)
                .FirstOrDefaultAsync(s => s.AuditCode == id);
            if (session == null) return NotFound();

            if (session.AuditBoxes.Any(ab => ab.Status != "CONTADA"))
            {
                TempData["Error"] = "Aún hay cajas sin contar. No se puede cerrar la auditoría.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var auditBoxCodes = session.AuditBoxes.Select(ab => ab.AuditBoxCode).ToList();
            var pendingCount = await _context.EngAuditParts
                .CountAsync(ap => auditBoxCodes.Contains(ap.AuditBoxCode) && ap.ResolutionStatus == "PENDIENTE");

            if (pendingCount > 0)
            {
                TempData["Error"] = $"Hay {pendingCount} discrepancia(s) sin resolver. Apruebe o descarte antes de cerrar.";
                return RedirectToAction(nameof(Review), new { id });
            }

            session.Status             = "CERRADA";
            session.ReviewedByUserCode = GetCurrentUserId();
            session.ReviewedDate       = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Auditoría cerrada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Cancel(int id)
        {
            var session = await _context.EngAuditSessions.FindAsync(id);
            if (session == null) return NotFound();

            if (session.Status == "CERRADA")
            {
                TempData["Error"] = "No se puede cancelar una auditoría ya cerrada.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            session.Status = "CANCELADA";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Auditoría cancelada.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== REASIGNACIÓN =====================

        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Reassign(int id)
        {
            var session = await LoadSessionWithBoxes(id);
            if (session == null) return NotFound();

            var vm = new AuditReassignVM
            {
                AuditCode        = session.AuditCode,
                ScopeLabel       = ScopeLabel(session),
                AssignedUserCode = session.AssignedUserCode,
                Users            = await GetUserSelectList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Reassign(AuditReassignVM vm)
        {
            var session = await _context.EngAuditSessions.FindAsync(vm.AuditCode);
            if (session == null) return NotFound();

            if (session.Status is "CERRADA" or "CANCELADA")
            {
                TempData["Error"] = "No se puede reasignar una auditoría cerrada o cancelada.";
                return RedirectToAction(nameof(Detail), new { id = vm.AuditCode });
            }

            session.AssignedUserCode = vm.AssignedUserCode!.Value;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Auditoría reasignada correctamente.";
            return RedirectToAction(nameof(Detail), new { id = vm.AuditCode });
        }

        // ===================== HELPERS =====================

        private async Task<EngAuditSession?> LoadSessionWithBoxes(int id) =>
            await _context.EngAuditSessions
                .Include(s => s.Line)
                .Include(s => s.Box).ThenInclude(b => b!.Line)
                .Include(s => s.AssignedUser).ThenInclude(u => u!.Employee)
                .Include(s => s.CreatedByUser).ThenInclude(u => u!.Employee)
                .Include(s => s.AuditBoxes).ThenInclude(ab => ab.Box).ThenInclude(b => b!.Line)
                .Include(s => s.AuditBoxes).ThenInclude(ab => ab.Box).ThenInclude(b => b!.Parts)
                .Include(s => s.AuditBoxes).ThenInclude(ab => ab.AuditParts).ThenInclude(ap => ap.Part)
                .FirstOrDefaultAsync(s => s.AuditCode == id);

        private bool CanAccessSession(EngAuditSession session)
        {
            if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.SvcAdmin)) return true;
            return session.AssignedUserCode == GetCurrentUserId();
        }

        private static string ScopeLabel(EngAuditSession session) =>
            session.AuditScope == "LINEA"
                ? $"Línea completa — {session.Line?.LineName}"
                : $"Caja {session.Box?.BoxNumber} — {session.Box?.Line?.LineName}";

        private async Task<IEnumerable<SelectListItem>> GetUserSelectList() =>
            (await _context.Users.Include(u => u.Employee).Where(u => u.Active)
                .OrderBy(u => u.Employee!.EmpSurname)
                .ToListAsync())
                .Select(u => new SelectListItem(
                    u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                    u.UserCode.ToString()));

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
