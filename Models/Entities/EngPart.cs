using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Repuesto de ingeniería contenido en una caja (<see cref="EngBox"/>).
    /// Lleva control de stock con cantidad disponible y registro de cada retiro/devolución
    /// en <see cref="EngPartMovement"/>.
    /// Tabla: TB_ENG_PART
    /// </summary>
    [Table("TB_ENG_PART")]
    public class EngPart
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("PART_CODE")]
        public int PartCode { get; set; }

        /// <summary>FK a la caja de repuestos que contiene esta pieza.</summary>
        [Column("BOX_CODE")]
        public int BoxCode { get; set; }

        /// <summary>Referencia interna del repuesto asignada por Diagnostika (máx. 20 caracteres).</summary>
        [Column("PART_REF")]
        [StringLength(20)]
        public string? PartRef { get; set; }

        /// <summary>Referencia del fabricante o número de parte original (máx. 60 caracteres).</summary>
        [Column("PART_MFR_REF")]
        [StringLength(60)]
        public string? PartMfrRef { get; set; }

        /// <summary>Nombre descriptivo del repuesto (máx. 120 caracteres).</summary>
        [Column("PART_NAME")]
        [StringLength(120)]
        public string? PartName { get; set; }

        /// <summary>Cantidad disponible en stock. Se actualiza automáticamente con cada movimiento.</summary>
        [Column("PART_QTY")]
        public int PartQty { get; set; }

        /// <summary>Notas adicionales del repuesto, como condición o restricciones de uso (máx. 200 caracteres).</summary>
        [Column("PART_NOTES")]
        [StringLength(200)]
        public string? PartNotes { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Caja de repuestos que contiene esta pieza.</summary>
        [ForeignKey("BoxCode")]
        public EngBox? Box { get; set; }

        /// <summary>Activos con los que este repuesto es compatible.</summary>
        public ICollection<AssetPart> AssetParts { get; set; } = [];
    }
}
