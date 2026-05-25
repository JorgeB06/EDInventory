using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Línea de servicio de ingeniería (ej. "Hemodiálisis", "Imágenes Diagnósticas").
    /// Agrupa las cajas de repuestos (<see cref="EngBox"/>) y los activos clínicos
    /// (<see cref="EngAsset"/>) bajo una misma especialidad técnica.
    /// Tabla: TB_ENG_LINE
    /// </summary>
    [Table("TB_ENG_LINE")]
    public class EngLine
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("LINE_CODE")]
        public int LineCode { get; set; }

        /// <summary>Nombre de la línea de servicio (máx. 60 caracteres).</summary>
        [Column("LINE_NAME")]
        [StringLength(60)]
        public string? LineName { get; set; }

        /// <summary>Agrupación o categoría superior a la que pertenece la línea (máx. 60 caracteres).</summary>
        [Column("LINE_GROUP")]
        [StringLength(60)]
        public string? LineGroup { get; set; }

        /// <summary>Descripción detallada de la línea de servicio (máx. 150 caracteres).</summary>
        [Column("LINE_DESC")]
        [StringLength(150)]
        public string? LineDesc { get; set; }

        /// <summary>Indica si la línea de servicio está activa.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Cajas de repuestos asignadas a esta línea de servicio.</summary>
        public ICollection<EngBox> Boxes { get; set; } = [];

        /// <summary>Activos clínicos (equipos de ingeniería) asignados a esta línea.</summary>
        public ICollection<EngAsset> Assets { get; set; } = [];
    }
}
