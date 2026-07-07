using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Sesión de auditoría (conteo físico) de repuestos de ingeniería, creada por un
    /// admin de Servicio y asignada a un usuario específico. El alcance puede ser una
    /// sola caja (<see cref="BoxCode"/>) o una línea completa (<see cref="LineCode"/>),
    /// en cuyo caso genera una fila en <see cref="EngAuditBox"/> por cada caja activa.
    /// Tabla: TB_ENG_AUDIT_SESSION
    /// </summary>
    [Table("TB_ENG_AUDIT_SESSION")]
    public class EngAuditSession
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("AUDIT_CODE")]
        public int AuditCode { get; set; }

        /// <summary>Alcance de la sesión. Valores: <c>'CAJA'</c> o <c>'LINEA'</c>.</summary>
        [Column("AUDIT_SCOPE")]
        [StringLength(6)]
        public string AuditScope { get; set; } = "CAJA";

        /// <summary>FK a la línea de servicio auditada (obligatorio si el alcance es LINEA).</summary>
        [Column("LINE_CODE")]
        public int? LineCode { get; set; }

        /// <summary>FK a la caja auditada (obligatorio si el alcance es CAJA).</summary>
        [Column("BOX_CODE")]
        public int? BoxCode { get; set; }

        /// <summary>FK al usuario asignado para ejecutar el conteo (técnico o admin).</summary>
        [Column("ASSIGNED_USER_CODE")]
        public int AssignedUserCode { get; set; }

        /// <summary>FK al admin que creó la sesión.</summary>
        [Column("CREATED_BY_USER_CODE")]
        public int CreatedByUserCode { get; set; }

        /// <summary>Fecha y hora de creación de la sesión.</summary>
        [Column("CREATED_DATE")]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Estado de la sesión. Valores: <c>ASIGNADA</c> → <c>EN_PROGRESO</c> →
        /// <c>PENDIENTE_APROBACION</c> → <c>CERRADA</c>, o <c>CANCELADA</c> en cualquier momento previo.
        /// </summary>
        [Column("STATUS")]
        [StringLength(20)]
        public string Status { get; set; } = "ASIGNADA";

        /// <summary>Fecha en que el usuario asignado inició el conteo (primer acceso).</summary>
        [Column("STARTED_DATE")]
        public DateTime? StartedDate { get; set; }

        /// <summary>Fecha en que se contaron todas las cajas del alcance.</summary>
        [Column("COMPLETED_DATE")]
        public DateTime? CompletedDate { get; set; }

        /// <summary>FK al admin que revisó/aprobó las discrepancias.</summary>
        [Column("REVIEWED_BY_USER_CODE")]
        public int? ReviewedByUserCode { get; set; }

        /// <summary>Fecha en que se cerró la sesión.</summary>
        [Column("REVIEWED_DATE")]
        public DateTime? ReviewedDate { get; set; }

        /// <summary>Notas generales de la sesión (máx. 300 caracteres).</summary>
        [Column("NOTES")]
        [StringLength(300)]
        public string? Notes { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Línea de servicio auditada (alcance LINEA).</summary>
        [ForeignKey("LineCode")]
        public EngLine? Line { get; set; }

        /// <summary>Caja auditada (alcance CAJA).</summary>
        [ForeignKey("BoxCode")]
        public EngBox? Box { get; set; }

        /// <summary>Usuario asignado para ejecutar el conteo.</summary>
        [ForeignKey("AssignedUserCode")]
        public User? AssignedUser { get; set; }

        /// <summary>Admin que creó la sesión.</summary>
        [ForeignKey("CreatedByUserCode")]
        public User? CreatedByUser { get; set; }

        /// <summary>Admin que revisó/cerró la sesión.</summary>
        [ForeignKey("ReviewedByUserCode")]
        public User? ReviewedByUser { get; set; }

        /// <summary>Cajas incluidas en el alcance de esta sesión.</summary>
        public ICollection<EngAuditBox> AuditBoxes { get; set; } = [];
    }
}
