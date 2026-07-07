using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Caja incluida en el alcance de una sesión de auditoría (<see cref="EngAuditSession"/>).
    /// Una sesión de alcance CAJA tiene una sola fila; una de alcance LINEA tiene una fila
    /// por cada caja activa de la línea.
    /// Tabla: TB_ENG_AUDIT_BOX
    /// </summary>
    [Table("TB_ENG_AUDIT_BOX")]
    public class EngAuditBox
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("AUDIT_BOX_CODE")]
        public int AuditBoxCode { get; set; }

        /// <summary>FK a la sesión de auditoría a la que pertenece esta caja.</summary>
        [Column("AUDIT_CODE")]
        public int AuditCode { get; set; }

        /// <summary>FK a la caja de repuestos auditada.</summary>
        [Column("BOX_CODE")]
        public int BoxCode { get; set; }

        /// <summary>Estado del conteo de esta caja. Valores: <c>PENDIENTE</c> | <c>CONTADA</c>.</summary>
        [Column("STATUS")]
        [StringLength(10)]
        public string Status { get; set; } = "PENDIENTE";

        /// <summary>Fecha en que se registró el conteo de esta caja.</summary>
        [Column("COUNTED_DATE")]
        public DateTime? CountedDate { get; set; }

        /// <summary>FK al usuario que efectuó el conteo de esta caja.</summary>
        [Column("COUNTED_BY_USER_CODE")]
        public int? CountedByUserCode { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Sesión de auditoría a la que pertenece esta caja.</summary>
        [ForeignKey("AuditCode")]
        public EngAuditSession? Session { get; set; }

        /// <summary>Caja de repuestos auditada.</summary>
        [ForeignKey("BoxCode")]
        public EngBox? Box { get; set; }

        /// <summary>Usuario que efectuó el conteo de esta caja.</summary>
        [ForeignKey("CountedByUserCode")]
        public User? CountedByUser { get; set; }

        /// <summary>Repuestos contados dentro de esta caja.</summary>
        public ICollection<EngAuditPart> AuditParts { get; set; } = [];
    }
}
