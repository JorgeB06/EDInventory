using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Sede física de la empresa (oficina, bodega central, etc.).
    /// Los equipos IT pueden estar asignados a una sede como ubicación primaria.
    /// Tabla: TB_SITE
    /// </summary>
    [Table("TB_SITE")]
    public class Site
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("SITE_CODE")]
        public int SiteCode { get; set; }

        /// <summary>Nombre descriptivo de la sede (máx. 45 caracteres).</summary>
        [Column("SITE_NAME")]
        [StringLength(45)]
        public string? SiteName { get; set; }

        /// <summary>Dirección física de la sede (máx. 100 caracteres).</summary>
        [Column("SITE_ADDRESS")]
        [StringLength(100)]
        public string? SiteAddress { get; set; }

        /// <summary>Indica si la sede está operativa.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>FK a la empresa dueña de esta sede.</summary>
        [Column("COMPANY_CODE")]
        public int? CompanyCode { get; set; }

        /// <summary>Empresa a la que pertenece esta sede.</summary>
        [ForeignKey("CompanyCode")]
        public Company? Company { get; set; }

        /// <summary>Departamentos internos de esta sede.</summary>
        public ICollection<Department> Departments { get; set; } = [];

        /// <summary>Equipos IT ubicados en esta sede.</summary>
        public ICollection<ItEquip> ItEquips { get; set; } = [];
    }
}
