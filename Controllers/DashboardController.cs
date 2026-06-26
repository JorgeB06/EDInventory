using EDInventory.Data;
using EDInventory.Models;
using EDInventory.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using EntityDocument = EDInventory.Models.Entities.Document;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Controlador del panel de control principal. Requiere usuario autenticado.
    /// Muestra métricas de TI (equipos, mantenimientos, licitaciones) y Servicio (activos, mantenimientos)
    /// filtrando por el rol del usuario activo. También genera el reporte general exportable.
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>Inicializa el controlador con el contexto de base de datos.</summary>
        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Muestra el Dashboard principal con tarjetas de KPIs y alertas de mantenimientos
        /// y licitaciones vencidas o por vencer, según el rol del usuario autenticado.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Visibilidad por rol
            bool isAdmin   = User.IsInRole(AppRoles.Admin);
            bool canSeeTI  = isAdmin || User.IsInRole(AppRoles.TiAdmin)  || User.IsInRole(AppRoles.TiTecnico)  || User.IsInRole(AppRoles.TiConsulta)  || User.IsInRole(AppRoles.SvcAdmin);
            bool canSeeSvc = isAdmin || User.IsInRole(AppRoles.SvcAdmin) || User.IsInRole(AppRoles.SvcTecnico) || User.IsInRole(AppRoles.SvcConsulta) || User.IsInRole(AppRoles.TiAdmin);

            ViewBag.CanSeeTI  = canSeeTI;
            ViewBag.CanSeeSvc = canSeeSvc;

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            // ── DATOS MÓDULO TI ───────────────────────────────────────────────
            if (canSeeTI)
            {
                ViewBag.TotalEquipos      = await _context.ItEquips.CountAsync(e => e.Active);
                ViewBag.EquiposEnBodega   = await _context.ItEquips.CountAsync(e => e.Active && e.WareCode.HasValue);
                ViewBag.EquiposEnHospital = await _context.ItEquips.CountAsync(e => e.Active && e.HospCode.HasValue);

                // Mantenimientos TI pendientes
                ViewBag.MantsTIPendientes = await _context.ItEquipMaintenances
                    .CountAsync(m => m.MaintStatus == "PENDIENTE");
                ViewBag.MantsTIVencidos = await _context.ItEquipMaintenances
                    .CountAsync(m => m.MaintStatus == "PENDIENTE" && m.MaintScheduled < hoy);

                // Licitaciones con alertas (calculado en memoria — colección pequeña)
                var licsActivas = await _context.Licitaciones
                    .Where(l => l.Active && l.LicEnd.HasValue)
                    .Select(l => new { l.LicEnd, l.LicWarnDays, l.LicPostponed })
                    .ToListAsync();
                ViewBag.LicVencidas   = licsActivas.Count(l => l.LicEnd < hoy && !l.LicPostponed);
                ViewBag.LicPorVencer  = licsActivas.Count(l => l.LicEnd >= hoy &&
                    l.LicEnd <= DateOnly.FromDateTime(DateTime.Today.AddDays(l.LicWarnDays)));

                ViewBag.EquiposPorTipo = await _context.ItEquips
                    .Where(e => e.Active)
                    .Include(e => e.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                    .GroupBy(e => e.Model!.Brand!.AssetType!.AssetsDesc)
                    .Select(g => new { Tipo = g.Key ?? "Sin tipo", Total = g.Count() })
                    .OrderByDescending(g => g.Total)
                    .ToListAsync();

                ViewBag.EquiposPorHospital = await _context.ItEquips
                    .Where(e => e.Active && e.HospCode.HasValue)
                    .Include(e => e.Hospital)
                    .GroupBy(e => e.Hospital!.HospName)
                    .Select(g => new { Hospital = g.Key ?? "Sin hospital", Total = g.Count() })
                    .OrderByDescending(g => g.Total)
                    .Take(3)
                    .ToListAsync();

                // KPIs de red TI
                ViewBag.TiSinHostname   = await _context.ItEquips.CountAsync(e => e.Active && string.IsNullOrEmpty(e.NetHostname));
                ViewBag.TiConIp         = await _context.ItEquips.CountAsync(e => e.Active && e.NetEnabled && !string.IsNullOrEmpty(e.NetIp));
                ViewBag.TiEnDominio     = await _context.ItEquips.CountAsync(e => e.Active && e.NetInDomain);
                ViewBag.TiEnRed         = await _context.ItEquips.CountAsync(e => e.Active && e.NetEnabled);

                ViewBag.RecentMovements = await _context.ItEquipHistories
                    .Include(h => h.ItEquip)
                    .Include(h => h.User)
                    .Include(h => h.Hospital)
                    .Include(h => h.HospitalDepartment)
                    .Include(h => h.Warehouse)
                    .Include(h => h.Site)
                    .OrderByDescending(h => h.HistDate)
                    .Take(15)
                    .ToListAsync();
            }

            // ── DATOS MÓDULO SERVICIO ─────────────────────────────────────────
            if (canSeeSvc)
            {
                ViewBag.TotalLineas              = await _context.EngLines.CountAsync(l => l.Active);
                ViewBag.TotalCajas               = await _context.EngBoxes.CountAsync(b => b.Active);
                ViewBag.TotalPartes              = await _context.EngParts.CountAsync();
                ViewBag.PartsSinStock            = await _context.EngParts.CountAsync(p => p.PartQty == 0);
                ViewBag.ActivosClinicosEnHospital = await _context.EngAssets.CountAsync(a => a.Active && a.HospCode.HasValue);

                // Mantenimientos Servicio pendientes
                ViewBag.MantsSvcPendientes = await _context.EngMaintenances
                    .CountAsync(m => m.MaintStatus == "PENDIENTE");
                ViewBag.MantsSvcVencidos = await _context.EngMaintenances
                    .CountAsync(m => m.MaintStatus == "PENDIENTE" && m.MaintScheduled < hoy);

                // Repuestos sin stock (lista alerta)
                ViewBag.ListaSinStock = await _context.EngParts
                    .Where(p => p.PartQty == 0)
                    .Include(p => p.Box).ThenInclude(b => b!.Line)
                    .OrderBy(p => p.Box!.Line!.LineName)
                    .ThenBy(p => p.Box!.BoxNumber)
                    .ThenBy(p => p.PartName)
                    .Take(12)
                    .ToListAsync();

                // Repuestos con stock bajo (mayor que 0 pero <= 3)
                ViewBag.PartsBajoStock = await _context.EngParts.CountAsync(p => p.PartQty > 0 && p.PartQty <= 3);

                // KPIs de red Servicio
                ViewBag.SvcSinHostname = await _context.EngAssets.CountAsync(a => a.Active && string.IsNullOrEmpty(a.NetHostname));
                ViewBag.SvcConIp       = await _context.EngAssets.CountAsync(a => a.Active && a.NetEnabled && !string.IsNullOrEmpty(a.NetIp));
                ViewBag.SvcEnDominio   = await _context.EngAssets.CountAsync(a => a.Active && a.NetInDomain);
                ViewBag.SvcEnRed       = await _context.EngAssets.CountAsync(a => a.Active && a.NetEnabled);

                // Distribución: repuestos por línea (top 8)
                ViewBag.PartsPorLinea = await _context.EngParts
                    .Include(p => p.Box).ThenInclude(b => b!.Line)
                    .GroupBy(p => p.Box!.Line!.LineName)
                    .Select(g => new { Linea = g.Key ?? "Sin linea", Total = g.Count(), SinStock = g.Count(p => p.PartQty == 0) })
                    .OrderByDescending(g => g.Total)
                    .Take(8)
                    .ToListAsync();
            }

            return View();
        }

        // ===================== CARGA DE TECNICOS =====================

        /// <summary>
        /// Muestra la carga de trabajo de cada técnico: incidentes abiertos y mantenimientos pendientes.
        /// Accesible para Administrador y roles TI/Servicio con permisos de lectura.
        /// </summary>
        public async Task<IActionResult> Workload()
        {
            bool isAdmin   = User.IsInRole(AppRoles.Admin);
            bool canSeeTI  = isAdmin || User.IsInRole(AppRoles.TiAdmin) || User.IsInRole(AppRoles.TiTecnico) || User.IsInRole(AppRoles.TiConsulta) || User.IsInRole(AppRoles.SvcAdmin);
            bool canSeeSvc = isAdmin || User.IsInRole(AppRoles.SvcAdmin) || User.IsInRole(AppRoles.SvcTecnico) || User.IsInRole(AppRoles.SvcConsulta) || User.IsInRole(AppRoles.TiAdmin);

            // Incidentes abiertos por técnico asignado (TI + Servicio)
            var openIncidents = await _context.Incidents
                .Where(i => i.IncidentStatus == "ABIERTA" || i.IncidentStatus == "EN_PROCESO")
                .Include(i => i.Assignee).ThenInclude(u => u!.Employee)
                .ToListAsync();

            // Mantenimientos IT pendientes por técnico
            var tiMaints = canSeeTI
                ? await _context.ItEquipMaintenances
                    .Where(m => m.MaintStatus == "PENDIENTE")
                    .Include(m => m.User).ThenInclude(u => u!.Employee)
                    .ToListAsync()
                : new List<ItEquipMaintenance>();

            // Mantenimientos Servicio pendientes por técnico
            var svcMaints = canSeeSvc
                ? await _context.EngMaintenances
                    .Where(m => m.MaintStatus == "PENDIENTE")
                    .Include(m => m.User).ThenInclude(u => u!.Employee)
                    .ToListAsync()
                : new List<EngMaintenance>();

            // Todos los técnicos activos
            var technicians = await _context.Users
                .Include(u => u.Employee)
                .Where(u => u.Active)
                .OrderBy(u => u.Employee!.EmpSurname)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);

            var workload = technicians.Select(u =>
            {
                var uIncidents = openIncidents.Where(i => i.AssignedUser == u.UserCode).ToList();
                var uTiMaints  = tiMaints.Where(m => m.UserCode == u.UserCode).ToList();
                var uSvcMaints = svcMaints.Where(m => m.UserCode == u.UserCode).ToList();
                return new
                {
                    User          = u,
                    Incidents     = uIncidents.Count,
                    IncidentsHigh = uIncidents.Count(i => i.IncidentPriority == "ALTA" || i.IncidentPriority == "CRITICA"),
                    TiMaints      = uTiMaints.Count,
                    TiMaintsOverdue = uTiMaints.Count(m => m.MaintScheduled < today),
                    SvcMaints     = uSvcMaints.Count,
                    SvcMaintsOverdue = uSvcMaints.Count(m => m.MaintScheduled < today),
                    Total         = uIncidents.Count + uTiMaints.Count + uSvcMaints.Count
                };
            }).OrderByDescending(w => w.Total).ToList();

            ViewBag.Workload       = workload;
            ViewBag.CanSeeTI       = canSeeTI;
            ViewBag.CanSeeSvc      = canSeeSvc;
            ViewBag.TotalOpen      = openIncidents.Count;
            ViewBag.TiMaintTotal   = tiMaints.Count;
            ViewBag.SvcMaintTotal  = svcMaints.Count;
            return View();
        }

        // ===================== BUSQUEDA GLOBAL =====================

        /// <summary>
        /// Busqueda global en tiempo real sobre equipos IT, activos clinicos, incidentes y calibraciones.
        /// Devuelve resultados agrupados por tipo.
        /// </summary>
        public async Task<IActionResult> Search(string? q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return View(new EDInventory.Models.ViewModels.GlobalSearchResult());

            q = q.Trim();
            bool isAdmin   = User.IsInRole(AppRoles.Admin);
            bool canSeeTI  = isAdmin || User.IsInRole(AppRoles.TiAdmin) || User.IsInRole(AppRoles.TiTecnico) || User.IsInRole(AppRoles.TiConsulta) || User.IsInRole(AppRoles.SvcAdmin);
            bool canSeeSvc = isAdmin || User.IsInRole(AppRoles.SvcAdmin) || User.IsInRole(AppRoles.SvcTecnico) || User.IsInRole(AppRoles.SvcConsulta) || User.IsInRole(AppRoles.TiAdmin);
            bool canSeeAdm = isAdmin || User.IsInRole(AppRoles.TiAdmin) || User.IsInRole(AppRoles.SvcAdmin);

            var result = new EDInventory.Models.ViewModels.GlobalSearchResult { Query = q };

            if (canSeeTI)
            {
                result.Equipos = await _context.ItEquips
                    .Include(e => e.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                    .Include(e => e.Hospital)
                    .Include(e => e.Warehouse)
                    .Include(e => e.Site)
                    .Where(e => e.ItequipDesc!.Contains(q) || e.ItequipSn!.Contains(q) ||
                                e.ItequipNum!.Contains(q) || e.NetHostname!.Contains(q) ||
                                e.NetIp!.Contains(q))
                    .OrderBy(e => e.ItequipDesc)
                    .Take(20)
                    .ToListAsync();
            }

            if (canSeeSvc)
            {
                result.Assets = await _context.EngAssets
                    .Include(a => a.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                    .Include(a => a.Hospital)
                    .Include(a => a.Line)
                    .Where(a => a.AssetDesc!.Contains(q) || a.AssetSN!.Contains(q) ||
                                a.AssetNum!.Contains(q)  || a.NetHostname!.Contains(q) ||
                                a.NetIp!.Contains(q))
                    .OrderBy(a => a.AssetDesc)
                    .Take(20)
                    .ToListAsync();

                result.Parts = await _context.EngParts
                    .Include(p => p.Box).ThenInclude(b => b!.Line)
                    .Where(p => p.PartName!.Contains(q) || p.PartRef!.Contains(q) || p.PartMfrRef!.Contains(q))
                    .OrderBy(p => p.PartName)
                    .Take(20)
                    .ToListAsync();
            }

            if (canSeeTI || canSeeSvc)
            {
                result.Incidents = await _context.Incidents
                    .Include(i => i.Assignee).ThenInclude(u => u!.Employee)
                    .Include(i => i.ItEquip)
                    .Include(i => i.EngAsset)
                    .Where(i => i.IncidentTitle.Contains(q) || i.IncidentDesc!.Contains(q))
                    .OrderByDescending(i => i.IncidentDate)
                    .Take(10)
                    .ToListAsync();
            }

            if (canSeeSvc)
            {
                result.Hospitals = await _context.Hospitals
                    .Where(h => h.HospName!.Contains(q) || h.HospAddress!.Contains(q))
                    .OrderBy(h => h.HospName)
                    .Take(10)
                    .ToListAsync();
            }

            ViewBag.Query = q;
            return View(result);
        }

        // ===================== PDF EXPORT =====================

        /// <summary>
        /// Genera y descarga un reporte PDF del inventario. Incluye equipos IT (si rol TI) y activos
        /// clinicos (si rol Servicio).
        /// </summary>
        public async Task<IActionResult> ExportPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            bool isAdmin   = User.IsInRole(AppRoles.Admin);
            bool canSeeTI  = isAdmin || User.IsInRole(AppRoles.TiAdmin) || User.IsInRole(AppRoles.TiTecnico) || User.IsInRole(AppRoles.TiConsulta) || User.IsInRole(AppRoles.SvcAdmin);
            bool canSeeSvc = isAdmin || User.IsInRole(AppRoles.SvcAdmin) || User.IsInRole(AppRoles.SvcTecnico) || User.IsInRole(AppRoles.SvcConsulta) || User.IsInRole(AppRoles.TiAdmin);

            var today = DateOnly.FromDateTime(DateTime.Today);

            var equipos = canSeeTI
                ? await _context.ItEquips
                    .Include(e => e.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                    .Include(e => e.Hospital)
                    .Include(e => e.Warehouse)
                    .Include(e => e.Site)
                    .Where(e => e.Active)
                    .OrderBy(e => e.Model!.Brand!.AssetType!.AssetsDesc).ThenBy(e => e.ItequipDesc)
                    .ToListAsync()
                : new List<ItEquip>();

            var activos = canSeeSvc
                ? await _context.EngAssets
                    .Include(a => a.Line)
                    .Include(a => a.Hospital)
                    .Include(a => a.Warehouse)
                    .Include(a => a.Model).ThenInclude(m => m!.Brand).ThenInclude(b => b!.AssetType)
                    .Where(a => a.Active)
                    .OrderBy(a => a.Line!.LineName).ThenBy(a => a.AssetDesc)
                    .ToListAsync()
                : new List<EngAsset>();

            var company = await _context.Companies
                .Where(c => c.Active).OrderBy(c => c.CompanyCode).FirstOrDefaultAsync();

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
                                col.Item().Text(company?.CompanyName ?? "EDInventory").FontSize(14).Bold();
                                col.Item().Text("Reporte de Inventario").FontSize(11).SemiBold();
                                col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });

                    page.Content().Column(col =>
                    {
                        if (canSeeTI && equipos.Any())
                        {
                            col.Item().PaddingTop(10).Text("Inventario TI").FontSize(11).Bold();
                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                });

                                table.Header(hdr =>
                                {
                                    void HdrCell(string txt) =>
                                        hdr.Cell().Background(Colors.Grey.Darken3)
                                           .Padding(3).Text(txt).FontColor(Colors.White).Bold().FontSize(7);
                                    HdrCell("Descripcion"); HdrCell("Tipo"); HdrCell("Serie");
                                    HdrCell("Num. Equipo"); HdrCell("Ubicacion"); HdrCell("Estado");
                                });

                                bool altRow = false;
                                foreach (var e in equipos)
                                {
                                    altRow = !altRow;
                                    var bg = altRow ? Colors.Grey.Lighten4 : Colors.White;
                                    string ubi = e.WareCode.HasValue ? $"Bodega: {e.Warehouse?.WareName}"
                                        : e.HospCode.HasValue ? $"Hospital: {e.Hospital?.HospName}"
                                        : e.SiteCode.HasValue ? $"Sede" : "—";

                                    void Cell(string txt) =>
                                        table.Cell().Background(bg).Padding(2).Text(txt ?? "—").FontSize(7);

                                    Cell(e.ItequipDesc ?? "—");
                                    Cell(e.Model?.Brand?.AssetType?.AssetsDesc ?? "—");
                                    Cell(e.ItequipSn ?? "—");
                                    Cell(e.ItequipNum ?? "—");
                                    Cell(ubi);
                                    Cell(e.EquipStatus.Replace("_", " "));
                                }
                            });
                        }

                        if (canSeeSvc && activos.Any())
                        {
                            col.Item().PaddingTop(16).Text("Activos Clinicos").FontSize(11).Bold();
                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                    cols.RelativeColumn(2);
                                });

                                table.Header(hdr =>
                                {
                                    void HdrCell(string txt) =>
                                        hdr.Cell().Background(Colors.Orange.Darken3)
                                           .Padding(3).Text(txt).FontColor(Colors.White).Bold().FontSize(7);
                                    HdrCell("Descripcion"); HdrCell("Linea"); HdrCell("Serie");
                                    HdrCell("Num. Activo"); HdrCell("Ubicacion"); HdrCell("Estado");
                                });

                                bool altRow = false;
                                foreach (var a in activos)
                                {
                                    altRow = !altRow;
                                    var bg = altRow ? Colors.Orange.Lighten5 : Colors.White;
                                    string ubi = a.WareCode.HasValue ? $"Bodega: {a.Warehouse?.WareName}"
                                        : a.HospCode.HasValue ? $"Hospital: {a.Hospital?.HospName}"
                                        : "—";

                                    void Cell(string txt) =>
                                        table.Cell().Background(bg).Padding(2).Text(txt ?? "—").FontSize(7);

                                    Cell(a.AssetDesc ?? "—");
                                    Cell(a.Line?.LineName ?? "—");
                                    Cell(a.AssetSN ?? "—");
                                    Cell(a.AssetNum ?? "—");
                                    Cell(ubi);
                                    Cell(a.AssetStatus.Replace("_", " "));
                                }
                            });
                        }
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
            var filename = $"Inventario_{DateTime.Today:yyyyMMdd}.pdf";
            return File(bytes, "application/pdf", filename);
        }

        /// <summary>
        /// Muestra el reporte general del inventario con tablas de equipos IT y activos clínicos.
        /// Solo incluye la sección TI si el usuario tiene rol TI; solo la sección Servicio si tiene rol Servicio.
        /// Los datos se pasan por ViewBag.
        /// </summary>
        public async Task<IActionResult> Report()
        {
            bool isAdmin   = User.IsInRole(AppRoles.Admin);
            bool canSeeTI  = isAdmin || User.IsInRole(AppRoles.TiAdmin)  || User.IsInRole(AppRoles.TiTecnico)  || User.IsInRole(AppRoles.TiConsulta)  || User.IsInRole(AppRoles.SvcAdmin);
            bool canSeeSvc = isAdmin || User.IsInRole(AppRoles.SvcAdmin) || User.IsInRole(AppRoles.SvcTecnico) || User.IsInRole(AppRoles.SvcConsulta) || User.IsInRole(AppRoles.TiAdmin);

            var today = DateOnly.FromDateTime(DateTime.Today);

            var equipos = canSeeTI
                ? await _context.ItEquips
                    .Include(e => e.Site)
                    .Include(e => e.Hospital)
                    .Include(e => e.HospitalDepartment)
                    .Include(e => e.Warehouse)
                    .Include(e => e.Model)
                        .ThenInclude(m => m!.Brand)
                            .ThenInclude(b => b!.AssetType)
                    .Include(e => e.Licitacion)
                    .OrderBy(e => e.Model!.Brand!.AssetType!.AssetsDesc)
                    .ThenBy(e => e.ItequipDesc)
                    .ToListAsync()
                : [];

            var activos = canSeeSvc
                ? await _context.EngAssets
                    .Include(a => a.Line)
                    .Include(a => a.Hospital)
                    .Include(a => a.HospitalDepartment)
                    .Include(a => a.Warehouse)
                    .Include(a => a.Model)
                        .ThenInclude(m => m!.Brand)
                            .ThenInclude(b => b!.AssetType)
                    .Include(a => a.Licitacion)
                    .OrderBy(a => a.Line!.LineName)
                    .ThenBy(a => a.AssetDesc)
                    .ToListAsync()
                : [];

            ViewBag.Today     = today;
            ViewBag.CanSeeTI  = canSeeTI;
            ViewBag.CanSeeSvc = canSeeSvc;
            ViewBag.Activos   = activos;
            return View(equipos);
        }
    }
}
