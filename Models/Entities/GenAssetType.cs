using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Categoría genérica de activo (ej. Equipos de Cómputo, Equipos Médicos).
    /// Nivel superior de la jerarquía: GenAssetType → AssetType → Brand → Model.
    /// Tabla: TB_GEN_ASSETS_TYPE
    /// </summary>
    [Table("TB_GEN_ASSETS_TYPE")]
    public class GenAssetType
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("GEN_ASSETS_TYPE_CODE")]
        public int GenAssetsTypeCode { get; set; }

        /// <summary>Descripción de la categoría genérica (máx. 100 caracteres).</summary>
        [Column("GEN_ASSETS_DESC")]
        [StringLength(100)]
        public string? GenAssetsDesc { get; set; }

        /// <summary>Indica si la categoría está activa en el sistema.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Tipos de activo específicos que pertenecen a esta categoría genérica.</summary>
        public ICollection<AssetType> AssetTypes { get; set; } = [];

        /// <summary>Detalles adicionales asociados a esta categoría (características técnicas).</summary>
        public ICollection<DetailGat> DetailGats { get; set; } = [];
    }
}
