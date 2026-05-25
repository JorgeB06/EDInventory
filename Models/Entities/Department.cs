using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Departamento interno de la empresa dentro de una sede
    /// (ej. Recursos Humanos, Contabilidad, TI).
    /// Tabla: TB_DEP
    /// </summary>
    [Table("TB_DEP")]
    public class Department
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("DEP_CODE")]
        public int DepCode { get; set; }

        /// <summary>Nombre del departamento (máx. 100 caracteres).</summary>
        [Column("DEP_NAME")]
        [StringLength(100)]
        public string? DepName { get; set; }

        /// <summary>FK a la sede a la que pertenece este departamento.</summary>
        [Column("SITE_CODE")]
        public int? SiteCode { get; set; }

        /// <summary>Indica si el departamento está activo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Sede a la que pertenece este departamento.</summary>
        [ForeignKey("SiteCode")]
        public Site? Site { get; set; }

        /// <summary>Puestos de trabajo definidos dentro de este departamento.</summary>
        public ICollection<Role> Roles { get; set; } = [];
    }
}
