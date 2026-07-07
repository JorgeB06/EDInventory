using EDInventory.Data;
using EDInventory.Models;
using EDInventory.Models.Entities;
using EDInventory.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Gestión de calibraciones de activos clínicos (módulo Servicio).
    /// </summary>
    [Authorize(Roles = AppRoles.SvcRead)]
    public class CalibrationController : Controller
    {
        private readonly AppDbContext _context;

        public CalibrationController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        public async Task<IActionResult> Index(int? assetId, string? result, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.Calibrations
                .Include(c => c.Asset).ThenInclude(a => a!.Line)
                .Include(c => c.Asset).ThenInclude(a => a!.Hospital)
                .Include(c => c.User).ThenInclude(u => u!.Employee)
                .Include(c => c.TechUser).ThenInclude(u => u!.Employee)
                .AsQueryable();

            if (assetId.HasValue)           query = query.Where(c => c.AssetCode == assetId);
            if (!string.IsNullOrEmpty(result)) query = query.Where(c => c.CalibResult == result);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            ViewBag.AssetId       = assetId;
            ViewBag.Result        = result;
            ViewBag.Page          = page;
            ViewBag.TotalPages    = totalPages;
            ViewBag.Total         = total;
            ViewBag.CurrentUserId = GetCurrentUserId();

            var items = await query
                .OrderByDescending(c => c.CalibDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Create(int? assetId)
        {
            var vm = new CalibrationViewModel
            {
                AssetCode = assetId ?? 0,
                CalibDate = DateOnly.FromDateTime(DateTime.Today),
            };
            await PopulateSelectLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Create(CalibrationViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelectLists(vm);
                return View(vm);
            }

            var calib = new Calibration
            {
                AssetCode     = vm.AssetCode,
                CalibDate     = vm.CalibDate,
                CalibNextDate = vm.CalibNextDate,
                CalibLab      = vm.CalibLab,
                CalibCertNum  = vm.CalibCertNum,
                CalibType     = vm.CalibType,
                CalibResult   = vm.CalibResult,
                CalibNotes    = vm.CalibNotes,
                CalibRegDate  = DateTime.Now,
                UserCode      = GetCurrentUserId(),
                TechCode      = vm.TechCode,
            };

            _context.Calibrations.Add(calib);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Calibracion registrada correctamente.";
            return RedirectToAction("AssetDetail", "Engineering", new { id = vm.AssetCode });
        }

        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Edit(int id)
        {
            var calib = await _context.Calibrations.FindAsync(id);
            if (calib == null) return NotFound();

            var vm = new CalibrationViewModel
            {
                CalibCode     = calib.CalibCode,
                AssetCode     = calib.AssetCode,
                CalibDate     = calib.CalibDate,
                CalibNextDate = calib.CalibNextDate,
                CalibLab      = calib.CalibLab,
                CalibCertNum  = calib.CalibCertNum,
                CalibType     = calib.CalibType,
                CalibResult   = calib.CalibResult,
                CalibNotes    = calib.CalibNotes,
                TechCode      = calib.TechCode,
            };
            await PopulateSelectLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Edit(CalibrationViewModel vm)
        {
            var calib = await _context.Calibrations.FindAsync(vm.CalibCode);
            if (calib == null) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateSelectLists(vm);
                return View(vm);
            }

            calib.CalibDate     = vm.CalibDate;
            calib.CalibNextDate = vm.CalibNextDate;
            calib.CalibLab      = vm.CalibLab;
            calib.CalibCertNum  = vm.CalibCertNum;
            calib.CalibType     = vm.CalibType;
            calib.CalibResult   = vm.CalibResult;
            calib.CalibNotes    = vm.CalibNotes;
            calib.TechCode      = vm.TechCode;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Calibracion actualizada.";
            return RedirectToAction("AssetDetail", "Engineering", new { id = calib.AssetCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> Delete(int id)
        {
            var calib = await _context.Calibrations.FindAsync(id);
            if (calib == null) return NotFound();
            var assetCode = calib.AssetCode;
            _context.Calibrations.Remove(calib);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Calibracion eliminada.";
            return RedirectToAction("AssetDetail", "Engineering", new { id = assetCode });
        }

        // ── F4: Historial + notas ─────────────────────────────────────────────

        public async Task<IActionResult> History(int? assetId)
        {
            var query = _context.Calibrations
                .Include(c => c.Asset).ThenInclude(a => a!.Hospital)
                .Include(c => c.Asset).ThenInclude(a => a!.Line)
                .Include(c => c.User).ThenInclude(u => u!.Employee)
                .Include(c => c.TechUser).ThenInclude(u => u!.Employee)
                .AsQueryable();

            if (assetId.HasValue) query = query.Where(c => c.AssetCode == assetId);

            var calibrations = await query.OrderByDescending(c => c.CalibDate).ToListAsync();

            var calibCodes = calibrations.Select(c => c.CalibCode).ToList();
            var notes = await _context.CalibNotes
                .Where(n => calibCodes.Contains(n.CalibCode))
                .Include(n => n.User).ThenInclude(u => u!.Employee)
                .OrderBy(n => n.NoteDate)
                .ToListAsync();

            ViewBag.Notes   = notes;
            ViewBag.AssetId = assetId;
            return View(calibrations);
        }

        /// <summary>Exporta el historial de calibraciones (opcionalmente filtrado por activo) a un PDF imprimible.</summary>
        public async Task<IActionResult> ExportHistoryPdf(int? assetId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var query = _context.Calibrations
                .Include(c => c.Asset).ThenInclude(a => a!.Hospital)
                .Include(c => c.Asset).ThenInclude(a => a!.Line)
                .Include(c => c.TechUser).ThenInclude(u => u!.Employee)
                .AsQueryable();

            if (assetId.HasValue) query = query.Where(c => c.AssetCode == assetId);

            var calibrations = await query.OrderByDescending(c => c.CalibDate).ToListAsync();
            var titulo = assetId.HasValue && calibrations.Count > 0
                ? $"Historial de Calibraciones — {calibrations[0].Asset?.AssetDesc}"
                : "Historial de Calibraciones";

            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(titulo).FontSize(14).Bold();
                                col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(3);
                            });

                            table.Header(hdr =>
                            {
                                void HdrCell(string txt) =>
                                    hdr.Cell().Background(Colors.Orange.Darken3).Padding(3).Text(txt).FontColor(Colors.White).Bold().FontSize(7);
                                HdrCell("Activo"); HdrCell("Línea"); HdrCell("Hospital"); HdrCell("Fecha");
                                HdrCell("Próxima"); HdrCell("Resultado"); HdrCell("Técnico"); HdrCell("Notas");
                            });

                            bool altRow = false;
                            foreach (var c in calibrations)
                            {
                                altRow = !altRow;
                                var bg = altRow ? Colors.Orange.Lighten5 : Colors.White;
                                void Cell(string txt) => table.Cell().Background(bg).Padding(2).Text(txt ?? "—").FontSize(7);

                                Cell(c.Asset?.AssetDesc ?? "—");
                                Cell(c.Asset?.Line?.LineName ?? "—");
                                Cell(c.Asset?.Hospital?.HospName ?? "—");
                                Cell(c.CalibDate.ToString("dd/MM/yyyy"));
                                Cell(c.CalibNextDate?.ToString("dd/MM/yyyy") ?? "—");
                                Cell(c.CalibResult ?? "—");
                                Cell(c.TechUser?.Employee != null ? $"{c.TechUser.Employee.EmpName} {c.TechUser.Employee.EmpSurname}" : c.TechUser?.UserLogin ?? "—");
                                Cell(c.CalibNotes ?? "—");
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
            return File(bytes, "application/pdf", $"Historial_Calibraciones_{DateTime.Today:yyyyMMdd}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCalibNote(int calibCode, string noteText)
        {
            if (!string.IsNullOrWhiteSpace(noteText))
            {
                _context.CalibNotes.Add(new CalibNote
                {
                    CalibCode = calibCode,
                    NoteText  = noteText.Trim(),
                    UserCode  = GetCurrentUserId(),
                    NoteDate  = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(History));
        }

        // ── F5: Creación masiva de calibraciones por hospital ─────────────────

        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> BulkCreate()
        {
            var vm = new CalibBulkCreateVM
            {
                Hospitals = await _context.Hospitals
                    .Where(h => h.Active).OrderBy(h => h.HospName)
                    .Select(h => new SelectListItem(h.HospName, h.HospCode.ToString()))
                    .ToListAsync(),
                TechUsers = await _context.Users
                    .Include(u => u.Employee).Where(u => u.Active)
                    .OrderBy(u => u.Employee!.EmpSurname)
                    .Select(u => new SelectListItem(
                        u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                        u.UserCode.ToString()))
                    .ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> BulkCreate(CalibBulkCreateVM vm)
        {
            if (!vm.HospCode.HasValue)
                ModelState.AddModelError("HospCode", "Seleccione un hospital.");

            if (!ModelState.IsValid)
            {
                vm.Hospitals = await _context.Hospitals
                    .Where(h => h.Active).OrderBy(h => h.HospName)
                    .Select(h => new SelectListItem(h.HospName, h.HospCode.ToString()))
                    .ToListAsync();
                vm.TechUsers = await _context.Users
                    .Include(u => u.Employee).Where(u => u.Active)
                    .OrderBy(u => u.Employee!.EmpSurname)
                    .Select(u => new SelectListItem(
                        u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                        u.UserCode.ToString()))
                    .ToListAsync();
                return View(vm);
            }

            var assets = await _context.EngAssets
                .Where(a => a.Active && a.HospCode == vm.HospCode)
                .ToListAsync();

            var hosp        = await _context.Hospitals.FindAsync(vm.HospCode);
            var now         = DateTime.Now;
            var currentUser = GetCurrentUserId();

            foreach (var a in assets)
            {
                _context.Calibrations.Add(new Calibration
                {
                    AssetCode     = a.AssetCode,
                    CalibDate     = vm.CalibDate,
                    CalibNextDate = vm.CalibNextDate,
                    CalibResult   = vm.CalibResult,
                    CalibType     = vm.CalibType,
                    CalibLab      = vm.CalibLab,
                    CalibRegDate  = now,
                    UserCode      = currentUser,
                    TechCode      = vm.TechCode,
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{assets.Count} calibración(es) creadas para {hosp?.HospName ?? "el hospital"}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectLists(CalibrationViewModel vm)
        {
            vm.Assets = (await _context.EngAssets
                .Where(a => a.Active)
                .Include(a => a.Line)
                .OrderBy(a => a.Line!.LineName).ThenBy(a => a.AssetDesc)
                .ToListAsync())
                .Select(a => new SelectListItem(
                    $"{a.AssetDesc} ({a.Line?.LineName ?? "—"})",
                    a.AssetCode.ToString()));

            vm.TechUsers = await _context.Users
                .Include(u => u.Employee).Where(u => u.Active)
                .OrderBy(u => u.Employee!.EmpSurname)
                .Select(u => new SelectListItem(
                    u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                    u.UserCode.ToString()))
                .ToListAsync();
        }
    }
}
