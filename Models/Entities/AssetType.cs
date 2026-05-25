using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Tipo específico de activo dentro de una categoría genérica
    /// (ej. Laptop, Impresora, Ultrasonido).
    /// Nivel 2 de jerarquía: GenAssetType → <b>AssetType</b> → Brand → Model.
    /// Tabla: TB_ASSETS_TYPE
    /// </summary>
    [Table("TB_ASSETS_TYPE")]
    public class AssetType
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("ASSETS_TYPE_CODE")]
        public int AssetsTypeCode { get; set; }

        /// <summary>Nombre del tipo de activo (máx. 100 caracteres).</summary>
        [Column("ASSETS_DESC")]
        [StringLength(100)]
        public string? AssetsDesc { get; set; }

        /// <summary>FK a la categoría genérica que agrupa este tipo.</summary>
        [Column("GEN_ASSTES_TYPE_CODE")]
        public int? GenAssetsTypeCode { get; set; }

        /// <summary>Indica si este tipo de activo está activo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Categoría genérica a la que pertenece este tipo de activo.</summary>
        [ForeignKey("GenAssetsTypeCode")]
        public GenAssetType? GenAssetType { get; set; }

        /// <summary>Marcas que fabrican activos de este tipo.</summary>
        public ICollection<Brand> Brands { get; set; } = [];
    }
}
