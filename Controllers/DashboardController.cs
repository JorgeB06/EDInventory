using EDInventory.Data;
using EDInventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
