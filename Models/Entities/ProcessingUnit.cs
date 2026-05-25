using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Unidad de Procesamiento (UP) o red hospitalaria a la que pertenecen los hospitales.
    /// Permite agrupar hospitales bajo una misma administración o CCSS.
    /// Tabla: TB_PU
    /// </summary>
    [Table("TB_PU")]
    public class ProcessingUnit
    {
        /// <summary>Clave primaria (asignada manualmente, no autogenerada).</summary>
        [Key]
        [Column("PU_CODE")]
        public int PuCode { get; set; }

        /// <summary>Nombre o descripción de la unidad de procesamiento (máx. 45 caracteres).</summary>
        [Column("PU_DESC")]
        [StringLength(45)]
        public string? PuDesc { get; set; }

        /// <summary>Indica si la unidad de procesamiento está activa.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Hospitales que pertenecen a esta unidad de procesamiento.</summary>
        public ICollection<Hospital> Hospitals { get; set; } = [];
    }
}
