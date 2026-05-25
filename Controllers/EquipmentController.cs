using ClosedXML.Excel;
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
    /// Controlador de inventario de equipos IT (división TI). Requiere rol <c>TiRead</c> como mínimo.
    /// Gestiona: inventario de equipos (<see cref="EDInventory.Models.Entities.ItEquip"/>),
    /// licitaciones, bodegas, importación/exportación Excel, etiquetas QR,
    /// estación de movimiento por escaneo de serial, y mantenimientos programados.
    /// Las acciones de escritura requieren <c>TiWrite</c> o superior.
    /// </summary>
    [Authorize(Roles = AppRoles.TiRead)]
    public class EquipmentController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>Inicializa el controlador con el contexto de base de datos.</summary>
        public EquipmentController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== INVENTARIO =====================

        /// <summary>
        /// Muestra el inventario de equipos IT paginado con filtros por sede, hospital, bodega,
        /// estado activo/inactivo y equipos sin licitación asignada.
        /// </summary>
        public async Task<IActionResult> Index(string? search, int? siteId, int? hospId, int? wareId, bool? active, bool? sinLicitacion, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.ItEquips
                .Include(e => e.Site)
                .Include(e => e.Hospital)
                .Include(e => e.HospitalDepartment)
                .Include(e => e.Warehouse)
                .Include(e => e.Model)
                    .ThenInclude(m => m!.Brand)
                        .ThenInclude(b => b!.AssetType)
                .Include(e => e.Licitacion)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e =>
                    e.ItequipDesc!.Contains(search) ||
                    e.ItequipSn!.Contains(search) ||
                    e.ItequipNum!.Contains(search));

            if (siteId.HasValue)
                query = query.Where(e => e.SiteCode == siteId);

            if (hospId.HasValue)
                query = query.Where(e => e.HospCode == hospId);

            if (wareId.HasValue)
                query = query.Where(e => e.WareCode == wareId);

            if (active.HasValue)
                query = query.Where(e => e.Active == active);

            if (sinLicitacion == true)
                query = query.Where(e => e.LicCode == null);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            ViewBag.Search         = search;
            ViewBag.SiteId         = siteId;
            ViewBag.HospId         = hospId;
            ViewBag.WareId         = wareId;
            ViewBag.Active         = active;
            ViewBag.SinLicitacion  = sinLicitacion;
            ViewBag.Page      = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total     = total;
            ViewBag.Sites = new SelectList(
                await _context.Sites.Where(s => s.Active).OrderBy(s => s.SiteName).ToListAsync(),
                "SiteCode", "SiteName");
            ViewBag.Hospitals = new SelectList(
                await _context.Hospitals.Where(h => h.Active).OrderBy(h => h.HospName).ToListAsync(),
                "HospCode", "HospName");
            ViewBag.Warehouses = new SelectList(
                await _context.Warehouses.Where(w => w.Active).OrderBy(w => w.WareName).ToListAsync(),
                "WareCode", "WareName");

            var items = await query
                .OrderByDescending(e => e.ItequipDnew)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        /// <summary>
        /// Genera y descarga un archivo Excel (.xlsx) con el inventario IT filtrado,
        /// respetando los mismos parámetros de filtro que <see cref="Index"/>.
        /// El archivo incluye estado de licitación con color de fila.
        /// </summary>
        public async Task<IActionResult> ExportExcel(string? search, int? siteId, int? hospId, int? wareId, bool? active, bool? sinLicitacion)
        {
            var query = _context.ItEquips
                .Include(e => e.Site)
                .Include(e => e.Hospital)
                .Include(e => e.HospitalDepartment)
                .Include(e => e.Warehouse)
                .Include(e => e.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                .Include(e => e.Licitacion)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.ItequipDesc!.Contains(search) || e.ItequipSn!.Contains(search) || e.ItequipNum!.Contains(search));
            if (siteId.HasValue)  query = query.Where(e => e.SiteCode == siteId);
            if (hospId.HasValue)  query = query.Where(e => e.HospCode == hospId);
            if (wareId.HasValue)  query = query.Where(e => e.WareCode == wareId);
            if (active.HasValue)        query = query.Where(e => e.Active == active);
            if (sinLicitacion == true)  query = query.Where(e => e.LicCode == null);

            var items = await query.OrderByDescending(e => e.ItequipDnew).ToListAsync();
            var hoy   = DateOnly.FromDateTime(DateTime.Today);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Inventario IT");

            // Encabezados
            string[] headers = ["#", "Descripcion", "Tipo de Activo", "Marca", "Modelo",
                "Serie", "Num. Equipo", "Garantia", "Ubicacion", "Detalle Ubicacion",
                "Licitacion", "Fin Licitacion", "Estado Lic.", "Activo"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#212529");
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Datos
            int row = 2;
            foreach (var eq in items)
            {
                var licExpired   = eq.Licitacion?.LicEnd.HasValue == true && eq.Licitacion.LicEnd < hoy;
                var licPostponed = licExpired && eq.Licitacion?.LicPostponed == true;
                string licEstado = !eq.Licitacion?.LicEnd.HasValue == true ? "Sin fecha"
                    : licPostponed  ? "Aplazada"
                    : licExpired    ? "Vencida"
                    : eq.Licitacion!.LicEnd <= DateOnly.FromDateTime(DateTime.Today.AddDays(eq.Licitacion.LicWarnDays)) ? "Por vencer"
                    : "Vigente";

                string ubicacion = eq.WareCode.HasValue ? "Bodega"
                    : eq.HospCode.HasValue ? "Hospital"
                    : eq.SiteCode.HasValue ? "Sede" : "";
                string detUbicacion = eq.WareCode.HasValue
                    ? $"{eq.Warehouse?.WareName} R:{eq.WareRack} E:{eq.WareEstante}"
                    : eq.HospCode.HasValue
                    ? $"{eq.Hospital?.HospName} - {eq.HospitalDepartment?.HospDepName}"
                    : eq.Site?.SiteName ?? "";

                ws.Cell(row, 1).Value  = eq.ItequipCode;
                ws.Cell(row, 2).Value  = eq.ItequipDesc ?? "";
                ws.Cell(row, 3).Value  = eq.Model?.Brand?.AssetType?.AssetsDesc ?? "";
                ws.Cell(row, 4).Value  = eq.Model?.Brand?.BrandDesc ?? "";
                ws.Cell(row, 5).Value  = eq.Model?.ModelDesc ?? "";
                ws.Cell(row, 6).Value  = eq.ItequipSn ?? "";
                ws.Cell(row, 7).Value  = eq.ItequipNum ?? "";
                ws.Cell(row, 8).Value  = eq.ItequipGnum ?? "";
                ws.Cell(row, 9).Value  = ubicacion;
                ws.Cell(row, 10).Value = detUbicacion;
                ws.Cell(row, 11).Value = eq.Licitacion != null ? $"{eq.Licitacion.LicNum} - {eq.Licitacion.LicDesc}" : "";
                ws.Cell(row, 12).Value = eq.ItequipDelic?.ToString("dd/MM/yyyy") ?? "";
                ws.Cell(row, 13).Value = licEstado;
                ws.Cell(row, 14).Value = eq.Active ? "Activo" : "Inactivo";

                // Color por estado de licitación
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

            var filename = $"Inventario_IT_{DateTime.Today:yyyyMMdd}.xlsx";
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var equip = await _context.ItEquips
                .Include(e => e.Site)
                .Include(e => e.Hospital)
                .Include(e => e.HospitalDepartment)
                .Include(e => e.Warehouse)
                .Include(e => e.Model)
                    .ThenInclude(m => m!.Brand)
                        .ThenInclude(b => b!.AssetType)
                            .ThenInclude(a => a!.GenAssetType)
                .Include(e => e.Licitacion)
                .Include(e => e.Details)
                .Include(e => e.History.OrderByDescending(h => h.HistDate))
                    .ThenInclude(h => h.User)
                .Include(e => e.History)
                    .ThenInclude(h => h.Employee)
                .Include(e => e.History)
                    .ThenInclude(h => h.Hospital)
                .Include(e => e.History)
                    .ThenInclude(h => h.HospitalDepartment)
                .Include(e => e.History)
                    .ThenInclude(h => h.Warehouse)
                .Include(e => e.History)
                    .ThenInclude(h => h.Site)
                .FirstOrDefaultAsync(e => e.ItequipCode == id);

            if (equip == null) return NotFound();

            ViewBag.EquipMovements = await _context.ItEquipMovements
                .Include(m => m.User).ThenInclude(u => u!.Employee)
                .Include(m => m.Hospital)
                .Where(m => m.ItequipCode == id)
                .OrderByDescending(m => m.MovDate)
                .ToListAsync();

            ViewBag.Maintenances = await _context.ItEquipMaintenances
                .Include(m => m.User).ThenInclude(u => u!.Employee)
                .Where(m => m.ItequipCode == id)
                .OrderByDescending(m => m.MaintScheduled)
                .ToListAsync();

            return View(equip);
        }

        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> Create()
        {
            var vm = new ItEquipViewModel
            {
                ItequipDnew = DateOnly.FromDateTime(DateTime.Today),
                Active = true
            };
            await PopulateSelectLists(vm);
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
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> Create(ItEquipViewModel vm)
        {
            ValidateLocationFields(vm);

            if (!string.IsNullOrWhiteSpace(vm.ItequipSn) &&
                await _context.ItEquips.AnyAsync(e => e.ItequipSn == vm.ItequipSn.Trim()))
                ModelState.AddModelError(nameof(vm.ItequipSn), "Este numero de serie ya esta registrado.");

            if (!ModelState.IsValid)
            {
                await PopulateSelectLists(vm);
                return View(vm);
            }

            var equip = new ItEquip
            {
                ItequipDesc = vm.ItequipDesc,
                SiteCode = vm.SiteCode,
                HospCode = vm.HospCode,
                HospDepCode = vm.HospDepCode,
                ItequipHospPos = vm.ItequipHospPos,
                WareCode = vm.WareCode,
                WareRack = vm.WareRack,
                WareEstante = vm.WareEstante,
                WareCaja = vm.WareCaja,
                ModelCode = vm.ModelCode,
                ItequipSn = vm.ItequipSn,
                LicCode = vm.LicCode,
                ItequipNum = vm.ItequipNum,
                ItequipDslic = vm.ItequipDslic,
                ItequipDelic = vm.ItequipDelic,
                ItequipGnum = vm.ItequipGnum,
                ItequipDjequip = vm.ItequipDjequip,
                ItequipAddata = vm.ItequipAddata,
                ItequipDnew = DateOnly.FromDateTime(DateTime.Today),
                ItequipDmod = DateOnly.FromDateTime(DateTime.Today),
                Active = vm.Active
            };

            // Transacción: el equipo y su historial inicial deben persistir juntos.
            // Si falla el historial después de guardado el equipo, se revierte todo.
            await using var tx = await _context.Database.BeginTransactionAsync();
            _context.ItEquips.Add(equip);
            await _context.SaveChangesAsync();                                      // obtiene el PK
            await RegisterHistory(equip, vm.HistNotes, GetCurrentUserId());         // usa el PK
            await tx.CommitAsync();

            TempData["Success"] = "Equipo registrado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.ItEquips.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = new ItEquipViewModel
            {
                ItequipCode = entity.ItequipCode,
                ItequipDesc = entity.ItequipDesc ?? string.Empty,
                SiteCode = entity.SiteCode,
                HospCode = entity.HospCode,
                HospDepCode = entity.HospDepCode,
                ItequipHospPos = entity.ItequipHospPos,
                WareCode = entity.WareCode,
                WareRack = entity.WareRack,
                WareEstante = entity.WareEstante,
                WareCaja = entity.WareCaja,
                ModelCode = entity.ModelCode,
                ItequipSn = entity.ItequipSn,
                LicCode = entity.LicCode,
                ItequipNum = entity.ItequipNum,
                ItequipDslic = entity.ItequipDslic,
                ItequipDelic = entity.ItequipDelic,
                ItequipGnum = entity.ItequipGnum,
                ItequipDjequip = entity.ItequipDjequip,
                ItequipAddata = entity.ItequipAddata,
                ItequipDnew = entity.ItequipDnew,
                ItequipDmod = entity.ItequipDmod,
                Active = entity.Active
            };
            await PopulateSelectLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> Edit(ItEquipViewModel vm)
        {
            ValidateLocationFields(vm);

            if (!string.IsNullOrWhiteSpace(vm.ItequipSn) &&
                await _context.ItEquips.AnyAsync(e => e.ItequipSn == vm.ItequipSn.Trim() && e.ItequipCode != vm.ItequipCode))
                ModelState.AddModelError(nameof(vm.ItequipSn), "Este numero de serie ya esta registrado.");

            if (!ModelState.IsValid)
            {
                await PopulateSelectLists(vm);
                return View(vm);
            }

            var entity = await _context.ItEquips.FindAsync(vm.ItequipCode);
            if (entity == null) return NotFound();

            bool locationChanged =
                entity.SiteCode != vm.SiteCode ||
                entity.HospCode != vm.HospCode ||
                entity.HospDepCode != vm.HospDepCode ||
                entity.ItequipHospPos != vm.ItequipHospPos ||
                entity.WareCode != vm.WareCode ||
                entity.WareRack != vm.WareRack ||
                entity.WareEstante != vm.WareEstante ||
                entity.WareCaja != vm.WareCaja;

            entity.ItequipDesc = vm.ItequipDesc;
            entity.SiteCode = vm.SiteCode;
            entity.HospCode = vm.HospCode;
            entity.HospDepCode = vm.HospDepCode;
            entity.ItequipHospPos = vm.ItequipHospPos;
            entity.WareCode = vm.WareCode;
            entity.WareRack = vm.WareRack;
            entity.WareEstante = vm.WareEstante;
            entity.WareCaja = vm.WareCaja;
            entity.ModelCode = vm.ModelCode;
            entity.ItequipSn = vm.ItequipSn;
            entity.LicCode = vm.LicCode;
            entity.ItequipNum = vm.ItequipNum;
            entity.ItequipDslic = vm.ItequipDslic;
            entity.ItequipDelic = vm.ItequipDelic;
            entity.ItequipGnum = vm.ItequipGnum;
            entity.ItequipDjequip = vm.ItequipDjequip;
            entity.ItequipAddata = vm.ItequipAddata;
            entity.ItequipDmod = DateOnly.FromDateTime(DateTime.Today);
            entity.Active = vm.Active;

            if (locationChanged)
                await RegisterHistory(entity, vm.HistNotes, GetCurrentUserId());

            await _context.SaveChangesAsync();
            TempData["Success"] = "Equipo actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.TiManage)]
        public async Task<IActionResult> Toggle(int id)
        {
            var entity = await _context.ItEquips.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            entity.ItequipDmod = DateOnly.FromDateTime(DateTime.Today);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Equipo {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Index));
        }

        // ===================== LICITACIONES (movidas a GestionController) =====================

        public IActionResult Licitaciones()    => RedirectToAction("Licitaciones",    "Gestion");
        public IActionResult LicitacionCreate() => RedirectToAction("LicitacionCreate","Gestion");
        public IActionResult LicitacionEdit(int id) => RedirectToAction("LicitacionEdit","Gestion", new { id });

        public async Task<IActionResult> ExportLicitaciones()
        {
            var list = await _context.Licitaciones
                .Include(l => l.ItEquips)
                .OrderBy(l => l.LicNum)
                .ToListAsync();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Licitaciones");

            string[] headers = ["#", "Numero", "Descripcion", "Inicio", "Vencimiento",
                "Dias Aviso", "Equipos Activos", "Estado", "Aplazada", "Nota Aplazamiento"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#212529");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = 2;
            foreach (var lic in list)
            {
                var vencida   = lic.LicEnd.HasValue && lic.LicEnd < hoy;
                var aplazada  = vencida && lic.LicPostponed;
                var porVencer = lic.LicEnd.HasValue && !vencida &&
                                lic.LicEnd <= DateOnly.FromDateTime(DateTime.Today.AddDays(lic.LicWarnDays));
                string estado = !lic.LicEnd.HasValue ? "Sin fecha"
                    : aplazada   ? "Aplazada"
                    : vencida    ? "Vencida"
                    : porVencer  ? "Por vencer"
                    : "Vigente";

                ws.Cell(row, 1).Value  = lic.LicCode;
                ws.Cell(row, 2).Value  = lic.LicNum ?? "";
                ws.Cell(row, 3).Value  = lic.LicDesc ?? "";
                ws.Cell(row, 4).Value  = lic.LicStart?.ToString("dd/MM/yyyy") ?? "";
                ws.Cell(row, 5).Value  = lic.LicEnd?.ToString("dd/MM/yyyy") ?? "";
                ws.Cell(row, 6).Value  = lic.LicWarnDays;
                ws.Cell(row, 7).Value  = lic.ItEquips.Count(e => e.Active);
                ws.Cell(row, 8).Value  = estado;
                ws.Cell(row, 9).Value  = lic.LicPostponed ? "Si" : "No";
                ws.Cell(row, 10).Value = lic.LicPostponedNote ?? "";

                if (aplazada)        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
                else if (vencida)    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");
                else if (porVencer)  ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Licitaciones_{DateTime.Today:yyyyMMdd}.xlsx");
        }

        // ===================== BODEGAS (movidas a GestionController) =====================

        public IActionResult Warehouses()           => RedirectToAction("Warehouses",    "Gestion");
        public IActionResult WarehouseCreate()      => RedirectToAction("WarehouseCreate","Gestion");
        public IActionResult WarehouseEdit(int id)  => RedirectToAction("WarehouseEdit",  "Gestion", new { id });

        // ===================== IMPORTAR EXCEL =====================

        /// <summary>
        /// Genera y descarga la plantilla Excel (.xlsx) para la importación masiva de equipos IT.
        /// Requiere rol <c>TiWrite</c>.
        /// </summary>
        [Authorize(Roles = AppRoles.TiWrite)]
        public IActionResult DownloadImportTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Plantilla");

            // Instrucciones en fila 1
            ws.Cell(1, 1).Value = "INSTRUCCIONES: Complete los campos a partir de la fila 3. Los campos con * son obligatorios. Marca y Modelo deben existir en el sistema.";
            ws.Range(1, 1, 1, 8).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");

            // Encabezados en fila 2
            string[] headers = ["Descripcion *", "Marca *", "Modelo *", "Num. Serie", "Num. Equipo", "Num. Garantia", "Num. Licitacion", "Datos Adicionales"];
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(2, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#212529");
                cell.Style.Font.FontColor = XLColor.White;
            }

            // Fila de ejemplo
            ws.Cell(3, 1).Value = "Computadora Escritorio Dell";
            ws.Cell(3, 2).Value = "Dell";
            ws.Cell(3, 3).Value = "OptiPlex 7090";
            ws.Cell(3, 4).Value = "SN-001234";
            ws.Cell(3, 5).Value = "EQ-0001";
            ws.Cell(3, 6).Value = "GAR-2024";
            ws.Cell(3, 7).Value = "LIC-2024-01";
            ws.Cell(3, 8).Value = "";
            ws.Row(3).Style.Fill.BackgroundColor = XLColor.FromHtml("#e8f4f8");

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Plantilla_Importar_Equipos.xlsx");
        }

        /// <summary>
        /// [POST] Procesa la importación masiva de equipos IT desde un archivo Excel.
        /// Requiere rol <c>TiWrite</c>. Inserta o actualiza equipos según el número de serie.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> ImportExcel(IFormFile? archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "Seleccione un archivo Excel (.xlsx) para importar.";
                return RedirectToAction(nameof(Index));
            }

            if (!archivo.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Solo se aceptan archivos .xlsx.";
                return RedirectToAction(nameof(Index));
            }

            // Cargar catálogos en memoria para lookup eficiente
            var brands = await _context.Brands
                .Include(b => b.Models)
                .ToListAsync();
            var licitaciones = await _context.Licitaciones
                .Where(l => l.Active)
                .ToListAsync();

            int importados = 0;
            var errores = new List<string>();

            XLWorkbook wb;
            IXLWorksheet ws;
            int lastRow;
            try
            {
                using var stream = archivo.OpenReadStream();
                wb = new XLWorkbook(stream);
                ws = wb.Worksheets.First();
                lastRow = ws.LastRowUsed()?.RowNumber() ?? 2;
            }
            catch
            {
                TempData["Error"] = "No se pudo leer el archivo Excel. Verifique que sea un archivo .xlsx valido y no este protegido.";
                return RedirectToAction(nameof(Index));
            }

            for (int r = 3; r <= lastRow; r++)
            {
                var desc   = ws.Cell(r, 1).GetString().Trim();
                var marca  = ws.Cell(r, 2).GetString().Trim();
                var modelo = ws.Cell(r, 3).GetString().Trim();

                if (string.IsNullOrWhiteSpace(desc) && string.IsNullOrWhiteSpace(marca))
                    continue; // fila vacía, se omite

                if (string.IsNullOrWhiteSpace(desc))
                {
                    errores.Add($"Fila {r}: Descripcion es obligatoria.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(marca) || string.IsNullOrWhiteSpace(modelo))
                {
                    errores.Add($"Fila {r} ({desc}): Marca y Modelo son obligatorios.");
                    continue;
                }

                // Lookup modelo
                var brandEntity = brands.FirstOrDefault(b =>
                    string.Equals(b.BrandDesc, marca, StringComparison.OrdinalIgnoreCase));
                if (brandEntity == null)
                {
                    errores.Add($"Fila {r} ({desc}): Marca '{marca}' no existe en el sistema.");
                    continue;
                }

                var modelEntity = brandEntity.Models?.FirstOrDefault(m =>
                    string.Equals(m.ModelDesc, modelo, StringComparison.OrdinalIgnoreCase));
                if (modelEntity == null)
                {
                    errores.Add($"Fila {r} ({desc}): Modelo '{modelo}' no existe bajo la marca '{marca}'.");
                    continue;
                }

                // Lookup licitacion (opcional)
                var licNum = ws.Cell(r, 7).GetString().Trim();
                int? licCode = null;
                if (!string.IsNullOrWhiteSpace(licNum))
                {
                    var licEntity = licitaciones.FirstOrDefault(l =>
                        string.Equals(l.LicNum, licNum, StringComparison.OrdinalIgnoreCase));
                    if (licEntity == null)
                    {
                        errores.Add($"Fila {r} ({desc}): Licitacion '{licNum}' no existe o no esta activa. El equipo fue importado sin licitacion.");
                    }
                    else
                    {
                        licCode = licEntity.LicCode;
                    }
                }

                var sn = ws.Cell(r, 4).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(sn) &&
                    await _context.ItEquips.AnyAsync(e => e.ItequipSn == sn))
                {
                    errores.Add($"Fila {r} ({desc}): El numero de serie '{sn}' ya existe. Fila omitida.");
                    continue;
                }

                string? nullOrVal(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

                var equip = new ItEquip
                {
                    ItequipDesc   = desc,
                    ModelCode     = modelEntity.ModelCode,
                    ItequipSn     = string.IsNullOrWhiteSpace(sn) ? null : sn,
                    ItequipNum    = nullOrVal(ws.Cell(r, 5).GetString().Trim()),
                    ItequipGnum   = nullOrVal(ws.Cell(r, 6).GetString().Trim()),
                    LicCode       = licCode,
                    ItequipAddata = nullOrVal(ws.Cell(r, 8).GetString().Trim()),
                    ItequipDnew   = DateOnly.FromDateTime(DateTime.Today),
                    ItequipDmod   = DateOnly.FromDateTime(DateTime.Today),
                    Active        = true
                };

                await using var txImp = await _context.Database.BeginTransactionAsync();
                _context.ItEquips.Add(equip);
                await _context.SaveChangesAsync();
                await RegisterHistory(equip, "Importado desde Excel", GetCurrentUserId());
                await txImp.CommitAsync();

                importados++;
            }

            if (importados > 0)
                TempData["Success"] = $"{importados} equipo(s) importados correctamente." +
                    (errores.Count > 0 ? $" {errores.Count} fila(s) con advertencias." : "");
            else
                TempData["Error"] = "No se importo ningun equipo.";

            if (errores.Count > 0)
                TempData["ImportErrors"] = string.Join("|", errores.Take(10));

            return RedirectToAction(nameof(Index));
        }

        // ===================== ETIQUETA =====================

        /// <summary>
        /// Muestra la vista de etiqueta imprimible para el equipo IT indicado por <paramref name="id"/>.
        /// Incluye QR con el número de serie y el historial de ubicaciones recientes.
        /// </summary>
        public async Task<IActionResult> Label(int id)
        {
            var equip = await _context.ItEquips
                .Include(e => e.Model)
                    .ThenInclude(m => m!.Brand)
                        .ThenInclude(b => b!.AssetType)
                .Include(e => e.Hospital)
                .Include(e => e.HospitalDepartment)
                .Include(e => e.Warehouse)
                .Include(e => e.Site)
                    .ThenInclude(s => s!.Company)
                .Include(e => e.History.OrderByDescending(h => h.HistDate))
                    .ThenInclude(h => h.Warehouse)
                .Include(e => e.History.OrderByDescending(h => h.HistDate))
                    .ThenInclude(h => h.Hospital)
                .Include(e => e.History.OrderByDescending(h => h.HistDate))
                    .ThenInclude(h => h.HospitalDepartment)
                .FirstOrDefaultAsync(e => e.ItequipCode == id);

            if (equip == null) return NotFound();

            // Fecha de salida de bodega = ultima vez que fue asignado a hospital
            var lastHospMove = equip.History
                .OrderByDescending(h => h.HistDate)
                .FirstOrDefault(h => h.LocType == "HOSPITAL");

            // Empresa: del site actual, o la primera activa del sistema como fallback
            var company = equip.Site?.Company
                ?? await _context.Companies
                    .Where(c => c.Active)
                    .OrderBy(c => c.CompanyCode)
                    .FirstOrDefaultAsync();

            ViewBag.LastHospMove = lastHospMove;
            ViewBag.Company = company;

            return View(equip);
        }

        // ===================== ESTACION DE MOVIMIENTO =====================

        /// <summary>
        /// Muestra la estación de movimiento masivo por escaneo de número de serie.
        /// Permite reubicar equipos IT escaneando su serial y seleccionando destino (Bodega u Hospital).
        /// Requiere rol <c>TiWrite</c>.
        /// </summary>
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> MovementStation()
        {
            ViewBag.Warehouses = await _context.Warehouses
                .Where(w => w.Active).OrderBy(w => w.WareName).ToListAsync();
            ViewBag.Hospitals = await _context.Hospitals
                .Where(h => h.Active).OrderBy(h => h.HospName).ToListAsync();
            return View();
        }

        /// <summary>
        /// [POST JSON] Procesa el escaneo de un número de serie y reubica el equipo IT.
        /// Recibe un <see cref="ScanMoveRequest"/> vía JSON; retorna JSON con <c>success</c> y mensaje.
        /// Requiere rol <c>TiWrite</c>.
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> ProcessScan([FromBody] ScanMoveRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Serial))
                return Json(new { success = false, message = "Serial vacio." });

            var equip = await _context.ItEquips
                .Include(e => e.Hospital)
                .Include(e => e.HospitalDepartment)
                .Include(e => e.Warehouse)
                .Include(e => e.Site)
                .FirstOrDefaultAsync(e => e.ItequipSn == req.Serial.Trim() && e.Active);

            if (equip == null)
                return Json(new { success = false, message = $"No se encontro equipo activo con serial: {req.Serial}" });

            // Descripcion de ubicacion anterior
            string prevLocation = equip.WareCode.HasValue
                ? $"Bodega: {equip.Warehouse?.WareName} / {equip.WareRack} / {equip.WareEstante}"
                : equip.HospCode.HasValue
                    ? $"Hospital: {equip.Hospital?.HospName} / {equip.HospitalDepartment?.HospDepName} / {equip.ItequipHospPos}"
                    : equip.SiteCode.HasValue ? $"Sede: {equip.Site?.SiteName}" : "Sin ubicacion";

            // Aplicar nueva ubicacion
            equip.WareCode = null; equip.WareRack = null; equip.WareEstante = null; equip.WareCaja = null;
            equip.HospCode = null; equip.HospDepCode = null; equip.ItequipHospPos = null;
            equip.SiteCode = null;

            if (req.LocType == "BODEGA")
            {
                equip.WareCode = req.WareCode;
                equip.WareRack = req.WareRack;
                equip.WareEstante = req.WareEstante;
                equip.WareCaja = req.WareCaja;
            }
            else if (req.LocType == "HOSPITAL")
            {
                equip.HospCode = req.HospCode;
                equip.HospDepCode = req.HospDepCode;
                equip.ItequipHospPos = req.HospPos;
            }

            equip.ItequipDmod = DateOnly.FromDateTime(DateTime.Today);
            await RegisterHistory(equip, req.HistNotes ?? "Movimiento via escaner", GetCurrentUserId());
            await _context.SaveChangesAsync();

            // Descripcion de nueva ubicacion
            string newLocation = req.LocType == "BODEGA"
                ? $"Bodega / {req.WareRack} / {req.WareEstante}{(!string.IsNullOrWhiteSpace(req.WareCaja) ? $" / {req.WareCaja}" : "")}"
                : $"Hospital / {req.HospPos}";

            return Json(new
            {
                success = true,
                message = "Movimiento registrado.",
                equip = new
                {
                    desc = equip.ItequipDesc,
                    sn = equip.ItequipSn,
                    prevLocation,
                    newLocation
                }
            });
        }

        // ===================== AJAX =====================

        /// <summary>
        /// [GET JSON] Retorna la lista de departamentos activos del hospital indicado por <paramref name="hospCode"/>.
        /// Usado por los formularios de alta/edición para cargar los departamentos en cascada.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHospDepartments(int hospCode)
        {
            var deps = await _context.HospitalDepartments
                .Where(d => d.HospCode == hospCode && d.Active)
                .OrderBy(d => d.HospDepName)
                .Select(d => new { value = d.HospDepCode, text = d.HospDepName })
                .ToListAsync();
            return Json(deps);
        }

        public async Task<IActionResult> CheckSerial(string sn, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(sn)) return Json(new { exists = false });
            var exists = await _context.ItEquips
                .AnyAsync(e => e.ItequipSn == sn.Trim() && e.ItequipCode != (excludeId ?? 0));
            return Json(new { exists });
        }

        // ===================== HELPERS =====================

        private void ValidateLocationFields(ItEquipViewModel vm)
        {
            if (!vm.HospCode.HasValue && !vm.WareCode.HasValue && !vm.SiteCode.HasValue)
                ModelState.AddModelError("", "El equipo debe tener al menos una ubicacion asignada (Hospital, Bodega o Sede).");

            if (vm.HospCode.HasValue)
            {
                if (!vm.HospDepCode.HasValue)
                    ModelState.AddModelError("HospDepCode", "La zona es requerida cuando se selecciona un hospital.");
                if (string.IsNullOrWhiteSpace(vm.ItequipHospPos))
                    ModelState.AddModelError("ItequipHospPos", "La posicion es requerida cuando se selecciona un hospital.");
            }

            if (vm.WareCode.HasValue)
            {
                if (string.IsNullOrWhiteSpace(vm.WareRack))
                    ModelState.AddModelError("WareRack", "El rack es requerido cuando se selecciona una bodega.");
                if (string.IsNullOrWhiteSpace(vm.WareEstante))
                    ModelState.AddModelError("WareEstante", "El estante es requerido cuando se selecciona una bodega.");
            }
        }

        private async Task RegisterHistory(ItEquip equip, string? notes, int? userId)
        {
            string locType = equip.HospCode.HasValue ? "HOSPITAL"
                           : equip.WareCode.HasValue ? "BODEGA"
                           : equip.SiteCode.HasValue ? "SEDE"
                           : "SIN_UB";

            int? empCode = null;
            if (userId.HasValue)
            {
                var user = await _context.Users
                    .Where(u => u.UserCode == userId.Value)
                    .Select(u => new { u.EmpCode })
                    .FirstOrDefaultAsync();
                empCode = user?.EmpCode;
            }

            _context.ItEquipHistories.Add(new ItEquipHistory
            {
                ItequipCode = equip.ItequipCode,
                HistDate = DateTime.Now,
                UserCode = userId,
                EmpCode = empCode,
                LocType = locType,
                WareCode = equip.WareCode,
                WareRack = equip.WareRack,
                WareEstante = equip.WareEstante,
                WareCaja = equip.WareCaja,
                HospCode = equip.HospCode,
                HospDepCode = equip.HospDepCode,
                ItequipHospPos = equip.ItequipHospPos,
                SiteCode = equip.SiteCode,
                HistNotes = notes
            });
            await _context.SaveChangesAsync();
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        // ===================== MOVIMIENTOS DE EQUIPOS IT =====================

        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> EquipWithdraw(int id)
        {
            var equip = await _context.ItEquips.FindAsync(id);
            if (equip == null) return NotFound();

            return View(new EDInventory.Models.ViewModels.EquipMovementVM
            {
                EntityCode = id,
                EntityDesc = equip.ItequipDesc,
                EntityType = "ITEQUIP",
                Hospitals  = await _context.Hospitals
                    .Where(h => h.Active).OrderBy(h => h.HospName)
                    .Select(h => new SelectListItem(h.HospName, h.HospCode.ToString()))
                    .ToListAsync()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> EquipWithdraw(EDInventory.Models.ViewModels.EquipMovementVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Hospitals = await _context.Hospitals
                    .Where(h => h.Active).OrderBy(h => h.HospName)
                    .Select(h => new SelectListItem(h.HospName, h.HospCode.ToString()))
                    .ToListAsync();
                return View(vm);
            }

            _context.ItEquipMovements.Add(new ItEquipMovement
            {
                ItequipCode = vm.EntityCode,
                UserCode    = GetCurrentUserId(),
                MovDate     = DateTime.Now,
                MovType     = vm.MovType,
                HospCode    = vm.HospCode,
                MovDest     = vm.MovDest,
                MovNotes    = vm.MovNotes
            });
            await _context.SaveChangesAsync();

            var label = vm.MovType == "RETIRO" ? "Retiro" : "Devolución";
            TempData["Success"] = $"{label} registrado correctamente.";
            return RedirectToAction(nameof(Detail), new { id = vm.EntityCode });
        }

        // ===================== MANTENIMIENTOS EQUIPOS IT =====================

        /// <summary>Muestra todos los mantenimientos IT con estado PENDIENTE, ordenados por fecha programada.</summary>
        public async Task<IActionResult> MaintenanceDue()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var pending = await _context.ItEquipMaintenances
                .Include(m => m.ItEquip)
                    .ThenInclude(e => e!.Hospital)
                .Include(m => m.User)
                    .ThenInclude(u => u!.Employee)
                .Where(m => m.MaintStatus == "PENDIENTE")
                .OrderBy(m => m.MaintScheduled)
                .ToListAsync();

            ViewBag.Today = today;
            return View(pending);
        }

        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> MaintCreate(int equipCode)
        {
            var equip = await _context.ItEquips.FindAsync(equipCode);
            if (equip == null) return NotFound();

            return View(new EDInventory.Models.ViewModels.MaintCreateVM
            {
                AssetCode   = equipCode,
                AssetDesc   = equip.ItequipDesc,
                Technicians = await GetTechnicianList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> MaintCreate(EDInventory.Models.ViewModels.MaintCreateVM vm)
        {
            if (!ModelState.IsValid)
            {
                var equip = await _context.ItEquips.FindAsync(vm.AssetCode);
                vm.AssetDesc   = equip?.ItequipDesc;
                vm.Technicians = await GetTechnicianList();
                return View(vm);
            }

            _context.ItEquipMaintenances.Add(new ItEquipMaintenance
            {
                ItequipCode    = vm.AssetCode,
                UserCode       = vm.UserCode,
                MaintType      = vm.MaintType,
                MaintStatus    = "PENDIENTE",
                MaintScheduled = vm.MaintScheduled,
                MaintNotes     = vm.MaintNotes,
                CreatedDate    = DateTime.Now,
                CreatedBy      = GetCurrentUserId()
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mantenimiento programado correctamente.";
            return RedirectToAction(nameof(Detail), new { id = vm.AssetCode });
        }

        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> MaintComplete(int id)
        {
            var maint = await _context.ItEquipMaintenances
                .Include(m => m.ItEquip)
                .FirstOrDefaultAsync(m => m.MaintCode == id);
            if (maint == null) return NotFound();

            return View(new EDInventory.Models.ViewModels.MaintCompleteVM
            {
                MaintCode      = maint.MaintCode,
                AssetCode      = maint.ItequipCode,
                AssetDesc      = maint.ItEquip?.ItequipDesc,
                MaintType      = maint.MaintType,
                MaintScheduled = maint.MaintScheduled
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> MaintComplete(EDInventory.Models.ViewModels.MaintCompleteVM vm)
        {
            var maint = await _context.ItEquipMaintenances.FindAsync(vm.MaintCode);
            if (maint == null) return NotFound();

            maint.MaintStatus    = "COMPLETADO";
            maint.MaintCompleted = vm.MaintCompleted;
            maint.MaintResult    = vm.MaintResult;
            _context.ItEquipMaintenances.Update(maint);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mantenimiento marcado como completado.";
            return RedirectToAction(nameof(Detail), new { id = maint.ItequipCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.TiWrite)]
        public async Task<IActionResult> MaintCancel(int id)
        {
            var maint = await _context.ItEquipMaintenances.FindAsync(id);
            if (maint == null) return NotFound();

            maint.MaintStatus = "CANCELADO";
            _context.ItEquipMaintenances.Update(maint);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Mantenimiento cancelado.";
            return RedirectToAction(nameof(Detail), new { id = maint.ItequipCode });
        }

        private async Task<IEnumerable<SelectListItem>> GetTechnicianList() =>
            await _context.Users
                .Include(u => u.Employee)
                .Where(u => u.Active)
                .OrderBy(u => u.Employee!.EmpSurname)
                .Select(u => new SelectListItem(
                    u.Employee != null ? $"{u.Employee.EmpName} {u.Employee.EmpSurname}" : u.UserLogin,
                    u.UserCode.ToString()))
                .ToListAsync();

        private async Task PopulateSelectLists(ItEquipViewModel vm)
        {
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
        }
    }
}
