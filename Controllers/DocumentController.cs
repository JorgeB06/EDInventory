using EDInventory.Data;
using EDInventory.Models;
using EDInventory.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EDInventory.Controllers
{
    /// <summary>
    /// Gestión de documentos adjuntos a equipos IT o activos clínicos.
    /// </summary>
    [Authorize(Roles = AppRoles.AdmRead)]
    public class DocumentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB
        private static readonly string[] AllowedMimeTypes =
        [
            "application/pdf",
            "image/jpeg", "image/png",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/msword", "application/vnd.ms-excel",
        ];

        public DocumentController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env     = env;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }

        [Authorize(Roles = AppRoles.AdmWrite)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, string entityType, int? itequipCode, int? assetCode, string docType, string? docNotes)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Debe seleccionar un archivo.";
                return RedirectBack(entityType, itequipCode, assetCode);
            }

            if (file.Length > MaxFileSizeBytes)
            {
                TempData["Error"] = "El archivo supera el limite de 20 MB.";
                return RedirectBack(entityType, itequipCode, assetCode);
            }

            if (!AllowedMimeTypes.Contains(file.ContentType))
            {
                TempData["Error"] = "Tipo de archivo no permitido. Se aceptan: PDF, imagenes, Word y Excel.";
                return RedirectBack(entityType, itequipCode, assetCode);
            }

            // Sanitize filename
            var ext       = Path.GetExtension(file.FileName);
            var safeName  = Path.GetFileNameWithoutExtension(file.FileName)
                                .Replace(" ", "_")
                                .Replace("..", "")
                                .Replace("/", "")
                                .Replace("\\", "");
            safeName = safeName[..Math.Min(safeName.Length, 80)];
            var filename  = $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeName}{ext}";

            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadDir);
            var fullPath  = Path.Combine(uploadDir, filename);
            var relPath   = $"/uploads/documents/{filename}";

            await using (var stream = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(stream);

            var doc = new Document
            {
                EntityType    = entityType,
                ItequipCode   = entityType == "TI"  ? itequipCode : null,
                AssetCode     = entityType == "SVC" ? assetCode   : null,
                DocType       = docType ?? "OTRO",
                DocName       = file.FileName,
                DocFilePath   = relPath,
                DocFileSize   = (int)file.Length,
                DocMimeType   = file.ContentType,
                DocUploadDate = DateTime.Now,
                UserCode      = GetCurrentUserId(),
                DocNotes      = docNotes,
            };

            _context.Documents.Add(doc);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Documento adjuntado correctamente.";
            return RedirectBack(entityType, itequipCode, assetCode);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppRoles.AdmWrite)]
        public async Task<IActionResult> Delete(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            // Delete physical file
            var fullPath = Path.Combine(_env.WebRootPath, doc.DocFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            var entityType  = doc.EntityType;
            var itequipCode = doc.ItequipCode;
            var assetCode   = doc.AssetCode;

            _context.Documents.Remove(doc);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Documento eliminado.";
            return RedirectBack(entityType, itequipCode, assetCode);
        }

        public async Task<IActionResult> Download(int id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            var fullPath = Path.Combine(_env.WebRootPath, doc.DocFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath)) return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, doc.DocMimeType ?? "application/octet-stream", doc.DocName);
        }

        private IActionResult RedirectBack(string entityType, int? itequipCode, int? assetCode)
        {
            if (entityType == "TI" && itequipCode.HasValue)
                return RedirectToAction("Detail", "Equipment", new { id = itequipCode });
            if (entityType == "SVC" && assetCode.HasValue)
                return RedirectToAction("AssetDetail", "Engineering", new { id = assetCode });
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
