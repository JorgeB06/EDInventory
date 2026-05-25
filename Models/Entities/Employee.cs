using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Empleado de la empresa. Puede o no tener acceso al sistema (ver <see cref="User"/>).
    /// Almacena datos personales y laborales del colaborador.
    /// Tabla: TB_EMPLOYEES
    /// </summary>
    [Table("TB_EMPLOYEES")]
    public class Employee
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("EMP_CODE")]
        public int EmpCode { get; set; }

        /// <summary>Nombre(s) del empleado (máx. 45 caracteres).</summary>
        [Column("EMP_NAME")]
        [StringLength(45)]
        public string? EmpName { get; set; }

        /// <summary>Apellido(s) del empleado (máx. 45 caracteres).</summary>
        [Column("EMP_SURNAME")]
        [StringLength(45)]
        public string? EmpSurname { get; set; }

        /// <summary>FK al tipo de documento de identidad.</summary>
        [Column("DOCTYPE_CODE_EMP")]
        public int? DoctypeCodeEmp { get; set; }

        /// <summary>Número de documento de identidad (máx. 9 caracteres).</summary>
        [Column("EMP_DOCNUM")]
        [StringLength(9)]
        public string? EmpDocnum { get; set; }

        /// <summary>Correo electrónico corporativo (máx. 60 caracteres).</summary>
        [Column("EMP_EMAIL")]
        [StringLength(60)]
        public string? EmpEmail { get; set; }

        /// <summary>FK al puesto de trabajo que ocupa el empleado.</summary>
        [Column("ROLE_CODE")]
        public int? RoleCode { get; set; }

        /// <summary>Datos adicionales o notas del empleado (máx. 100 caracteres).</summary>
        [Column("EMP_ADDATA")]
        [StringLength(100)]
        public string? EmpAddata { get; set; }

        /// <summary>Fecha de ingreso a la empresa.</summary>
        [Column("EMP_HIGH_DATE")]
        public DateOnly? EmpHighDate { get; set; }

        /// <summary>Fecha de salida de la empresa. Nulo si aún está activo.</summary>
        [Column("EMP_LEAVE_DATE")]
        public DateOnly? EmpLeaveDate { get; set; }

        /// <summary>Indica si el empleado está activo en la empresa.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Puesto de trabajo del empleado.</summary>
        [ForeignKey("RoleCode")]
        public Role? Role { get; set; }

        /// <summary>Tipo de documento de identidad del empleado.</summary>
        [ForeignKey("DoctypeCodeEmp")]
        public DocumentTypeEmployee? DocumentTypeEmployee { get; set; }

        /// <summary>
        /// Cuenta de acceso al sistema vinculada a este empleado.
        /// Puede ser <c>null</c> si el empleado no tiene usuario.
        /// </summary>
        public User? User { get; set; }
    }
}
