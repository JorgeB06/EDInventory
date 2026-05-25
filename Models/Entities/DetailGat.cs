using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Detalle o característica técnica asociada a una categoría genérica de activo
    /// (ej. campos personalizados por tipo de equipo).
    /// Tabla: TB_DETAILS_GAT
    /// </summary>
    [Table("TB_DETAILS_GAT")]
    public class DetailGat
    {
        /// <summary>Clave primaria autogenerada (no definida en el schema original, gestionada por EF).</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public int Id { get; set; }

        /// <summary>FK a la categoría genérica de activo a la que pertenece este detalle.</summary>
        [Column("GEN_ASSETS_TYPE_CODE")]
        public int GenAssetsTypeCode { get; set; }

        /// <summary>Nombre o etiqueta del detalle técnico (máx. 50 caracteres).</summary>
        [Column("DETAILS")]
        [StringLength(50)]
        public string? Details { get; set; }

        /// <summary>Indica si este detalle está activo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Categoría genérica de activo a la que pertenece este detalle.</summary>
        [ForeignKey("GenAssetsTypeCode")]
        public GenAssetType? GenAssetType { get; set; }
    }
}
