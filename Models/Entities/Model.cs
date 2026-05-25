using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Modelo específico de un activo bajo una marca (ej. OptiPlex 7090, LaserJet Pro M404).
    /// Nivel 4 de jerarquía: GenAssetType → AssetType → Brand → <b>Model</b>.
    /// Tabla: TB_MODEL
    /// </summary>
    [Table("TB_MODEL")]
    public class Model
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("MODEL_CODE")]
        public int ModelCode { get; set; }

        /// <summary>Nombre del modelo (máx. 45 caracteres).</summary>
        [Column("MODEL_DESC")]
        [StringLength(45)]
        public string? ModelDesc { get; set; }

        /// <summary>FK a la marca a la que pertenece este modelo.</summary>
        [Column("BRAND_CODE")]
        public int? BrandCode { get; set; }

        /// <summary>Indica si el modelo está activo en el catálogo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Marca fabricante de este modelo.</summary>
        [ForeignKey("BrandCode")]
        public Brand? Brand { get; set; }

        /// <summary>Equipos IT registrados con este modelo.</summary>
        public ICollection<ItEquip> ItEquips { get; set; } = [];
    }
}
