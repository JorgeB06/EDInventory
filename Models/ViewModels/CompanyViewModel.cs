using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel para crear o editar un tipo de documento de empresa (ej. cédula jurídica, RUC).
    /// Usado en el módulo de Administración.
    /// </summary>
    public class DocumentTypeViewModel
    {
        /// <summary>Clave primaria del tipo de documento. Cero indica creación.</summary>
        public int DoctypeCode { get; set; }

        /// <summary>Descripción del tipo de documento (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "La descripcion es requerida")]
        [Display(Name = "Descripcion")]
        [StringLength(45)]
        public string DoctypeDesc { get; set; } = string.Empty;

        /// <summary>Indica si el tipo de documento está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;
    }

    /// <summary>
    /// ViewModel para crear o editar una empresa propietaria de activos.
    /// Incluye la lista de tipos de documento disponibles para el selector.
    /// </summary>
    public class CompanyViewModel
    {
        /// <summary>Clave primaria de la empresa. Cero indica creación.</summary>
        public int CompanyCode { get; set; }

        /// <summary>Nombre legal de la empresa (máx. 100 caracteres).</summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre")]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>FK al tipo de documento de identificación de la empresa.</summary>
        [Display(Name = "Tipo de Documento")]
        public int? DoctypeCode { get; set; }

        /// <summary>Número del documento de identificación (máx. 45 caracteres).</summary>
        [Display(Name = "Numero de Documento")]
        [StringLength(45)]
        public string? DocumentType { get; set; }

        /// <summary>Indica si la empresa está activa en el sistema.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de tipos de documento para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> DocumentTypes { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar una sede física de la organización.
    /// Incluye la lista de empresas disponibles para el selector.
    /// </summary>
    public class SiteViewModel
    {
        /// <summary>Clave primaria de la sede. Cero indica creación.</summary>
        public int SiteCode { get; set; }

        /// <summary>Nombre de la sede (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre de Sede")]
        [StringLength(45)]
        public string SiteName { get; set; } = string.Empty;

        /// <summary>Dirección física de la sede (máx. 100 caracteres).</summary>
        [Display(Name = "Direccion")]
        [StringLength(100)]
        public string? SiteAddress { get; set; }

        /// <summary>FK a la empresa propietaria de la sede.</summary>
        [Required(ErrorMessage = "La empresa es requerida")]
        [Display(Name = "Empresa")]
        public int? CompanyCode { get; set; }

        /// <summary>Indica si la sede está activa.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de empresas para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Companies { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar un departamento interno de la organización.
    /// Incluye la lista de sedes disponibles para el selector.
    /// </summary>
    public class DepartmentViewModel
    {
        /// <summary>Clave primaria del departamento. Cero indica creación.</summary>
        public int DepCode { get; set; }

        /// <summary>Nombre del departamento (máx. 100 caracteres).</summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre de Departamento")]
        [StringLength(100)]
        public string DepName { get; set; } = string.Empty;

        /// <summary>FK a la sede a la que pertenece el departamento.</summary>
        [Required(ErrorMessage = "La sede es requerida")]
        [Display(Name = "Sede")]
        public int? SiteCode { get; set; }

        /// <summary>Indica si el departamento está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de sedes para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Sites { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar un tipo general de activo (nivel 1 de la jerarquía de catálogo).
    /// </summary>
    public class GenAssetTypeViewModel
    {
        /// <summary>Clave primaria del tipo general. Cero indica creación.</summary>
        public int GenAssetsTypeCode { get; set; }

        /// <summary>Descripción del tipo general de activo (máx. 100 caracteres).</summary>
        [Required(ErrorMessage = "La descripcion es requerida")]
        [Display(Name = "Tipo General de Activo")]
        [StringLength(100)]
        public string GenAssetsDesc { get; set; } = string.Empty;

        /// <summary>Indica si el tipo general está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;
    }

    /// <summary>
    /// ViewModel para crear o editar un tipo específico de activo (nivel 2 de la jerarquía de catálogo).
    /// Incluye la lista de tipos generales para el selector.
    /// </summary>
    public class AssetTypeViewModel
    {
        /// <summary>Clave primaria del tipo de activo. Cero indica creación.</summary>
        public int AssetsTypeCode { get; set; }

        /// <summary>Descripción del tipo de activo (máx. 100 caracteres).</summary>
        [Required(ErrorMessage = "La descripcion es requerida")]
        [Display(Name = "Tipo de Activo")]
        [StringLength(100)]
        public string AssetsDesc { get; set; } = string.Empty;

        /// <summary>FK al tipo general al que pertenece.</summary>
        [Required(ErrorMessage = "El tipo general es requerido")]
        [Display(Name = "Categoria")]
        public int? GenAssetsTypeCode { get; set; }

        /// <summary>Indica si el tipo de activo está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de tipos generales para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> GenAssetTypes { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar una marca de fabricante (nivel 3 de la jerarquía de catálogo).
    /// Incluye la lista de tipos de activo disponibles para el selector.
    /// </summary>
    public class BrandViewModel
    {
        /// <summary>Clave primaria de la marca. Cero indica creación.</summary>
        public int BrandCode { get; set; }

        /// <summary>Nombre de la marca del fabricante (máx. 40 caracteres).</summary>
        [Required(ErrorMessage = "La marca es requerida")]
        [Display(Name = "Marca")]
        [StringLength(40)]
        public string BrandDesc { get; set; } = string.Empty;

        /// <summary>FK al tipo de activo al que aplica esta marca.</summary>
        [Required(ErrorMessage = "El tipo de activo es requerido")]
        [Display(Name = "Tipo de Activo")]
        public int? AssetsTypeCode { get; set; }

        /// <summary>Indica si la marca está activa.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de tipos de activo para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> AssetTypes { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar un modelo de equipo (nivel 4 de la jerarquía de catálogo).
    /// Incluye la lista de marcas disponibles para el selector.
    /// </summary>
    public class ModelViewModel
    {
        /// <summary>Clave primaria del modelo. Cero indica creación.</summary>
        public int ModelCode { get; set; }

        /// <summary>Nombre del modelo (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El modelo es requerido")]
        [Display(Name = "Modelo")]
        [StringLength(45)]
        public string ModelDesc { get; set; } = string.Empty;

        /// <summary>FK a la marca del fabricante a la que pertenece este modelo.</summary>
        [Required(ErrorMessage = "La marca es requerida")]
        [Display(Name = "Marca")]
        public int? BrandCode { get; set; }

        /// <summary>Indica si el modelo está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de marcas para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Brands { get; set; } = [];
    }
}
