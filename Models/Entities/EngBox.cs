using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Caja de repuestos de ingeniería perteneciente a una línea de servicio.
    /// Cada caja contiene una colección de repuestos (<see cref="EngPart"/>) y puede
    /// estar almacenada en una bodega con ubicación física detallada (rack y estante).
    /// Tabla: TB_ENG_BOX
    /// </summary>
    [Table("TB_ENG_BOX")]
    public class EngBox
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("BOX_CODE")]
        public int BoxCode { get; set; }

        /// <summary>FK a la línea de servicio a la que pertenece esta caja.</summary>
        [Column("LINE_CODE")]
        public int LineCode { get; set; }

        /// <summary>Número identificador de la caja dentro de la línea de servicio.</summary>
        [Column("BOX_NUMBER")]
        public int BoxNumber { get; set; }

        /// <summary>Descripción o contenido general de la caja (máx. 150 caracteres).</summary>
        [Column("BOX_DESC")]
        [StringLength(150)]
        public string? BoxDesc { get; set; }

        /// <summary>FK a la bodega donde se almacena esta caja.</summary>
        [Column("WARE_CODE")]
        public int? WareCode { get; set; }

        /// <summary>Rack o pasillo dentro de la bodega donde se encuentra la caja (máx. 30 caracteres).</summary>
        [Column("WARE_RACK")]
        [StringLength(30)]
        public string? WareRack { get; set; }

        /// <summary>Estante dentro del rack donde se encuentra la caja (máx. 30 caracteres).</summary>
        [Column("WARE_ESTANTE")]
        [StringLength(30)]
        public string? WareEstante { get; set; }

        /// <summary>Indica si la caja está activa en el inventario.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Línea de servicio a la que pertenece esta caja.</summary>
        [ForeignKey("LineCode")]
        public EngLine? Line { get; set; }

        /// <summary>Bodega donde está almacenada la caja.</summary>
        [ForeignKey("WareCode")]
        public Warehouse? Warehouse { get; set; }

        /// <summary>Repuestos contenidos en esta caja.</summary>
        public ICollection<EngPart> Parts { get; set; } = [];
    }
}
