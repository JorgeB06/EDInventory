using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Marca fabricante de un activo (ej. Dell, HP, Philips).
    /// Nivel 3 de jerarquía: GenAssetType → AssetType → <b>Brand</b> → Model.
    /// Tabla: TB_BRAND
    /// </summary>
    [Table("TB_BRAND")]
    public class Brand
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("BRAND_CODE")]
        public int BrandCode { get; set; }

        /// <summary>Nombre de la marca (máx. 40 caracteres).</summary>
        [Column("BRAND_DESC")]
        [StringLength(40)]
        public string? BrandDesc { get; set; }

        /// <summary>FK al tipo de activo al que pertenece esta marca.</summary>
        [Column("ASSTES_TYPE_CODE")]
        public int? AssetsTypeCode { get; set; }

        /// <summary>Indica si la marca está activa en el catálogo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Tipo de activo al que pertenece esta marca.</summary>
        [ForeignKey("AssetsTypeCode")]
        public AssetType? AssetType { get; set; }

        /// <summary>Modelos disponibles bajo esta marca.</summary>
        public ICollection<Model> Models { get; set; } = [];
    }
}
