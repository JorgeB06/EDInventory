using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel para crear o editar una Unidad Programática (UP) que agrupa hospitales.
    /// </summary>
    public class ProcessingUnitViewModel
    {
        /// <summary>Clave primaria de la UP. Cero indica creación.</summary>
        public int PuCode { get; set; }

        /// <summary>Descripción o nombre de la unidad programática (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "La descripcion es requerida")]
        [Display(Name = "Unidad Programatica")]
        [StringLength(45)]
        public string PuDesc { get; set; } = string.Empty;

        /// <summary>Indica si la unidad programática está activa.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;
    }

    /// <summary>
    /// ViewModel para crear o editar un hospital cliente.
    /// Incluye la lista de unidades programáticas para el selector.
    /// </summary>
    public class HospitalViewModel
    {
        /// <summary>Clave primaria del hospital. Cero indica creación.</summary>
        public int HospCode { get; set; }

        /// <summary>Nombre oficial del hospital (máx. 50 caracteres).</summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre del Hospital")]
        [StringLength(50)]
        public string HospName { get; set; } = string.Empty;

        /// <summary>Dirección física del hospital (máx. 100 caracteres).</summary>
        [Display(Name = "Direccion")]
        [StringLength(100)]
        public string? HospAddress { get; set; }

        /// <summary>FK a la unidad programática a la que pertenece el hospital.</summary>
        [Display(Name = "Unidad Programatica")]
        public int? PuCode { get; set; }

        /// <summary>Indica si el hospital está activo en el sistema.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de unidades programáticas para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> ProcessingUnits { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar un departamento o zona dentro de un hospital.
    /// Incluye la lista de hospitales para el selector.
    /// </summary>
    public class HospitalDepartmentViewModel
    {
        /// <summary>Clave primaria del departamento hospitalario. Cero indica creación.</summary>
        public int HospDepCode { get; set; }

        /// <summary>Nombre del departamento hospitalario (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Departamento")]
        [StringLength(45)]
        public string HospDepName { get; set; } = string.Empty;

        /// <summary>FK al hospital al que pertenece este departamento.</summary>
        [Required(ErrorMessage = "El hospital es requerido")]
        [Display(Name = "Hospital")]
        public int? HospCode { get; set; }

        /// <summary>Indica si el departamento hospitalario está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de hospitales para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Hospitals { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar un cargo o rol clínico dentro de un departamento hospitalario.
    /// Incluye la lista de departamentos para el selector.
    /// </summary>
    public class HospitalRoleViewModel
    {
        /// <summary>Clave primaria del cargo clínico. Cero indica creación.</summary>
        public int HospRoleCode { get; set; }

        /// <summary>Nombre del cargo clínico (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Cargo / Rol")]
        [StringLength(45)]
        public string HospRoleName { get; set; } = string.Empty;

        /// <summary>FK al departamento hospitalario al que pertenece este cargo.</summary>
        [Required(ErrorMessage = "El departamento es requerido")]
        [Display(Name = "Departamento")]
        public int? HospDepCode { get; set; }

        /// <summary>Indica si el cargo clínico está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de departamentos hospitalarios para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> HospitalDepartments { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar un contacto clínico (médico, técnico u otro) dentro de un hospital.
    /// Incluye la lista de cargos disponibles para el selector.
    /// </summary>
    public class HospitalDoctorViewModel
    {
        /// <summary>Clave primaria del contacto clínico. Cero indica creación.</summary>
        public int HospDocCode { get; set; }

        /// <summary>Nombre del contacto (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre")]
        [StringLength(45)]
        public string HospDocName { get; set; } = string.Empty;

        /// <summary>Apellido del contacto (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El apellido es requerido")]
        [Display(Name = "Apellido")]
        [StringLength(45)]
        public string HospDocSurname { get; set; } = string.Empty;

        /// <summary>Correo electrónico del contacto (máx. 60 caracteres).</summary>
        [Display(Name = "Correo Electronico")]
        [EmailAddress(ErrorMessage = "Correo invalido")]
        [StringLength(60)]
        public string? HospDocMail { get; set; }

        /// <summary>FK al cargo clínico que ocupa este contacto.</summary>
        [Required(ErrorMessage = "El cargo es requerido")]
        [Display(Name = "Cargo")]
        public int? HospRoleCode { get; set; }

        /// <summary>Datos adicionales del contacto (máx. 100 caracteres).</summary>
        [Display(Name = "Datos Adicionales")]
        [StringLength(100)]
        public string? HospDocAddata { get; set; }

        /// <summary>Indica si el contacto está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de cargos clínicos para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> HospitalRoles { get; set; } = [];
    }
}
