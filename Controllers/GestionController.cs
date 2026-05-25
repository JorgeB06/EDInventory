using ClosedXML.Excel;
using EDInventory.Data;
using EDInventory.Models;
using EDInventory.Models.Entities;
using EDInventory.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Controlador de gestión compartida entre TI y Servicio. Requiere rol <c>AdmRead</c>.
    /// Gestiona: Licitaciones/contratos (con exportación Excel) y Bodegas de almacenamiento.
    /// Ambas entidades son compartidas entre las divisiones TI y Servicio.
    /// Las acciones de escritura requieren <c>AdmWrite</c> o superior.
    /// </summary>
    [Authorize(Roles = AppRoles.AdmRead)]
    public class GestionController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>Inicializa el controlador con el contexto de base de datos.</summary>
        public GestionController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== LICITACIONES =====================

        /// <summary>Muestra el listado de licitaciones con el conteo de equipos IT vinculados.</summary>
        public async Task<IActionResult> Licitaciones()
        {
            var list = await _context.Licitaciones
                .Include(l => l.ItEquips)
                .OrderBy(l => l.LicNum)
                .ToListAsync();
            return View(list);
        }

        /// <summary>
        /// Genera y descarga un archivo Excel (.xlsx) con el listado de licitaciones,
        /// incluyendo estado (Vigente/Por vencer/Vencida/Aplazada) con color de fila.
        /// </summary>
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

                if (aplazada)       ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");
                else if (vencida)   ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");
                else if (porVencer) ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff3cd");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Licitaciones_{DateTime.Today:yyyyMMdd}.xlsx");
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public IActionResult LicitacionCreate() => View(new LicitacionViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> LicitacionCreate(LicitacionViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            _context.Licitaciones.Add(new Licitacion
            {
                LicNum           = vm.LicNum,
                LicDesc          = vm.LicDesc,
                LicStart         = vm.LicStart,
                LicEnd           = vm.LicEnd,
                LicWarnDays      = vm.LicWarnDays,
                LicPostponed     = vm.LicPostponed,
                LicPostponedNote = vm.LicPostponed ? vm.LicPostponedNote : null,
                Active           = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Licitacion creada correctamente.";
            return RedirectToAction(nameof(Licitaciones));
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> LicitacionEdit(int id)
        {
            var entity = await _context.Licitaciones.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new LicitacionViewModel
            {
                LicCode          = entity.LicCode,
                LicNum           = entity.LicNum ?? string.Empty,
                LicDesc          = entity.LicDesc,
                LicStart         = entity.LicStart,
                LicEnd           = entity.LicEnd,
                LicWarnDays      = entity.LicWarnDays,
                LicPostponed     = entity.LicPostponed,
                LicPostponedNote = entity.LicPostponedNote,
                Active           = entity.Active
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> LicitacionEdit(LicitacionViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var entity = await _context.Licitaciones.FindAsync(vm.LicCode);
            if (entity == null) return NotFound();

            entity.LicNum           = vm.LicNum;
            entity.LicDesc          = vm.LicDesc;
            entity.LicStart         = vm.LicStart;
            entity.LicEnd           = vm.LicEnd;
            entity.LicWarnDays      = vm.LicWarnDays;
            entity.LicPostponed     = vm.LicPostponed;
            entity.LicPostponedNote = vm.LicPostponed ? vm.LicPostponedNote : null;
            entity.Active           = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Licitacion actualizada correctamente.";
            return RedirectToAction(nameof(Licitaciones));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> LicitacionToggle(int id)
        {
            var entity = await _context.Licitaciones.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Licitacion {(entity.Active ? "activada" : "desactivada")}.";
            return RedirectToAction(nameof(Licitaciones));
        }

        // ===================== BODEGAS =====================

        /// <summary>
        /// Muestra el listado de bodegas con la cantidad de equipos IT y cajas de repuestos almacenadas.
        /// </summary>
        public async Task<IActionResult> Warehouses()
        {
            var list = await _context.Warehouses
                .Include(w => w.ItEquips)
                .Include(w => w.EngBoxes)
                    .ThenInclude(b => b.Parts)
                .OrderBy(w => w.WareName)
                .ToListAsync();
            return View(list);
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public IActionResult WarehouseCreate() => View(new WarehouseViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> WarehouseCreate(WarehouseViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            _context.Warehouses.Add(new Warehouse
            {
                WareName = vm.WareName,
                WareDesc = vm.WareDesc,
                Active   = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bodega creada correctamente.";
            return RedirectToAction(nameof(Warehouses));
        }

        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> WarehouseEdit(int id)
        {
            var entity = await _context.Warehouses.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new WarehouseViewModel
            {
                WareCode = entity.WareCode,
                WareName = entity.WareName ?? string.Empty,
                WareDesc = entity.WareDesc,
                Active   = entity.Active
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> WarehouseEdit(WarehouseViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var entity = await _context.Warehouses.FindAsync(vm.WareCode);
            if (entity == null) return NotFound();

            entity.WareName = vm.WareName;
            entity.WareDesc = vm.WareDesc;
            entity.Active   = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Bodega actualizada correctamente.";
            return RedirectToAction(nameof(Warehouses));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmManage)]
        public async Task<IActionResult> WarehouseToggle(int id)
        {
            var entity = await _context.Warehouses.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Bodega {(entity.Active ? "activada" : "desactivada")}.";
            return RedirectToAction(nameof(Warehouses));
        }
    }
}
