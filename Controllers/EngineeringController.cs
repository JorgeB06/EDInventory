using ClosedXML.Excel;
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
    /// Controlador de ingeniería de servicio (división Servicio). Requiere rol <c>SvcRead</c> como mínimo.
    /// Gestiona: Líneas de servicio, Cajas de repuestos, Repuestos (con control de stock y movimientos),
    /// Activos clínicos (<see cref="EDInventory.Models.Entities.EngAsset"/>), importación Excel,
    /// reportes de stock, búsqueda de repuestos y mantenimientos de activos.
    /// Las acciones de escritura requieren <c>SvcWrite</c> o superior.
    /// </summary>
    [Authorize(Roles = AppRoles.SvcRead)]
    public class EngineeringController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>Inicializa el controlador con el contexto de base de datos.</summary>
        public EngineeringController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== LÍNEAS =====================

        /// <summary>
        /// Muestra el listado paginado de líneas de servicio de ingeniería con filtros por nombre y grupo.
        /// </summary>
        public async Task<IActionResult> Index(string? search, string? group, int page = 1)
        {
            const int pageSize = 12;

            var query = _context.EngLines
                .Include(l => l.Boxes)
                    .ThenInclude(b => b.Parts)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search;
                query = query.Where(l => (l.LineName != null && l.LineName.Contains(s)) ||
                                         (l.LineDesc != null && l.LineDesc.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(group))
                query = query.Where(l => l.LineGroup == group);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var lines = await query
                .OrderBy(l => l.LineGroup)
                .ThenBy(l => l.LineName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var allGroups = await _context.EngLines
                .Where(l => l.LineGroup != null && l.LineGroup != "")
                .Select(l => l.LineGroup!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            ViewBag.Groups     = allGroups;
            ViewBag.Search     = search;
            ViewBag.Group      = group;
            ViewBag.Page       = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total      = total;

            return View(lines);
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public IActionResult LineCreate()
        {
            return View(new EngLine { Active = true });
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> LineCreate(EngLine model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.EngLines.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Línea '{model.LineName}' creada.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> LineEdit(int id)
        {
            var line = await _context.EngLines.FindAsync(id);
            if (line == null) return NotFound();
            return View(line);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> LineEdit(EngLine model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.EngLines.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Línea actualizada.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== DETALLE DE LÍNEA =====================

        public async Task<IActionResult> LineDetail(int id, string? search)
        {
            var line = await _context.EngLines
                .Include(l => l.Boxes.OrderBy(b => b.BoxNumber))
                    .ThenInclude(b => b.Parts)
                .FirstOrDefaultAsync(l => l.LineCode == id);

            if (line == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLower();
                foreach (var box in line.Boxes)
                {
                    box.Parts = box.Parts
                        .Where(p => (p.PartName != null && p.PartName.ToLower().Contains(q)) ||
                                    (p.PartRef  != null && p.PartRef.ToLower().Contains(q)))
                        .ToList();
                }
            }

            ViewBag.Search = search;
            return View(line);
        }

        // ===================== CAJAS =====================

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> BoxCreate(int lineCode)
        {
            var line = await _context.EngLines.FindAsync(lineCode);
            if (line == null) return NotFound();

            ViewBag.LineName   = line.LineName;
            ViewBag.Warehouses = await _context.Warehouses.Where(w => w.Active).OrderBy(w => w.WareName).ToListAsync();
            return View(new EngBox { LineCode = lineCode, Active = true });
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> BoxCreate(EngBox model)
        {
            if (!ModelState.IsValid)
            {
                var line = await _context.EngLines.FindAsync(model.LineCode);
                ViewBag.LineName   = line?.LineName;
                ViewBag.Warehouses = await _context.Warehouses.Where(w => w.Active).OrderBy(w => w.WareName).ToListAsync();
                return View(model);
            }
            _context.EngBoxes.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Caja {model.BoxNumber} agregada.";
            return RedirectToAction(nameof(LineDetail), new { id = model.LineCode });
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> BoxEdit(int id)
        {
            var box = await _context.EngBoxes.Include(b => b.Line).FirstOrDefaultAsync(b => b.BoxCode == id);
            if (box == null) return NotFound();
            ViewBag.Warehouses = await _context.Warehouses.Where(w => w.Active).OrderBy(w => w.WareName).ToListAsync();
            return View(box);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> BoxEdit(EngBox model)
        {
            if (!ModelState.IsValid)
            {
                model.Line         = await _context.EngLines.FindAsync(model.LineCode);
                ViewBag.Warehouses = await _context.Warehouses.Where(w => w.Active).OrderBy(w => w.WareName).ToListAsync();
                return View(model);
            }
            _context.EngBoxes.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Caja {model.BoxNumber} actualizada.";
            return RedirectToAction(nameof(LineDetail), new { id = model.LineCode });
        }

        // ===================== ETIQUETA CAJA =====================

        public async Task<IActionResult> BoxLabel(int id)
        {
            var box = await _context.EngBoxes
                .Include(b => b.Line)
                .Include(b => b.Parts)
                .Include(b => b.Warehouse)
                .FirstOrDefaultAsync(b => b.BoxCode == id);

            if (box == null) return NotFound();

            var company = await _context.Companies
                .Where(c => c.Active)
                .OrderBy(c => c.CompanyCode)
                .FirstOrDefaultAsync();

            ViewBag.Company = company;
            return View(box);
        }

        // ===================== DETALLE DE CAJA =====================

        public async Task<IActionResult> BoxDetail(int id)
        {
            var box = await _context.EngBoxes
                .Include(b => b.Line)
                .Include(b => b.Parts)
                .Include(b => b.Warehouse)
                .FirstOrDefaultAsync(b => b.BoxCode == id);

            if (box == null) return NotFound();

            var partCodes = box.Parts.Select(p => p.PartCode).ToList();
            ViewBag.Movements = await _context.EngPartMovements
                .Include(m => m.Part)
                .Include(m => m.User).ThenInclude(u => u!.Employee)
                .Include(m => m.Hospital)
                .Where(m => partCodes.Contains(m.PartCode))
                .OrderByDescending(m => m.MovDate)
                .Take(30)
                .ToListAsync();

            return View(box);
        }

        // ===================== MOVIMIENTOS DE REPUESTOS =====================

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> PartWithdraw(int id)
        {
            var part = await _context.EngParts
                .Include(p => p.Box)
                .FirstOrDefaultAsync(p => p.PartCode == id);
            if (part == null) return NotFound();

            var vm = new EDInventory.Models.ViewModels.PartWithdrawVM
            {
                PartCode   = part.PartCode,
                BoxCode    = part.BoxCode,
                PartName   = part.PartName,
                PartRef    = part.PartRef,
                CurrentQty = part.PartQty,
                Hospitals  = await _context.Hospitals
                    .Where(h => h.Active).OrderBy(h => h.HospName)
                    .Select(h => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(h.HospName, h.HospCode.ToString()))
                    .ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> PartWithdraw(EDInventory.Models.ViewModels.PartWithdrawVM vm)
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
                vm.Hospitals = await _context.Hospitals
                    .Where(h => h.Active).OrderBy(h => h.HospName)
                    .Select(h => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(h.HospName, h.HospCode.ToString()))
                    .ToListAsync();
                return View(vm);
            }

            // MovQty negativo = RETIRO (salida), positivo = DEVOLUCION (entrada)
            int delta = vm.MovType == "RETIRO" ? -vm.Qty : vm.Qty;
            part.PartQty += delta;

            _context.EngParts.Update(part);
            _context.EngPartMovements.Add(new EngPartMovement
            {
                PartCode     = part.PartCode,
                UserCode     = GetSvcCurrentUserId(),
                MovDate      = DateTime.Now,
                MovQty       = delta,
                MovType      = vm.MovType,
                HospCode     = vm.HospCode,
                MovNotes     = vm.Notes,
                PartQtyAfter = part.PartQty
            });
            await _context.SaveChangesAsync();

            var tipoLabel = vm.MovType == "RETIRO" ? "Retiro" : "Devolución";
            TempData["Success"] = $"{tipoLabel} registrado. Nuevo stock: {part.PartQty}";
            return RedirectToAction(nameof(BoxDetail), new { id = part.BoxCode });
        }

        // ===================== HISTORIAL DE REPUESTO =====================

        public async Task<IActionResult> PartHistory(int id)
        {
            var part = await _context.EngParts
                .Include(p => p.Box).ThenInclude(b => b!.Line)
                .FirstOrDefaultAsync(p => p.PartCode == id);
            if (part == null) return NotFound();

            var movements = await _context.EngPartMovements
                .Include(m => m.User).ThenInclude(u => u!.Employee)
                .Include(m => m.Hospital)
                .Where(m => m.PartCode == id)
                .OrderByDescending(m => m.MovDate)
                .ToListAsync();

            ViewBag.Movements = movements;
            return View(part);
        }

        // ===================== ESCANEO DE CAJA =====================

        public IActionResult ScanBox()
        {
            return View();
        }

        public async Task<IActionResult> BoxByScan(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return RedirectToAction(nameof(ScanBox));

            var raw = code.Trim();
            EngBox? box = null;

            // Formato principal: ENG{BoxCode:D6}  ej. ENG000014
            if (raw.StartsWith("ENG", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(raw[3..], out int boxCodeA))
            {
                box = await _context.EngBoxes.FirstOrDefaultAsync(b => b.BoxCode == boxCodeA);
            }

            // Fallback: ENG-{LineCode}-{BoxNumber}
            if (box == null)
            {
                var parts = raw.Split('-');
                if (parts.Length == 3 && parts[0].Equals("ENG", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(parts[1], out int lineCode)
                    && int.TryParse(parts[2], out int boxNumber))
                {
                    box = await _context.EngBoxes
                        .FirstOrDefaultAsync(b => b.LineCode == lineCode && b.BoxNumber == boxNumber);
                }

                // Fallback: ENG-{BoxCode}
                if (box == null && parts.Length == 2
                    && parts[0].Equals("ENG", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(parts[1], out int boxCodeB))
                {
                    box = await _context.EngBoxes.FirstOrDefaultAsync(b => b.BoxCode == boxCodeB);
                }
            }

            if (box != null)
                return RedirectToAction(nameof(BoxDetail), new { id = box.BoxCode });

            // Muestra el valor exacto recibido (largo y caracteres) para diagnóstico
            TempData["ScanError"] = $"Código no reconocido: \"{raw}\" ({raw.Length} caracteres)";
            return RedirectToAction(nameof(ScanBox));
        }

        // ===================== REPUESTOS =====================

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> PartCreate(int boxCode)
        {
            var box = await _context.EngBoxes.Include(b => b.Line).FirstOrDefaultAsync(b => b.BoxCode == boxCode);
            if (box == null) return NotFound();

            ViewBag.BoxNumber = box.BoxNumber;
            ViewBag.LineName  = box.Line?.LineName;
            ViewBag.LineCode  = box.LineCode;
            return View(new EngPart { BoxCode = boxCode });
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> PartCreate(EngPart model)
        {
            if (!ModelState.IsValid)
            {
                var box = await _context.EngBoxes.Include(b => b.Line).FirstOrDefaultAsync(b => b.BoxCode == model.BoxCode);
                ViewBag.BoxNumber = box?.BoxNumber;
                ViewBag.LineName  = box?.Line?.LineName;
                ViewBag.LineCode  = box?.LineCode;
                return View(model);
            }
            _context.EngParts.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Repuesto '{model.PartName}' agregado.";

            var b2 = await _context.EngBoxes.FindAsync(model.BoxCode);
            return RedirectToAction(nameof(LineDetail), new { id = b2?.LineCode });
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> PartEdit(int id)
        {
            var part = await _context.EngParts
                .Include(p => p.Box)
                    .ThenInclude(b => b!.Line)
                .FirstOrDefaultAsync(p => p.PartCode == id);
            if (part == null) return NotFound();

            ViewBag.BoxNumber = part.Box?.BoxNumber;
            ViewBag.LineName  = part.Box?.Line?.LineName;
            ViewBag.LineCode  = part.Box?.LineCode;
            return View(part);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> PartEdit(EngPart model)
        {
            if (!ModelState.IsValid)
            {
                var box = await _context.EngBoxes.Include(b => b.Line).FirstOrDefaultAsync(b => b.BoxCode == model.BoxCode);
                ViewBag.BoxNumber = box?.BoxNumber;
                ViewBag.LineName  = box?.Line?.LineName;
                ViewBag.LineCode  = box?.LineCode;
                return View(model);
            }
            _context.EngParts.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Repuesto actualizado.";

            var b2 = await _context.EngBoxes.FindAsync(model.BoxCode);
            return RedirectToAction(nameof(LineDetail), new { id = b2?.LineCode });
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> PartDelete(int id)
        {
            var part = await _context.EngParts.Include(p => p.Box).FirstOrDefaultAsync(p => p.PartCode == id);
            if (part == null) return NotFound();

            var lineCode = part.Box?.LineCode;
            _context.EngParts.Remove(part);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Repuesto eliminado.";
            return RedirectToAction(nameof(LineDetail), new { id = lineCode });
        }

        // ===================== REPORTE IMPRIMIBLE =====================

        /// <summary>Muestra el reporte imprimible de stock de repuestos agrupado por línea y caja.</summary>
        public async Task<IActionResult> StockReport()
        {
            var lines = await _context.EngLines
                .Include(l => l.Boxes.Where(b => b.Active).OrderBy(b => b.BoxNumber))
                    .ThenInclude(b => b.Parts.OrderBy(p => p.PartRef))
                .Include(l => l.Boxes)
                    .ThenInclude(b => b.Warehouse)
                .Where(l => l.Active)
                .OrderBy(l => l.LineGroup)
                .ThenBy(l => l.LineName)
                .ToListAsync();

            ViewBag.GeneratedAt = DateTime.Now;
            return View(lines);
        }

        // ===================== EXPORTACIÓN A EXCEL =====================

        /// <summary>
        /// Genera y descarga un archivo Excel (.xlsx) con el inventario completo de repuestos
        /// incluyendo stock actual, línea, caja, ubicación en bodega y referencia del fabricante.
        /// </summary>
        public async Task<IActionResult> ExportStock()
        {
            var parts = await _context.EngParts
                .Include(p => p.Box)
                    .ThenInclude(b => b!.Line)
                .Include(p => p.Box)
                    .ThenInclude(b => b!.Warehouse)
                .OrderBy(p => p.Box!.Line!.LineGroup)
                .ThenBy(p => p.Box!.Line!.LineName)
                .ThenBy(p => p.Box!.BoxNumber)
                .ThenBy(p => p.PartRef)
                .ToListAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Stock Repuestos");

            string[] headers = ["Grupo", "Línea", "Caja", "Desc. Caja", "Bodega", "Rack", "Estante",
                                 "Cód. Interno", "Parte Fab.", "Repuesto", "Cantidad", "Notas"];
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold       = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            headerRange.Style.Font.FontColor  = XLColor.White;

            int row = 2;
            foreach (var p in parts)
            {
                ws.Cell(row, 1).Value  = p.Box?.Line?.LineGroup ?? "";
                ws.Cell(row, 2).Value  = p.Box?.Line?.LineName  ?? "";
                ws.Cell(row, 3).Value  = p.Box?.BoxNumber;
                ws.Cell(row, 4).Value  = p.Box?.BoxDesc ?? "";
                ws.Cell(row, 5).Value  = p.Box?.Warehouse?.WareName ?? "";
                ws.Cell(row, 6).Value  = p.Box?.WareRack ?? "";
                ws.Cell(row, 7).Value  = p.Box?.WareEstante ?? "";
                ws.Cell(row, 8).Value  = p.PartRef ?? "";
                ws.Cell(row, 9).Value  = p.PartMfrRef ?? "";
                ws.Cell(row, 10).Value = p.PartName ?? "";
                ws.Cell(row, 11).Value = p.PartQty;
                ws.Cell(row, 12).Value = p.PartNotes ?? "";

                var qtyCell = ws.Cell(row, 11);
                if (p.PartQty == 0)
                    qtyCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");
                else if (p.PartQty <= 2)
                    qtyCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var fecha = DateTime.Now.ToString("yyyyMMdd_HHmm");
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"stock_repuestos_{fecha}.xlsx");
        }

        // ===================== IMPORTACIÓN MASIVA =====================

        /// <summary>
        /// Genera y descarga la plantilla Excel (.xlsx) para importar repuestos de ingeniería masivamente.
        /// </summary>
        public IActionResult ImportTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Repuestos");

            // Encabezados
            string[] headers = ["LINEA_GRUPO", "LINEA_NOMBRE", "CAJA_NUMERO", "CAJA_DESC",
                                 "REPUESTO_REF", "REPUESTO_NOMBRE", "CANTIDAD", "NOTAS"];
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            headerRange.Style.Font.FontColor = XLColor.White;

            // Fila de ejemplo
            ws.Cell(2, 1).Value = "PHILIPS";
            ws.Cell(2, 2).Value = "ULTRASOUND HD11";
            ws.Cell(2, 3).Value = 1;
            ws.Cell(2, 4).Value = "Caja principal";
            ws.Cell(2, 5).Value = "PHI-001";
            ws.Cell(2, 6).Value = "Transductor L12-3";
            ws.Cell(2, 7).Value = 2;
            ws.Cell(2, 8).Value = "Reserva";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "plantilla_ingenieria.xlsx");
        }

        /// <summary>[GET] Muestra el formulario de importación masiva de repuestos. Requiere rol <c>SvcWrite</c>.</summary>
        [Authorize(Roles = AppRoles.SvcWrite)]
        public IActionResult Import() => View();

        /// <summary>
        /// [POST] Procesa la importación masiva de repuestos de ingeniería desde un archivo Excel.
        /// Crea líneas, cajas y repuestos si no existen; omite duplicados por referencia interna.
        /// Retorna un <see cref="EDInventory.Models.ViewModels.EngImportResultViewModel"/> con el resultado.
        /// Requiere rol <c>SvcWrite</c>.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> Import(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Selecciona un archivo Excel (.xlsx).");
                return View();
            }

            var result = new EngImportResultViewModel();

            // Cachés en memoria para evitar N+1 queries
            var lineCache = (await _context.EngLines.ToListAsync())
                            .ToDictionary(l => $"{l.LineGroup}|{l.LineName}");
            var boxCache  = (await _context.EngBoxes.ToListAsync())
                            .ToDictionary(b => $"{b.LineCode}|{b.BoxNumber}");
            var partSet   = (await _context.EngParts.ToListAsync())
                            .Where(p => p.PartRef != null)
                            .Select(p => $"{p.BoxCode}|{p.PartRef}")
                            .ToHashSet();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            XLWorkbook wb;
            IXLWorksheet ws;
            try
            {
                wb = new XLWorkbook(stream);
                ws = wb.Worksheets.First();
            }
            catch
            {
                ModelState.AddModelError("", "No se pudo leer el archivo Excel. Verifique que sea un archivo .xlsx valido y no este protegido.");
                return View();
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                int rowNum = 2;
                while (!ws.Row(rowNum).IsEmpty())
                {
                    var r         = ws.Row(rowNum);
                    var lineGroup = r.Cell(1).GetString().Trim();
                    var lineName  = r.Cell(2).GetString().Trim();
                    var boxNumStr = r.Cell(3).GetString().Trim();
                    var boxDesc   = r.Cell(4).GetString().Trim();
                    var partRef   = r.Cell(5).GetString().Trim();
                    var partName  = r.Cell(6).GetString().Trim();
                    var qtyStr    = r.Cell(7).GetString().Trim();
                    var notes     = r.Cell(8).GetString().Trim();

                    result.TotalRows++;

                    if (string.IsNullOrEmpty(lineName))
                    {
                        result.Errors.Add($"Fila {rowNum}: LINEA_NOMBRE es obligatorio.");
                        rowNum++; continue;
                    }
                    if (!int.TryParse(boxNumStr, out int boxNum) || boxNum <= 0)
                    {
                        result.Errors.Add($"Fila {rowNum}: CAJA_NUMERO debe ser un número positivo.");
                        rowNum++; continue;
                    }
                    if (string.IsNullOrEmpty(partName))
                    {
                        result.Errors.Add($"Fila {rowNum}: REPUESTO_NOMBRE es obligatorio.");
                        rowNum++; continue;
                    }
                    if (!int.TryParse(qtyStr, out int qty) || qty < 0) qty = 0;

                    // Buscar o crear EngLine
                    var lineKey = $"{lineGroup}|{lineName}";
                    if (!lineCache.TryGetValue(lineKey, out var line))
                    {
                        line = new EngLine
                        {
                            LineName  = lineName,
                            LineGroup = string.IsNullOrEmpty(lineGroup) ? null : lineGroup,
                            Active    = true
                        };
                        _context.EngLines.Add(line);
                        await _context.SaveChangesAsync();
                        lineCache[lineKey] = line;
                        result.LinesCreated++;
                    }

                    // Buscar o crear EngBox
                    var boxKey = $"{line.LineCode}|{boxNum}";
                    if (!boxCache.TryGetValue(boxKey, out var box))
                    {
                        box = new EngBox
                        {
                            LineCode  = line.LineCode,
                            BoxNumber = boxNum,
                            BoxDesc   = string.IsNullOrEmpty(boxDesc) ? null : boxDesc,
                            Active    = true
                        };
                        _context.EngBoxes.Add(box);
                        await _context.SaveChangesAsync();
                        boxCache[boxKey] = box;
                        result.BoxesCreated++;
                    }

                    // Verificar duplicado por PartRef en misma caja
                    var partKey = $"{box.BoxCode}|{partRef}";
                    if (!string.IsNullOrEmpty(partRef) && partSet.Contains(partKey))
                    {
                        result.PartsSkipped++;
                        rowNum++; continue;
                    }

                    _context.EngParts.Add(new EngPart
                    {
                        BoxCode   = box.BoxCode,
                        PartRef   = string.IsNullOrEmpty(partRef) ? null : partRef,
                        PartName  = partName,
                        PartQty   = qty,
                        PartNotes = string.IsNullOrEmpty(notes) ? null : notes
                    });

                    if (!string.IsNullOrEmpty(partRef))
                        partSet.Add(partKey);

                    result.PartsInserted++;
                    rowNum++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error inesperado durante la importacion. No se guardaron cambios.");
                return View();
            }

            return View("ImportResult", result);
        }

        // ===================== ACTIVOS EN SITIO =====================

        /// <summary>
        /// Muestra el inventario paginado de activos clínicos con filtros por línea de servicio,
        /// hospital, bodega y estado activo/inactivo.
        /// </summary>
        public async Task<IActionResult> AssetsOnSite(string? search, int? lineId, int? hospId, int? wareId, bool? active, string? status, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.EngAssets
                .Include(a => a.Line)
                .Include(a => a.Hospital)
                .Include(a => a.HospitalDepartment)
                .Include(a => a.Warehouse)
                .Include(a => a.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.AssetDesc!.Contains(search) || a.AssetSN!.Contains(search) || a.AssetNum!.Contains(search) || a.NetHostname!.Contains(search) || a.NetIp!.Contains(search));

            if (lineId.HasValue)  query = query.Where(a => a.LineCode == lineId);
            if (hospId.HasValue)  query = query.Where(a => a.HospCode == hospId);
            if (wareId.HasValue)  query = query.Where(a => a.WareCode == wareId);
            if (active.HasValue)  query = query.Where(a => a.Active == active);
            if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.AssetStatus == status);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var items = await query
                .OrderBy(a => a.Line!.LineName)
                .ThenBy(a => a.AssetDesc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search    = search;
            ViewBag.LineId    = lineId;
            ViewBag.HospId    = hospId;
            ViewBag.WareId    = wareId;
            ViewBag.Active    = active;
            ViewBag.Status    = status;
            ViewBag.Page      = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total     = total;

            ViewBag.Lines = new SelectList(
                await _context.EngLines.Where(l => l.Active).OrderBy(l => l.LineName).ToListAsync(),
                "LineCode", "LineName");
            ViewBag.Hospitals = new SelectList(
                await _context.Hospitals.Where(h => h.Active).OrderBy(h => h.HospName).ToListAsync(),
                "HospCode", "HospName");
            ViewBag.Warehouses = new SelectList(
                await _context.Warehouses.Where(w => w.Active).OrderBy(w => w.WareName).ToListAsync(),
                "WareCode", "WareName");

            return View(items);
        }

        public async Task<IActionResult> ExportAssets(string? search, int? lineId, int? hospId, int? wareId, bool? active)
        {
            var query = _context.EngAssets
                .Include(a => a.Line)
                .Include(a => a.Hospital)
                .Include(a => a.HospitalDepartment)
                .Include(a => a.Warehouse)
                .Include(a => a.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                .Include(a => a.Licitacion)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.AssetDesc!.Contains(search) || a.AssetSN!.Contains(search) || a.AssetNum!.Contains(search));
            if (lineId.HasValue) query = query.Where(a => a.LineCode == lineId);
            if (hospId.HasValue) query = query.Where(a => a.HospCode == hospId);
            if (wareId.HasValue) query = query.Where(a => a.WareCode == wareId);
            if (active.HasValue) query = query.Where(a => a.Active == active);

            var items = await query
                .OrderBy(a => a.Line!.LineName)
                .ThenBy(a => a.AssetDesc)
                .ToListAsync();

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Activos Clinicos");

            string[] headers = ["#", "Descripcion", "Linea", "Tipo Activo", "Marca", "Modelo",
                "Serie", "Num. Activo", "Garantia", "Ubicacion", "Detalle Ubicacion",
                "Licitacion", "Fin Licitacion", "Estado Lic.", "Activo"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#8a4800");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var a in items)
            {
                var licExpired   = a.Licitacion?.LicEnd.HasValue == true && a.Licitacion.LicEnd < hoy;
                var licPostponed = licExpired && a.Licitacion?.LicPostponed == true;
                string licEstado = a.Licitacion?.LicEnd == null ? ""
                    : licPostponed  ? "Aplazada"
                    : licExpired    ? "Vencida"
                    : a.Licitacion!.LicEnd <= DateOnly.FromDateTime(DateTime.Today.AddDays(a.Licitacion.LicWarnDays)) ? "Por vencer"
                    : "Vigente";

                string ubicacion = a.WareCode.HasValue ? "Bodega"
                    : a.HospCode.HasValue ? "Hospital"
                    : a.SiteCode.HasValue ? "Sede" : "";
                string detUbicacion = a.WareCode.HasValue
                    ? $"{a.Warehouse?.WareName} R:{a.WareRack} E:{a.WareEstante}"
                    : a.HospCode.HasValue
                    ? $"{a.Hospital?.HospName} - {a.HospitalDepartment?.HospDepName}"
                    : "";

                ws.Cell(row, 1).Value  = a.AssetCode;
                ws.Cell(row, 2).Value  = a.AssetDesc ?? "";
                ws.Cell(row, 3).Value  = a.Line?.LineName ?? "";
                ws.Cell(row, 4).Value  = a.Model?.Brand?.AssetType?.AssetsDesc ?? "";
                ws.Cell(row, 5).Value  = a.Model?.Brand?.BrandDesc ?? "";
                ws.Cell(row, 6).Value  = a.Model?.ModelDesc ?? "";
                ws.Cell(row, 7).Value  = a.AssetSN ?? "";
                ws.Cell(row, 8).Value  = a.AssetNum ?? "";
                ws.Cell(row, 9).Value  = a.AssetGnum ?? "";
                ws.Cell(row, 10).Value = ubicacion;
                ws.Cell(row, 11).Value = detUbicacion;
                ws.Cell(row, 12).Value = a.Licitacion != null ? $"{a.Licitacion.LicNum} - {a.Licitacion.LicDesc}" : "";
                ws.Cell(row, 13).Value = a.AssetDelic?.ToString("dd/MM/yyyy") ?? "";
                ws.Cell(row, 14).Value = licEstado;
                ws.Cell(row, 15).Value = a.Active ? "Activo" : "Inactivo";

                if (licPostponed || licEstado == "Por vencer")
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
                else if (licExpired)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Activos_Clinicos_{DateTime.Today:yyyyMMdd}.xlsx");
        }

        public async Task<IActionResult> AssetLabel(int id)
        {
            var asset = await _context.EngAssets
                .Include(a => a.Line)
                .Include(a => a.Hospital)
                .Include(a => a.HospitalDepartment)
                .Include(a => a.Warehouse)
                .Include(a => a.Site)
                .Include(a => a.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                .FirstOrDefaultAsync(a => a.AssetCode == id);

            if (asset == null) return NotFound();

            var company = await _context.Companies
                .Where(c => c.Active)
                .OrderBy(c => c.CompanyCode)
                .FirstOrDefaultAsync();

            ViewBag.Company = company;
            return View(asset);
        }

        public async Task<IActionResult> AssetDetail(int id)
        {
            var asset = await _context.EngAssets
                .Include(a => a.Line)
                .Include(a => a.Site)
                .Include(a => a.Hospital)
                .Include(a => a.HospitalDepartment)
                .Include(a => a.Warehouse)
                .Include(a => a.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                .Include(a => a.Licitacion)
                .Include(a => a.History.OrderByDescending(h => h.HistDate))
                    .ThenInclude(h => h.User)
                .Include(a => a.History)
                    .ThenInclude(h => h.Hospital)
                .Include(a => a.History)
                    .ThenInclude(h => h.HospitalDepartment)
                .Include(a => a.History)
                    .ThenInclude(h => h.Warehouse)
                .Include(a => a.History)
                    .ThenInclude(h => h.Site)
                .FirstOrDefaultAsync(a => a.AssetCode == id);

            if (asset == null) return NotFound();

            ViewBag.Maintenances = await _context.EngMaintenances
                .Include(m => m.User).ThenInclude(u => u!.Employee)
                .Where(m => m.AssetCode == id)
                .OrderByDescending(m => m.MaintScheduled)
                .ToListAsync();

            ViewBag.AssetMovements = await _context.EngAssetMovements
                .Include(m => m.User).ThenInclude(u => u!.Employee)
                .Include(m => m.Hospital)
                .Where(m => m.AssetCode == id)
                .OrderByDescending(m => m.MovDate)
                .ToListAsync();

            ViewBag.Incidents = await _context.Incidents
                .Include(i => i.Reporter).ThenInclude(u => u!.Employee)
                .Include(i => i.Assignee).ThenInclude(u => u!.Employee)
                .Where(i => i.AssetCode == id)
                .OrderByDescending(i => i.IncidentDate)
                .ToListAsync();

            ViewBag.Calibrations = await _context.Calibrations
                .Include(c => c.User).ThenInclude(u => u!.Employee)
                .Where(c => c.AssetCode == id)
                .OrderByDescending(c => c.CalibDate)
                .ToListAsync();

            ViewBag.Documents = await _context.Documents
                .Include(d => d.User).ThenInclude(u => u!.Employee)
                .Where(d => d.AssetCode == id)
                .OrderByDescending(d => d.DocUploadDate)
                .ToListAsync();

            ViewBag.AssetParts = await _context.AssetParts
                .Include(ap => ap.Part)
                    .ThenInclude(p => p!.Box)
                        .ThenInclude(b => b!.Line)
                .Where(ap => ap.AssetCode == id)
                .ToListAsync();

            ViewBag.AvailableParts = await _context.EngParts
                .Include(p => p.Box).ThenInclude(b => b!.Line)
                .OrderBy(p => p.Box!.Line!.LineName)
                .ThenBy(p => p.PartName)
                .Select(p => new SelectListItem(
                    $"{p.PartName} ({p.PartRef}) - {p.Box!.Line!.LineName}",
                    p.PartCode.ToString()))
                .ToListAsync();

            return View(asset);
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> AssetCreate()
        {
            var vm = new EngAssetViewModel
            {
                AssetDnew = DateOnly.FromDateTime(DateTime.Today),
                Active = true
            };
            await PopulateAssetSelectLists(vm);
            ViewBag.LicitacionDates = System.Text.Json.JsonSerializer.Serialize(
                (await _context.Licitaciones.Where(l => l.Active).ToListAsync())
                .ToDictionary(
                    l => l.LicCode.ToString(),
                    l => new { start = l.LicStart?.ToString("yyyy-MM-dd"), end = l.LicEnd?.ToString("yyyy-MM-dd") }
                )
            );
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> AssetCreate(EngAssetViewModel vm)
        {
            ValidateAssetLocationFields(vm);

            if (!string.IsNullOrWhiteSpace(vm.AssetSN) &&
                await _context.EngAssets.AnyAsync(a => a.AssetSN == vm.AssetSN.Trim()))
                ModelState.AddModelError(nameof(vm.AssetSN), "Este numero de serie ya esta registrado.");

            if (!ModelState.IsValid)
            {
                await PopulateAssetSelectLists(vm);
                return View(vm);
            }

            var asset = new EngAsset
            {
                AssetDesc    = vm.AssetDesc,
                LineCode     = vm.LineCode,
                HospCode     = vm.HospCode,
                HospDepCode  = vm.HospDepCode,
                AssetHospPos = vm.AssetHospPos,
                WareCode     = vm.WareCode,
                WareRack     = vm.WareRack,
                WareEstante  = vm.WareEstante,
                SiteCode     = vm.SiteCode,
                ModelCode    = vm.ModelCode,
                AssetSN      = vm.AssetSN,
                LicCode      = vm.LicCode,
                AssetNum     = vm.AssetNum,
                AssetDslic   = vm.AssetDslic,
                AssetDelic   = vm.AssetDelic,
                AssetGnum    = vm.AssetGnum,
                AssetDjequip = vm.AssetDjequip,
                AssetAddata  = vm.AssetAddata,
                AssetDnew    = DateOnly.FromDateTime(DateTime.Today),
                AssetDmod    = DateOnly.FromDateTime(DateTime.Today),
                Active          = vm.Active,
                AssetStatus     = vm.AssetStatus,
                ResponsibleUser = vm.ResponsibleUser,
                ResponsibleExt  = vm.ResponsibleExt,
                AcquireCost     = vm.AcquireCost,
                AcquireDate     = vm.AcquireDate,
                DepreYears      = vm.DepreYears,
                NetHostname     = vm.NetHostname,
                NetInDomain     = vm.NetInDomain,
                NetEnabled      = vm.NetEnabled,
                NetIp           = vm.NetEnabled ? vm.NetIp  : null,
                NetType         = vm.NetEnabled ? vm.NetType : null,
            };

            await using var tx = await _context.Database.BeginTransactionAsync();
            _context.EngAssets.Add(asset);
            await _context.SaveChangesAsync();
            await RegisterAssetHistory(asset, vm.HistNotes, GetSvcCurrentUserId(), "CREACION");
            await tx.CommitAsync();

            TempData["Success"] = "Activo clinico registrado correctamente.";
            return RedirectToAction(nameof(AssetsOnSite));
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> AssetEdit(int id)
        {
            var entity = await _context.EngAssets.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = new EngAssetViewModel
            {
                AssetCode    = entity.AssetCode,
                AssetDesc    = entity.AssetDesc ?? string.Empty,
                LineCode     = entity.LineCode,
                HospCode     = entity.HospCode,
                HospDepCode  = entity.HospDepCode,
                AssetHospPos = entity.AssetHospPos,
                WareCode     = entity.WareCode,
                WareRack     = entity.WareRack,
                WareEstante  = entity.WareEstante,
                SiteCode     = entity.SiteCode,
                ModelCode    = entity.ModelCode,
                AssetSN      = entity.AssetSN,
                LicCode      = entity.LicCode,
                AssetNum     = entity.AssetNum,
                AssetDslic   = entity.AssetDslic,
                AssetDelic   = entity.AssetDelic,
                AssetGnum    = entity.AssetGnum,
                AssetDjequip = entity.AssetDjequip,
                AssetAddata  = entity.AssetAddata,
                AssetDnew    = entity.AssetDnew,
                AssetDmod    = entity.AssetDmod,
                Active          = entity.Active,
                AssetStatus     = entity.AssetStatus,
                ResponsibleUser = entity.ResponsibleUser,
                ResponsibleExt  = entity.ResponsibleExt,
                AcquireCost     = entity.AcquireCost,
                AcquireDate     = entity.AcquireDate,
                DepreYears      = entity.DepreYears,
                NetHostname     = entity.NetHostname,
                NetInDomain     = entity.NetInDomain,
                NetEnabled      = entity.NetEnabled,
                NetIp           = entity.NetIp,
                NetType         = entity.NetType,
            };
            await PopulateAssetSelectLists(vm);
            ViewBag.LicitacionDates = System.Text.Json.JsonSerializer.Serialize(
                (await _context.Licitaciones.Where(l => l.Active).ToListAsync())
                .ToDictionary(
                    l => l.LicCode.ToString(),
                    l => new { start = l.LicStart?.ToString("yyyy-MM-dd"), end = l.LicEnd?.ToString("yyyy-MM-dd") }
                )
            );
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> AssetEdit(EngAssetViewModel vm)
        {
            ValidateAssetLocationFields(vm);

            if (!string.IsNullOrWhiteSpace(vm.AssetSN) &&
                await _context.EngAssets.AnyAsync(a => a.AssetSN == vm.AssetSN.Trim() && a.AssetCode != vm.AssetCode))
                ModelState.AddModelError(nameof(vm.AssetSN), "Este numero de serie ya esta registrado.");

            if (!ModelState.IsValid)
            {
                await PopulateAssetSelectLists(vm);
                return View(vm);
            }

            var entity = await _context.EngAssets.FindAsync(vm.AssetCode);
            if (entity == null) return NotFound();

            bool locationChanged =
                entity.HospCode     != vm.HospCode     || entity.HospDepCode != vm.HospDepCode ||
                entity.AssetHospPos != vm.AssetHospPos ||
                entity.WareCode     != vm.WareCode     || entity.WareRack    != vm.WareRack    ||
                entity.WareEstante  != vm.WareEstante  || entity.SiteCode    != vm.SiteCode;

            string? newNetIp   = vm.NetEnabled ? vm.NetIp   : null;
            string? newNetType = vm.NetEnabled ? vm.NetType : null;

            bool netChanged =
                entity.NetHostname != vm.NetHostname ||
                entity.NetInDomain != vm.NetInDomain ||
                entity.NetEnabled  != vm.NetEnabled  ||
                entity.NetIp       != newNetIp       ||
                entity.NetType     != newNetType;

            bool dataChanged =
                entity.ModelCode   != vm.ModelCode  ||
                entity.AssetSN     != vm.AssetSN    ||
                entity.AssetNum    != vm.AssetNum    ||
                entity.AssetGnum   != vm.AssetGnum   ||
                entity.AssetAddata != vm.AssetAddata;

            entity.AssetDesc    = vm.AssetDesc;
            entity.LineCode     = vm.LineCode;
            entity.HospCode     = vm.HospCode;
            entity.HospDepCode  = vm.HospDepCode;
            entity.AssetHospPos = vm.AssetHospPos;
            entity.WareCode     = vm.WareCode;
            entity.WareRack     = vm.WareRack;
            entity.WareEstante  = vm.WareEstante;
            entity.SiteCode     = vm.SiteCode;
            entity.ModelCode    = vm.ModelCode;
            entity.AssetSN      = vm.AssetSN;
            entity.LicCode      = vm.LicCode;
            entity.AssetNum     = vm.AssetNum;
            entity.AssetDslic   = vm.AssetDslic;
            entity.AssetDelic   = vm.AssetDelic;
            entity.AssetGnum    = vm.AssetGnum;
            entity.AssetDjequip = vm.AssetDjequip;
            entity.AssetAddata  = vm.AssetAddata;
            entity.AssetDmod        = DateOnly.FromDateTime(DateTime.Today);
            entity.Active           = vm.Active;
            entity.AssetStatus      = vm.AssetStatus;
            entity.ResponsibleUser  = vm.ResponsibleUser;
            entity.ResponsibleExt   = vm.ResponsibleExt;
            entity.AcquireCost      = vm.AcquireCost;
            entity.AcquireDate      = vm.AcquireDate;
            entity.DepreYears       = vm.DepreYears;
            entity.NetHostname      = vm.NetHostname;
            entity.NetInDomain  = vm.NetInDomain;
            entity.NetEnabled   = vm.NetEnabled;
            entity.NetIp        = newNetIp;
            entity.NetType      = newNetType;

            if (locationChanged)
                await RegisterAssetHistory(entity, vm.HistNotes, GetSvcCurrentUserId(), "UBICACION");
            else if (netChanged)
                await RegisterAssetHistory(entity, vm.HistNotes, GetSvcCurrentUserId(), "RED");
            else if (dataChanged)
                await RegisterAssetHistory(entity, vm.HistNotes, GetSvcCurrentUserId(), "DATOS");

            await _context.SaveChangesAsync();
            TempData["Success"] = "Activo clinico actualizado correctamente.";
            return RedirectToAction(nameof(AssetsOnSite));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcManage)]
        public async Task<IActionResult> AssetToggle(int id)
        {
            var entity = await _context.EngAssets.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            entity.AssetDmod = DateOnly.FromDateTime(DateTime.Today);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Activo {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(AssetsOnSite));
        }

        public async Task<IActionResult> CheckAssetSerial(string sn, int? excludeId = null)
        {
            var exists = await _context.EngAssets
                .AnyAsync(a => a.AssetSN == sn.Trim() && a.AssetCode != (excludeId ?? 0));
            return Json(new { exists });
        }

        // ===================== HELPERS ACTIVOS =====================

        private void ValidateAssetLocationFields(EngAssetViewModel vm)
        {
            if (vm.HospCode.HasValue)
            {
                if (!vm.HospDepCode.HasValue)
                    ModelState.AddModelError("HospDepCode", "La zona es requerida cuando se selecciona un hospital.");
                if (string.IsNullOrWhiteSpace(vm.AssetHospPos))
                    ModelState.AddModelError("AssetHospPos", "La posicion es requerida cuando se selecciona un hospital.");
            }
            if (vm.WareCode.HasValue)
            {
                if (string.IsNullOrWhiteSpace(vm.WareRack))
                    ModelState.AddModelError("WareRack", "El rack es requerido cuando se selecciona una bodega.");
                if (string.IsNullOrWhiteSpace(vm.WareEstante))
                    ModelState.AddModelError("WareEstante", "El estante es requerido cuando se selecciona una bodega.");
            }
        }

        private async Task RegisterAssetHistory(EngAsset asset, string? notes, int? userId, string histType = "UBICACION")
        {
            string locType = asset.HospCode.HasValue ? "HOSPITAL"
                           : asset.WareCode.HasValue ? "BODEGA"
                           : asset.SiteCode.HasValue ? "SEDE"
                           : "SIN_UB";

            _context.EngAssetHistories.Add(new EngAssetHistory
            {
                AssetCode    = asset.AssetCode,
                HistDate     = DateTime.Now,
                UserCode     = userId,
                LocType      = locType,
                WareCode     = asset.WareCode,
                WareRack     = asset.WareRack,
                WareEstante  = asset.WareEstante,
                HospCode     = asset.HospCode,
                HospDepCode  = asset.HospDepCode,
                AssetHospPos = asset.AssetHospPos,
                SiteCode     = asset.SiteCode,
                HistNotes    = notes,
                HistType     = histType,
                NetHostname  = asset.NetHostname,
                NetInDomain  = asset.NetInDomain,
                NetEnabled   = asset.NetEnabled,
                NetIp        = asset.NetIp,
                NetType      = asset.NetType,
            });
            await _context.SaveChangesAsync();
        }

        private int? GetSvcCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        // ===================== MOVIMIENTOS DE ACTIVOS CLÍNICOS =====================

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> AssetWithdraw(int id)
        {
            var asset = await _context.EngAssets.FindAsync(id);
            if (asset == null) return NotFound();

            return View(new EDInventory.Models.ViewModels.EquipMovementVM
            {
                EntityCode = id,
                EntityDesc = asset.AssetDesc,
                EntityType = "ASSET",
                Hospitals  = await GetHospitalList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> AssetWithdraw(EDInventory.Models.ViewModels.EquipMovementVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Hospitals = await GetHospitalList();
                return View(vm);
            }

            _context.EngAssetMovements.Add(new EngAssetMovement
            {
                AssetCode = vm.EntityCode,
                UserCode  = GetSvcCurrentUserId(),
                MovDate   = DateTime.Now,
                MovType   = vm.MovType,
                HospCode  = vm.HospCode,
                MovDest   = vm.MovDest,
                MovNotes  = vm.MovNotes
            });
            await _context.SaveChangesAsync();

            var label = vm.MovType == "RETIRO" ? "Retiro" : "Devolución";
            TempData["Success"] = $"{label} registrado correctamente.";
            return RedirectToAction(nameof(AssetDetail), new { id = vm.EntityCode });
        }

        private async Task<IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetHospitalList() =>
            await _context.Hospitals
                .Where(h => h.Active).OrderBy(h => h.HospName)
                .Select(h => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(h.HospName, h.HospCode.ToString()))
                .ToListAsync();

        // ===================== MANTENIMIENTOS PREVENTIVOS =====================

        /// <summary>
        /// Muestra todos los mantenimientos de activos clínicos con estado PENDIENTE,
        /// ordenados por fecha programada. Los vencidos se resaltan en la vista.
        /// </summary>
        public async Task<IActionResult> MaintenanceDue()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var pending = await _context.EngMaintenances
                .Include(m => m.Asset)
                    .ThenInclude(a => a!.Hospital)
                .Include(m => m.User)
                    .ThenInclude(u => u!.Employee)
                .Where(m => m.MaintStatus == "PENDIENTE")
                .OrderBy(m => m.MaintScheduled)
                .ToListAsync();

            ViewBag.Today = today;
            return View(pending);
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> MaintCreate(int assetCode)
        {
            var asset = await _context.EngAssets.FindAsync(assetCode);
            if (asset == null) return NotFound();

            var technicians = await _context.Users
                .Include(u => u.Employee)
                .Where(u => u.Active)
                .OrderBy(u => u.Employee!.EmpSurname)
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
                    u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                    u.UserCode.ToString()))
                .ToListAsync();

            return View(new EDInventory.Models.ViewModels.MaintCreateVM
            {
                AssetCode  = assetCode,
                AssetDesc  = asset.AssetDesc,
                Technicians = technicians
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> MaintCreate(EDInventory.Models.ViewModels.MaintCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                var asset = await _context.EngAssets.FindAsync(vm.AssetCode);
                vm.AssetDesc = asset?.AssetDesc;
                vm.Technicians = await _context.Users
                    .Include(u => u.Employee).Where(u => u.Active)
                    .OrderBy(u => u.Employee!.EmpSurname)
                    .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
                        u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                        u.UserCode.ToString()))
                    .ToListAsync();
                return View(vm);
            }

            _context.EngMaintenances.Add(new EngMaintenance
            {
                AssetCode      = vm.AssetCode,
                UserCode       = vm.UserCode,
                MaintType      = vm.MaintType,
                MaintStatus    = "PENDIENTE",
                MaintScheduled = vm.MaintScheduled,
                MaintNotes     = vm.MaintNotes,
                CreatedDate    = DateTime.Now,
                CreatedBy      = GetSvcCurrentUserId()
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mantenimiento programado correctamente.";
            return RedirectToAction(nameof(AssetDetail), new { id = vm.AssetCode });
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> MaintComplete(int id)
        {
            var maint = await _context.EngMaintenances
                .Include(m => m.Asset)
                .FirstOrDefaultAsync(m => m.MaintCode == id);
            if (maint == null) return NotFound();

            return View(new EDInventory.Models.ViewModels.MaintCompleteVM
            {
                MaintCode      = maint.MaintCode,
                AssetCode      = maint.AssetCode,
                AssetDesc      = maint.Asset?.AssetDesc,
                MaintType      = maint.MaintType,
                MaintScheduled = maint.MaintScheduled
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> MaintComplete(EDInventory.Models.ViewModels.MaintCompleteVM vm)
        {
            var maint = await _context.EngMaintenances.FindAsync(vm.MaintCode);
            if (maint == null) return NotFound();

            maint.MaintStatus    = "COMPLETADO";
            maint.MaintCompleted = vm.MaintCompleted;
            maint.MaintResult    = vm.MaintResult;
            _context.EngMaintenances.Update(maint);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mantenimiento marcado como completado.";
            return RedirectToAction(nameof(AssetDetail), new { id = maint.AssetCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> MaintCancel(int id)
        {
            var maint = await _context.EngMaintenances.FindAsync(id);
            if (maint == null) return NotFound();

            maint.MaintStatus = "CANCELADO";
            _context.EngMaintenances.Update(maint);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mantenimiento cancelado.";
            return RedirectToAction(nameof(AssetDetail), new { id = maint.AssetCode });
        }

        private async Task PopulateAssetSelectLists(EngAssetViewModel vm)
        {
            vm.Lines = (await _context.EngLines.Where(l => l.Active).OrderBy(l => l.LineName).ToListAsync())
                .Select(l => new SelectListItem(l.LineName, l.LineCode.ToString()));

            vm.Sites = (await _context.Sites.Where(s => s.Active).OrderBy(s => s.SiteName).ToListAsync())
                .Select(s => new SelectListItem(s.SiteName, s.SiteCode.ToString()));

            vm.Hospitals = (await _context.Hospitals.Where(h => h.Active).OrderBy(h => h.HospName).ToListAsync())
                .Select(h => new SelectListItem(h.HospName, h.HospCode.ToString()));

            vm.HospDepartments = vm.HospCode.HasValue
                ? (await _context.HospitalDepartments
                    .Where(d => d.HospCode == vm.HospCode && d.Active)
                    .OrderBy(d => d.HospDepName).ToListAsync())
                    .Select(d => new SelectListItem(d.HospDepName, d.HospDepCode.ToString()))
                : [];

            vm.Warehouses = (await _context.Warehouses.Where(w => w.Active).OrderBy(w => w.WareName).ToListAsync())
                .Select(w => new SelectListItem(w.WareName, w.WareCode.ToString()));

            vm.Models = (await _context.Models
                .Include(m => m.Brand).ThenInclude(b => b!.AssetType)
                .Where(m => m.Active).ToListAsync())
                .Select(m => new SelectListItem(
                    $"{m.ModelDesc} - {m.Brand?.BrandDesc} ({m.Brand?.AssetType?.AssetsDesc})",
                    m.ModelCode.ToString()));

            vm.Licitaciones = (await _context.Licitaciones.Where(l => l.Active).OrderBy(l => l.LicNum).ToListAsync())
                .Select(l => new SelectListItem($"{l.LicNum} - {l.LicDesc}", l.LicCode.ToString()));

            vm.TechUsers = (await _context.Users
                .Include(u => u.Employee)
                .Where(u => u.Active)
                .OrderBy(u => u.Employee!.EmpSurname)
                .ToListAsync())
                .Select(u => new SelectListItem(
                    u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                    u.UserCode.ToString()));
        }

        // ===================== REPUESTOS VINCULADOS A ACTIVO =====================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> AssetPartAdd(int assetCode, int partCode, string? notes)
        {
            var exists = await _context.AssetParts
                .AnyAsync(ap => ap.AssetCode == assetCode && ap.PartCode == partCode);
            if (!exists)
            {
                _context.AssetParts.Add(new AssetPart
                {
                    AssetCode = assetCode,
                    PartCode  = partCode,
                    ApNotes   = notes
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Repuesto vinculado al activo.";
            }
            else
            {
                TempData["Warning"] = "Este repuesto ya estaba vinculado.";
            }
            return RedirectToAction(nameof(AssetDetail), new { id = assetCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
        public async Task<IActionResult> AssetPartRemove(int apCode, int assetCode)
        {
            var link = await _context.AssetParts.FindAsync(apCode);
            if (link != null)
            {
                _context.AssetParts.Remove(link);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Repuesto desvinculado del activo.";
            }
            return RedirectToAction(nameof(AssetDetail), new { id = assetCode });
        }

        // ===================== BÚSQUEDA GLOBAL =====================

        /// <summary>
        /// Busca repuestos de ingeniería por nombre, referencia interna o referencia del fabricante.
        /// Retorna una vista con los resultados coincidentes con enlace directo a la caja del repuesto.
        /// </summary>
        public async Task<IActionResult> Search(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View(new List<EngPart>());

            var results = await _context.EngParts
                .Include(p => p.Box)
                    .ThenInclude(b => b!.Line)
                .Where(p => p.PartName!.Contains(q) || p.PartRef!.Contains(q) || p.PartMfrRef!.Contains(q))
                .OrderBy(p => p.Box!.Line!.LineName)
                .ThenBy(p => p.Box!.BoxNumber)
                .ToListAsync();

            ViewBag.Query = q;
            return View(results);
        }
    }
}
