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
    /// Controlador de catálogos del sistema. Requiere rol <c>Administrador</c>.
    /// Gestiona la jerarquía completa de catálogos: Empresas, Sedes, Tipos de Documento,
    /// Tipos Generales de Activo → Tipos de Activo → Marcas → Modelos.
    /// Cada entidad expone acciones List / Create (GET+POST) / Edit (GET+POST) / Toggle y Delete donde aplica.
    /// </summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class CatalogController : Controller
    {
        private readonly AppDbContext _context;

        /// <summary>Inicializa el controlador con el contexto de base de datos.</summary>
        public CatalogController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== EMPRESAS =====================

        /// <summary>Muestra el listado de empresas con su tipo de documento.</summary>
        public async Task<IActionResult> Companies()
        {
            var list = await _context.Companies
                .Include(c => c.DocumentTypeCompany)
                .OrderBy(c => c.CompanyName)
                .ToListAsync();
            return View(list);
        }

        /// <summary>[GET] Muestra el formulario de creación de empresa.</summary>
        public async Task<IActionResult> CompanyCreate()
        {
            var vm = new CompanyViewModel
            {
                DocumentTypes = await GetDocumentTypeSelectList()
            };
            return View(vm);
        }

        /// <summary>[POST] Persiste la nueva empresa en la base de datos.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyCreate(CompanyViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.DocumentTypes = await GetDocumentTypeSelectList();
                return View(vm);
            }

            _context.Companies.Add(new Company
            {
                CompanyName = vm.CompanyName,
                DoctypeCode = vm.DoctypeCode,
                DocumentType = vm.DocumentType,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Empresa creada correctamente.";
            return RedirectToAction(nameof(Companies));
        }

        /// <summary>[GET] Muestra el formulario de edición de la empresa indicada por <paramref name="id"/>.</summary>
        public async Task<IActionResult> CompanyEdit(int id)
        {
            var entity = await _context.Companies.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new CompanyViewModel
            {
                CompanyCode = entity.CompanyCode,
                CompanyName = entity.CompanyName ?? string.Empty,
                DoctypeCode = entity.DoctypeCode,
                DocumentType = entity.DocumentType,
                Active = entity.Active,
                DocumentTypes = await GetDocumentTypeSelectList()
            });
        }

        /// <summary>[POST] Guarda los cambios de la empresa en la base de datos.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyEdit(CompanyViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.DocumentTypes = await GetDocumentTypeSelectList();
                return View(vm);
            }

            var entity = await _context.Companies.FindAsync(vm.CompanyCode);
            if (entity == null) return NotFound();

            entity.CompanyName = vm.CompanyName;
            entity.DoctypeCode = vm.DoctypeCode;
            entity.DocumentType = vm.DocumentType;
            entity.Active = vm.Active;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Empresa actualizada correctamente.";
            return RedirectToAction(nameof(Companies));
        }

        /// <summary>[POST] Alterna el estado activo/inactivo de la empresa indicada por <paramref name="id"/>.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompanyToggle(int id)
        {
            var entity = await _context.Companies.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Empresa {(entity.Active ? "activada" : "desactivada")}.";
            return RedirectToAction(nameof(Companies));
        }

        // ===================== SEDES =====================

        /// <summary>Muestra el listado de sedes físicas con su empresa propietaria.</summary>
        public async Task<IActionResult> Sites()
        {
            var list = await _context.Sites
                .Include(s => s.Company)
                .OrderBy(s => s.SiteName)
                .ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> SiteCreate()
        {
            return View(new SiteViewModel { Companies = await GetCompanySelectList() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SiteCreate(SiteViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Companies = await GetCompanySelectList();
                return View(vm);
            }

            _context.Sites.Add(new Site
            {
                SiteName = vm.SiteName,
                SiteAddress = vm.SiteAddress,
                CompanyCode = vm.CompanyCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Sede creada correctamente.";
            return RedirectToAction(nameof(Sites));
        }

        public async Task<IActionResult> SiteEdit(int id)
        {
            var entity = await _context.Sites.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new SiteViewModel
            {
                SiteCode = entity.SiteCode,
                SiteName = entity.SiteName ?? string.Empty,
                SiteAddress = entity.SiteAddress,
                CompanyCode = entity.CompanyCode,
                Active = entity.Active,
                Companies = await GetCompanySelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SiteEdit(SiteViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Companies = await GetCompanySelectList();
                return View(vm);
            }

            var entity = await _context.Sites.FindAsync(vm.SiteCode);
            if (entity == null) return NotFound();

            entity.SiteName = vm.SiteName;
            entity.SiteAddress = vm.SiteAddress;
            entity.CompanyCode = vm.CompanyCode;
            entity.Active = vm.Active;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Sede actualizada correctamente.";
            return RedirectToAction(nameof(Sites));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SiteToggle(int id)
        {
            var entity = await _context.Sites.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Sede {(entity.Active ? "activada" : "desactivada")}.";
            return RedirectToAction(nameof(Sites));
        }

        // ===================== DEPARTAMENTOS =====================

        public async Task<IActionResult> Departments()
        {
            var list = await _context.Departments
                .Include(d => d.Site)
                .OrderBy(d => d.DepName)
                .ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> DepartmentCreate()
        {
            return View(new DepartmentViewModel { Sites = await GetSiteSelectList() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartmentCreate(DepartmentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Sites = await GetSiteSelectList();
                return View(vm);
            }

            _context.Departments.Add(new Department
            {
                DepName = vm.DepName,
                SiteCode = vm.SiteCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Departamento creado correctamente.";
            return RedirectToAction(nameof(Departments));
        }

        public async Task<IActionResult> DepartmentEdit(int id)
        {
            var entity = await _context.Departments.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new DepartmentViewModel
            {
                DepCode = entity.DepCode,
                DepName = entity.DepName ?? string.Empty,
                SiteCode = entity.SiteCode,
                Active = entity.Active,
                Sites = await GetSiteSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartmentEdit(DepartmentViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Sites = await GetSiteSelectList();
                return View(vm);
            }

            var entity = await _context.Departments.FindAsync(vm.DepCode);
            if (entity == null) return NotFound();

            entity.DepName = vm.DepName;
            entity.SiteCode = vm.SiteCode;
            entity.Active = vm.Active;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Departamento actualizado correctamente.";
            return RedirectToAction(nameof(Departments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DepartmentToggle(int id)
        {
            var entity = await _context.Departments.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Departamento {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Departments));
        }

        // ===================== TIPOS GENERALES DE ACTIVO =====================

        /// <summary>Muestra la jerarquía completa: Tipos Generales → Tipos Específicos → Marcas → Modelos.</summary>
        public async Task<IActionResult> AssetTypes()
        {
            var genTypes = await _context.GenAssetTypes
                .Include(g => g.AssetTypes)
                .OrderBy(g => g.GenAssetsDesc)
                .ToListAsync();
            return View(genTypes);
        }

        public IActionResult GenAssetTypeCreate() => View(new GenAssetTypeViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenAssetTypeCreate(GenAssetTypeViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            _context.GenAssetTypes.Add(new GenAssetType
            {
                GenAssetsDesc = vm.GenAssetsDesc,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Categoria creada correctamente.";
            return RedirectToAction(nameof(AssetTypes));
        }

        public async Task<IActionResult> GenAssetTypeEdit(int id)
        {
            var entity = await _context.GenAssetTypes.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new GenAssetTypeViewModel
            {
                GenAssetsTypeCode = entity.GenAssetsTypeCode,
                GenAssetsDesc = entity.GenAssetsDesc ?? string.Empty,
                Active = entity.Active
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenAssetTypeEdit(GenAssetTypeViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var entity = await _context.GenAssetTypes.FindAsync(vm.GenAssetsTypeCode);
            if (entity == null) return NotFound();

            entity.GenAssetsDesc = vm.GenAssetsDesc;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Categoria actualizada correctamente.";
            return RedirectToAction(nameof(AssetTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenAssetTypeToggle(int id)
        {
            var entity = await _context.GenAssetTypes.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Categoria {(entity.Active ? "activada" : "desactivada")}.";
            return RedirectToAction(nameof(AssetTypes));
        }

        // ===================== TIPOS DE ACTIVO =====================

        public async Task<IActionResult> AssetTypeCreate()
        {
            return View(new AssetTypeViewModel { GenAssetTypes = await GetGenAssetTypeSelectList() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssetTypeCreate(AssetTypeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.GenAssetTypes = await GetGenAssetTypeSelectList();
                return View(vm);
            }

            _context.AssetTypes.Add(new AssetType
            {
                AssetsDesc = vm.AssetsDesc,
                GenAssetsTypeCode = vm.GenAssetsTypeCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tipo de activo creado correctamente.";
            return RedirectToAction(nameof(AssetTypes));
        }

        public async Task<IActionResult> AssetTypeEdit(int id)
        {
            var entity = await _context.AssetTypes.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new AssetTypeViewModel
            {
                AssetsTypeCode = entity.AssetsTypeCode,
                AssetsDesc = entity.AssetsDesc ?? string.Empty,
                GenAssetsTypeCode = entity.GenAssetsTypeCode,
                Active = entity.Active,
                GenAssetTypes = await GetGenAssetTypeSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssetTypeEdit(AssetTypeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.GenAssetTypes = await GetGenAssetTypeSelectList();
                return View(vm);
            }

            var entity = await _context.AssetTypes.FindAsync(vm.AssetsTypeCode);
            if (entity == null) return NotFound();

            entity.AssetsDesc = vm.AssetsDesc;
            entity.GenAssetsTypeCode = vm.GenAssetsTypeCode;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tipo de activo actualizado correctamente.";
            return RedirectToAction(nameof(AssetTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssetTypeToggle(int id)
        {
            var entity = await _context.AssetTypes.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Tipo de activo {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(AssetTypes));
        }

        // ===================== MARCAS Y MODELOS =====================

        /// <summary>Muestra el listado de marcas con sus modelos asociados.</summary>
        public async Task<IActionResult> Brands()
        {
            var list = await _context.Brands
                .Include(b => b.AssetType)
                .Include(b => b.Models)
                .OrderBy(b => b.BrandDesc)
                .ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> BrandCreate()
        {
            return View(new BrandViewModel { AssetTypes = await GetAssetTypeSelectList() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BrandCreate(BrandViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AssetTypes = await GetAssetTypeSelectList();
                return View(vm);
            }

            _context.Brands.Add(new Brand
            {
                BrandDesc = vm.BrandDesc,
                AssetsTypeCode = vm.AssetsTypeCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Marca creada correctamente.";
            return RedirectToAction(nameof(Brands));
        }

        public async Task<IActionResult> BrandEdit(int id)
        {
            var entity = await _context.Brands.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new BrandViewModel
            {
                BrandCode = entity.BrandCode,
                BrandDesc = entity.BrandDesc ?? string.Empty,
                AssetsTypeCode = entity.AssetsTypeCode,
                Active = entity.Active,
                AssetTypes = await GetAssetTypeSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BrandEdit(BrandViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AssetTypes = await GetAssetTypeSelectList();
                return View(vm);
            }

            var entity = await _context.Brands.FindAsync(vm.BrandCode);
            if (entity == null) return NotFound();

            entity.BrandDesc = vm.BrandDesc;
            entity.AssetsTypeCode = vm.AssetsTypeCode;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Marca actualizada correctamente.";
            return RedirectToAction(nameof(Brands));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BrandToggle(int id)
        {
            var entity = await _context.Brands.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Marca {(entity.Active ? "activada" : "desactivada")}.";
            return RedirectToAction(nameof(Brands));
        }

        // ===================== MODELOS =====================

        /// <summary>[GET] Muestra el formulario de creación de un modelo para la marca indicada por <paramref name="brandId"/>.</summary>
        public async Task<IActionResult> ModelCreate(int brandId)
        {
            var brand = await _context.Brands.FindAsync(brandId);
            if (brand == null) return NotFound();

            return View(new ModelViewModel
            {
                BrandCode = brandId,
                Brands = await GetBrandSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModelCreate(ModelViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Brands = await GetBrandSelectList();
                return View(vm);
            }

            _context.Models.Add(new Model
            {
                ModelDesc = vm.ModelDesc,
                BrandCode = vm.BrandCode,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Modelo creado correctamente.";
            return RedirectToAction(nameof(Brands));
        }

        public async Task<IActionResult> ModelEdit(int id)
        {
            var entity = await _context.Models.FindAsync(id);
            if (entity == null) return NotFound();

            return View(new ModelViewModel
            {
                ModelCode = entity.ModelCode,
                ModelDesc = entity.ModelDesc ?? string.Empty,
                BrandCode = entity.BrandCode,
                Active = entity.Active,
                Brands = await GetBrandSelectList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModelEdit(ModelViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Brands = await GetBrandSelectList();
                return View(vm);
            }

            var entity = await _context.Models.FindAsync(vm.ModelCode);
            if (entity == null) return NotFound();

            entity.ModelDesc = vm.ModelDesc;
            entity.BrandCode = vm.BrandCode;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Modelo actualizado correctamente.";
            return RedirectToAction(nameof(Brands));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModelToggle(int id)
        {
            var entity = await _context.Models.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Modelo {(entity.Active ? "activado" : "desactivado")}.";
            return RedirectToAction(nameof(Brands));
        }

        // ===================== TIPOS DE DOCUMENTO (EMPRESA) =====================

        /// <summary>Muestra el catálogo de tipos de documento de empresa (ej. cédula jurídica).</summary>
        public async Task<IActionResult> DocumentTypes()
        {
            var list = await _context.DocumentTypeCompanies.OrderBy(d => d.DoctypeDesc).ToListAsync();
            return View(list);
        }

        public IActionResult DocumentTypeCreate() => View(new DocumentTypeViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentTypeCreate(DocumentTypeViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            _context.DocumentTypeCompanies.Add(new DocumentTypeCompany
            {
                DoctypeDesc = vm.DoctypeDesc,
                Active = vm.Active
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tipo de documento creado correctamente.";
            return RedirectToAction(nameof(DocumentTypes));
        }

        public async Task<IActionResult> DocumentTypeEdit(int id)
        {
            var entity = await _context.DocumentTypeCompanies.FindAsync(id);
            if (entity == null) return NotFound();
            return View(new DocumentTypeViewModel
            {
                DoctypeCode = entity.DoctypeCode,
                DoctypeDesc = entity.DoctypeDesc ?? string.Empty,
                Active = entity.Active
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentTypeEdit(DocumentTypeViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);
            var entity = await _context.DocumentTypeCompanies.FindAsync(vm.DoctypeCode);
            if (entity == null) return NotFound();
            entity.DoctypeDesc = vm.DoctypeDesc;
            entity.Active = vm.Active;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tipo de documento actualizado.";
            return RedirectToAction(nameof(DocumentTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentTypeToggle(int id)
        {
            var entity = await _context.DocumentTypeCompanies.FindAsync(id);
            if (entity == null) return NotFound();
            entity.Active = !entity.Active;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(DocumentTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DocumentTypeDelete(int id)
        {
            var entity = await _context.DocumentTypeCompanies.FindAsync(id);
            if (entity == null) return NotFound();
            var enUso = await _context.Companies.AnyAsync(c => c.DoctypeCode == id);
            if (enUso)
            {
                TempData["Error"] = "No se puede eliminar: hay empresas usando este tipo de documento.";
                return RedirectToAction(nameof(DocumentTypes));
            }
            _context.DocumentTypeCompanies.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tipo de documento eliminado.";
            return RedirectToAction(nameof(DocumentTypes));
        }

        // ===================== DELETE: CATEGORIAS, TIPOS, MARCAS, MODELOS =====================

        /// <summary>[POST] Elimina la categoría general si no tiene tipos de activo dependientes.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenAssetTypeDelete(int id)
        {
            var entity = await _context.GenAssetTypes.FindAsync(id);
            if (entity == null) return NotFound();
            var enUso = await _context.AssetTypes.AnyAsync(a => a.GenAssetsTypeCode == id);
            if (enUso)
            {
                TempData["Error"] = "No se puede eliminar: hay tipos de activo usando esta categoria.";
                return RedirectToAction(nameof(AssetTypes));
            }
            _context.GenAssetTypes.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Categoria eliminada.";
            return RedirectToAction(nameof(AssetTypes));
        }

        /// <summary>[POST] Elimina el tipo de activo si no tiene marcas dependientes.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssetTypeDelete(int id)
        {
            var entity = await _context.AssetTypes.FindAsync(id);
            if (entity == null) return NotFound();
            var enUso = await _context.Brands.AnyAsync(b => b.AssetsTypeCode == id);
            if (enUso)
            {
                TempData["Error"] = "No se puede eliminar: hay marcas usando este tipo de activo.";
                return RedirectToAction(nameof(AssetTypes));
            }
            _context.AssetTypes.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tipo de activo eliminado.";
            return RedirectToAction(nameof(AssetTypes));
        }

        /// <summary>[POST] Elimina la marca si no tiene modelos ni equipos asociados.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BrandDelete(int id)
        {
            var entity = await _context.Brands.Include(b => b.Models).FirstOrDefaultAsync(b => b.BrandCode == id);
            if (entity == null) return NotFound();
            var enUso = await _context.ItEquips.AnyAsync(e => e.ModelCode != null &&
                _context.Models.Any(m => m.ModelCode == e.ModelCode && m.BrandCode == id));
            if (enUso || entity.Models.Any())
            {
                TempData["Error"] = "No se puede eliminar: la marca tiene modelos o equipos asociados.";
                return RedirectToAction(nameof(Brands));
            }
            _context.Brands.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Marca eliminada.";
            return RedirectToAction(nameof(Brands));
        }

        /// <summary>[POST] Elimina el modelo si no hay equipos IT ni activos clínicos que lo referencien.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ModelDelete(int id)
        {
            var entity = await _context.Models.FindAsync(id);
            if (entity == null) return NotFound();
            var enUso = await _context.ItEquips.AnyAsync(e => e.ModelCode == id);
            if (enUso)
            {
                TempData["Error"] = "No se puede eliminar: hay equipos usando este modelo.";
                return RedirectToAction(nameof(Brands));
            }
            _context.Models.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Modelo eliminado.";
            return RedirectToAction(nameof(Brands));
        }

        // ===================== HELPERS SelectList =====================

        private async Task<IEnumerable<SelectListItem>> GetDocumentTypeSelectList() =>
            (await _context.DocumentTypeCompanies.Where(d => d.Active).OrderBy(d => d.DoctypeDesc).ToListAsync())
            .Select(d => new SelectListItem(d.DoctypeDesc, d.DoctypeCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetCompanySelectList() =>
            (await _context.Companies.Where(c => c.Active).OrderBy(c => c.CompanyName).ToListAsync())
            .Select(c => new SelectListItem(c.CompanyName, c.CompanyCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetSiteSelectList() =>
            (await _context.Sites.Where(s => s.Active).OrderBy(s => s.SiteName).ToListAsync())
            .Select(s => new SelectListItem(s.SiteName, s.SiteCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetGenAssetTypeSelectList() =>
            (await _context.GenAssetTypes.Where(g => g.Active).OrderBy(g => g.GenAssetsDesc).ToListAsync())
            .Select(g => new SelectListItem(g.GenAssetsDesc, g.GenAssetsTypeCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetAssetTypeSelectList() =>
            (await _context.AssetTypes.Where(a => a.Active).OrderBy(a => a.AssetsDesc).ToListAsync())
            .Select(a => new SelectListItem(a.AssetsDesc, a.AssetsTypeCode.ToString()));

        private async Task<IEnumerable<SelectListItem>> GetBrandSelectList() =>
            (await _context.Brands.Where(b => b.Active).OrderBy(b => b.BrandDesc).ToListAsync())
            .Select(b => new SelectListItem(b.BrandDesc, b.BrandCode.ToString()));
    }
}
