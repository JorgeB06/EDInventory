using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Puesto de trabajo dentro de un departamento de la empresa
    /// (ej. Técnico de TI, Jefe de Servicio). No confundir con <see cref="UserRole"/>
    /// que es el rol de acceso al sistema.
    /// Tabla: TB_ROLES
    /// </summary>
    [Table("TB_ROLES")]
    public class Role
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("ROLE_CODE")]
        public int RoleCode { get; set; }

        /// <summary>Nombre del puesto (máx. 100 caracteres).</summary>
        [Column("ROLE_NAME")]
        [StringLength(100)]
        public string? RoleName { get; set; }

        /// <summary>FK al departamento al que pertenece este puesto.</summary>
        [Column("DEP_CODE")]
        public int? DepCode { get; set; }

        /// <summary>Indica si el puesto está activo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Departamento al que pertenece este puesto.</summary>
        [ForeignKey("DepCode")]
        public Department? Department { get; set; }

        /// <summary>Empleados que ocupan este puesto.</summary>
        public ICollection<Employee> Employees { get; set; } = [];
    }
}
