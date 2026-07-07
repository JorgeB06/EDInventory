using EDInventory.Data;
using EDInventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Punto de entrada unificado a los mantenimientos y calibraciones de ambas divisiones.
    /// No introduce un modelo de datos nuevo: TI (<see cref="EDInventory.Models.Entities.ItEquipMaintenance"/>),
    /// Servicio (<see cref="EDInventory.Models.Entities.EngMaintenance"/>) y Calibraciones
    /// (<see cref="EDInventory.Models.Entities.Calibration"/>) siguen siendo módulos independientes;
    /// esta sección solo centraliza el acceso y muestra un resumen de pendientes de cada uno.
    /// </summary>
    [Authorize(Roles = AppRoles.AdmRead)]
    public class MaintenanceController : Controller
    {
        private readonly AppDbContext _context;

        public MaintenanceController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            bool isAdmin   = User.IsInRole(AppRoles.Admin);
            bool canSeeTI  = isAdmin || User.IsInRole(AppRoles.TiAdmin)  || User.IsInRole(AppRoles.TiTecnico)  || User.IsInRole(AppRoles.TiConsulta)  || User.IsInRole(AppRoles.SvcAdmin);
            bool canSeeSvc = isAdmin || User.IsInRole(AppRoles.SvcAdmin) || User.IsInRole(AppRoles.SvcTecnico) || User.IsInRole(AppRoles.SvcConsulta) || User.IsInRole(AppRoles.TiAdmin);

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            if (canSeeTI)
            {
                ViewBag.MantsTIPendientes = await _context.ItEquipMaintenances.CountAsync(m => m.MaintStatus == "PENDIENTE");
                ViewBag.MantsTIVencidos   = await _context.ItEquipMaintenances.CountAsync(m => m.MaintStatus == "PENDIENTE" && m.MaintScheduled < hoy);
            }

            if (canSeeSvc)
            {
                ViewBag.MantsSvcPendientes = await _context.EngMaintenances.CountAsync(m => m.MaintStatus == "PENDIENTE");
                ViewBag.MantsSvcVencidos   = await _context.EngMaintenances.CountAsync(m => m.MaintStatus == "PENDIENTE" && m.MaintScheduled < hoy);

                ViewBag.CalibPendientes = await _context.Calibrations
                    .CountAsync(c => c.CalibNextDate.HasValue && c.CalibNextDate <= hoy.AddDays(30) && c.CalibNextDate >= hoy);
                ViewBag.CalibVencidas = await _context.Calibrations
                    .CountAsync(c => c.CalibNextDate.HasValue && c.CalibNextDate < hoy);
            }

            ViewBag.CanSeeTI  = canSeeTI;
            ViewBag.CanSeeSvc = canSeeSvc;
            return View();
        }
    }
}
