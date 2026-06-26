using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Relacion entre un activo clinico y un repuesto compatible.
    /// Indica que el repuesto puede ser utilizado en ese activo.
    /// Tabla: TB_ASSET_PART
    /// </summary>
    [Table("TB_ASSET_PART")]
    public class AssetPart
    {
        [Key]
        [Column("AP_CODE")]
        public int ApCode { get; set; }

        [Column("ASSET_CODE")]
        public int AssetCode { get; set; }

        [Column("PART_CODE")]
        public int PartCode { get; set; }

        [Column("AP_NOTES")]
        [StringLength(200)]
        public string? ApNotes { get; set; }

        [ForeignKey("AssetCode")]
        public EngAsset? Asset { get; set; }

        [ForeignKey("PartCode")]
        public EngPart? Part { get; set; }
    }
}
