using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Licitación o contrato de mantenimiento bajo el cual se adquirieron equipos IT.
    /// Los equipos asociados a una licitación vencida generan alertas en el Dashboard.
    /// Tabla: TB_LIC
    /// </summary>
    [Table("TB_LIC")]
    public class Licitacion
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("LIC_CODE")]
        public int LicCode { get; set; }

        /// <summary>Número oficial de la licitación (máx. 45 caracteres).</summary>
        [Column("LIC_NUM")]
        [StringLength(45)]
        public string? LicNum { get; set; }

        /// <summary>Descripción o nombre del contrato (máx. 60 caracteres).</summary>
        [Column("LIC_DESC")]
        [StringLength(60)]
        public string? LicDesc { get; set; }

        /// <summary>Fecha de inicio de vigencia del contrato.</summary>
        [Column("LIC_START")]
        public DateOnly? LicStart { get; set; }

        /// <summary>Fecha de vencimiento del contrato. Nulo si no tiene fecha definida.</summary>
        [Column("LIC_END")]
        public DateOnly? LicEnd { get; set; }

        /// <summary>
        /// Días de anticipación para mostrar la alerta de "por vencer" en Dashboard.
        /// Valor predeterminado: 180 días.
        /// </summary>
        [Column("LIC_WARN_DAYS")]
        public int LicWarnDays { get; set; } = 180;

        /// <summary>
        /// Indica si la licitación vencida fue aplazada por decisión administrativa.
        /// Una licitación aplazada no genera alerta roja, sino amarilla.
        /// </summary>
        [Column("LIC_POSTPONED")]
        public bool LicPostponed { get; set; }

        /// <summary>Justificación del aplazamiento (máx. 150 caracteres). Solo aplica si <see cref="LicPostponed"/> es <c>true</c>.</summary>
        [Column("LIC_POSTPONED_NOTE")]
        [StringLength(150)]
        public string? LicPostponedNote { get; set; }

        /// <summary>Indica si la licitación está activa en el sistema.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Equipos IT vinculados a esta licitación.</summary>
        public ICollection<ItEquip> ItEquips { get; set; } = [];
    }
}
