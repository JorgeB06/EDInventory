using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel para crear o editar un empleado de la organización.
    /// Incluye listas de tipos de documento y puestos para los selectores.
    /// </summary>
    public class EmployeeViewModel
    {
        /// <summary>Clave primaria del empleado. Cero indica creación.</summary>
        public int EmpCode { get; set; }

        /// <summary>Nombre del empleado (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El nombre es requerido")]
        [Display(Name = "Nombre")]
        [StringLength(45)]
        public string EmpName { get; set; } = string.Empty;

        /// <summary>Apellido del empleado (máx. 45 caracteres).</summary>
        [Required(ErrorMessage = "El apellido es requerido")]
        [Display(Name = "Apellido")]
        [StringLength(45)]
        public string EmpSurname { get; set; } = string.Empty;

        /// <summary>FK al tipo de documento de identidad del empleado.</summary>
        [Display(Name = "Tipo de Documento")]
        public int? DoctypeCodeEmp { get; set; }

        /// <summary>Número del documento de identidad (máx. 9 caracteres).</summary>
        [Display(Name = "Numero de Documento")]
        [StringLength(9)]
        public string? EmpDocnum { get; set; }

        /// <summary>Correo electrónico corporativo del empleado (máx. 60 caracteres).</summary>
        [Display(Name = "Correo Electronico")]
        [EmailAddress(ErrorMessage = "Correo invalido")]
        [StringLength(60)]
        public string? EmpEmail { get; set; }

        /// <summary>FK al puesto de trabajo que ocupa el empleado.</summary>
        [Display(Name = "Puesto / Rol")]
        public int? RoleCode { get; set; }

        /// <summary>Datos adicionales del empleado (máx. 100 caracteres).</summary>
        [Display(Name = "Datos Adicionales")]
        [StringLength(100)]
        public string? EmpAddata { get; set; }

        /// <summary>Fecha de ingreso del empleado a la organización.</summary>
        [Display(Name = "Fecha de Ingreso")]
        public DateOnly? EmpHighDate { get; set; }

        /// <summary>Fecha de salida o baja del empleado. Nulo si sigue activo.</summary>
        [Display(Name = "Fecha de Salida")]
        public DateOnly? EmpLeaveDate { get; set; }

        /// <summary>Indica si el empleado está activo en la organización.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de tipos de documento para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> DocumentTypes { get; set; } = [];

        /// <summary>Lista de puestos de trabajo para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Roles { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar un puesto de trabajo interno.
    /// Incluye la lista de departamentos para el selector.
    /// </summary>
    public class PositionViewModel
    {
        /// <summary>Clave primaria del puesto. Cero indica creación.</summary>
        public int RoleCode { get; set; }

        /// <summary>Nombre del puesto de trabajo (máx. 100 caracteres).</summary>
        [Required(ErrorMessage = "El nombre del puesto es requerido")]
        [Display(Name = "Puesto / Rol")]
        [StringLength(100)]
        public string RoleName { get; set; } = string.Empty;

        /// <summary>FK al departamento al que pertenece el puesto.</summary>
        [Display(Name = "Departamento")]
        public int? DepCode { get; set; }

        /// <summary>Indica si el puesto está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de departamentos para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Departments { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar una cuenta de usuario del sistema.
    /// La contraseña se hashea con BCrypt antes de persistir. El campo <see cref="Password"/>
    /// es opcional en edición (vacío = no cambiar).
    /// </summary>
    public class UserViewModel
    {
        /// <summary>Clave primaria del usuario. Cero indica creación.</summary>
        public int UserCode { get; set; }

        /// <summary>Nombre de acceso al sistema (máx. 15 caracteres, único).</summary>
        [Required(ErrorMessage = "El usuario es requerido")]
        [Display(Name = "Usuario")]
        [StringLength(15)]
        public string UserLogin { get; set; } = string.Empty;

        /// <summary>
        /// Nueva contraseña del usuario (mín. 6 caracteres, máx. 72).
        /// Vacío en edición significa que no se cambia la contraseña existente.
        /// </summary>
        [Display(Name = "Contrasena")]
        [StringLength(72, MinimumLength = 6, ErrorMessage = "Minimo 6 caracteres")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        /// <summary>Confirmación de la nueva contraseña. Debe coincidir con <see cref="Password"/>.</summary>
        [Display(Name = "Confirmar Contrasena")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contrasenas no coinciden")]
        public string? ConfirmPassword { get; set; }

        /// <summary>FK al empleado vinculado a esta cuenta de usuario.</summary>
        [Required(ErrorMessage = "El empleado es requerido")]
        [Display(Name = "Empleado")]
        public int? EmpCode { get; set; }

        /// <summary>FK al rol de sistema que determina los permisos de acceso.</summary>
        [Required(ErrorMessage = "El rol de sistema es requerido")]
        [Display(Name = "Rol de Sistema")]
        public int? UroleCode { get; set; }

        /// <summary>Indica si la cuenta de usuario está activa.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;

        /// <summary>Lista de empleados para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Employees { get; set; } = [];

        /// <summary>Lista de roles de sistema para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> UserRoles { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para crear o editar un rol de acceso al sistema
    /// (ej. TI.Administrador, Servicio.Tecnico).
    /// </summary>
    public class UserRoleViewModel
    {
        /// <summary>Clave primaria del rol de sistema. Cero indica creación.</summary>
        public int UroleCode { get; set; }

        /// <summary>Descripción del rol de sistema (máx. 45 caracteres). Debe coincidir con las constantes de <c>AppRoles</c>.</summary>
        [Required(ErrorMessage = "La descripcion es requerida")]
        [Display(Name = "Rol de Sistema")]
        [StringLength(45)]
        public string UroleDesc { get; set; } = string.Empty;

        /// <summary>Indica si el rol de sistema está activo.</summary>
        [Display(Name = "Activo")]
        public bool Active { get; set; } = true;
    }
}
