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
                .AsQueryable();

            if (assetId.HasValue)           query = query.Where(c => c.AssetCode == assetId);
            if (!string.IsNullOrEmpty(result)) query = query.Where(c => c.CalibResult == result);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            ViewBag.AssetId    = assetId;
            ViewBag.Result     = result;
            ViewBag.Page       = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Total      = total;

            var items = await query
                .OrderByDescending(c => c.CalibDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(items);
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
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
        [Authorize(Roles = AppRoles.SvcWrite)]
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
            };

            _context.Calibrations.Add(calib);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Calibracion registrada correctamente.";
            return RedirectToAction("AssetDetail", "Engineering", new { id = vm.AssetCode });
        }

        [Authorize(Roles = AppRoles.SvcWrite)]
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
            };
            await PopulateSelectLists(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.SvcWrite)]
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
        }
    }
}
