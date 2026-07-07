using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Conteo físico de un repuesto dentro de una caja auditada (<see cref="EngAuditBox"/>).
    /// Se crea de forma perezosa cuando el técnico abre la caja para contar (no al crear
    /// la sesión), de modo que <see cref="SystemQty"/> sea un snapshot fresco del momento
    /// real del conteo. Las discrepancias (<see cref="Variance"/> != 0) requieren aprobación
    /// de un admin antes de ajustar <see cref="EngPart.PartQty"/>.
    /// Tabla: TB_ENG_AUDIT_PART
    /// </summary>
    [Table("TB_ENG_AUDIT_PART")]
    public class EngAuditPart
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("AUDIT_PART_CODE")]
        public int AuditPartCode { get; set; }

        /// <summary>FK a la caja auditada que contiene este repuesto.</summary>
        [Column("AUDIT_BOX_CODE")]
        public int AuditBoxCode { get; set; }

        /// <summary>FK al repuesto contado.</summary>
        [Column("PART_CODE")]
        public int PartCode { get; set; }

        /// <summary>Cantidad en sistema (snapshot de <see cref="EngPart.PartQty"/>) en el instante del conteo.</summary>
        [Column("SYSTEM_QTY")]
        public int SystemQty { get; set; }

        /// <summary>Cantidad física ingresada por el usuario que contó. Nulo hasta que se registra el conteo.</summary>
        [Column("COUNTED_QTY")]
        public int? CountedQty { get; set; }

        /// <summary>Diferencia (<see cref="CountedQty"/> - <see cref="SystemQty"/>), persistida al guardar el conteo.</summary>
        [Column("VARIANCE")]
        public int? Variance { get; set; }

        /// <summary>
        /// Estado de resolución de la diferencia. Valores: <c>PENDIENTE</c> (por revisar),
        /// <c>SIN_DIFERENCIA</c> (varianza cero, no requiere acción), <c>APROBADO_AJUSTE</c>
        /// (un admin aprobó ajustar el stock), <c>RECHAZADO</c> (un admin descartó el ajuste).
        /// </summary>
        [Column("RESOLUTION_STATUS")]
        [StringLength(20)]
        public string ResolutionStatus { get; set; } = "PENDIENTE";

        /// <summary>Notas de la resolución (justificación de aprobación o rechazo).</summary>
        [Column("RESOLUTION_NOTES")]
        [StringLength(200)]
        public string? ResolutionNotes { get; set; }

        /// <summary>FK al movimiento de ajuste generado al aprobar la discrepancia (MovType = 'AJUSTE').</summary>
        [Column("MOV_CODE")]
        public int? MovCode { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Caja auditada que contiene este repuesto.</summary>
        [ForeignKey("AuditBoxCode")]
        public EngAuditBox? AuditBox { get; set; }

        /// <summary>Repuesto contado.</summary>
        [ForeignKey("PartCode")]
        public EngPart? Part { get; set; }

        /// <summary>Movimiento de ajuste generado al aprobar la discrepancia.</summary>
        [ForeignKey("MovCode")]
        public EngPartMovement? Movement { get; set; }
    }
}
