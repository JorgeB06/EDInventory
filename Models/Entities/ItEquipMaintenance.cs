using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Mantenimiento programado o completado para un equipo IT.
    /// El estado puede ser <c>PENDIENTE</c>, <c>COMPLETADO</c> o <c>CANCELADO</c>.
    /// Los registros con estado PENDIENTE y fecha vencida generan alertas en el Dashboard.
    /// Tabla: TB_ITEQUIP_MAINT
    /// </summary>
    [Table("TB_ITEQUIP_MAINT")]
    public class ItEquipMaintenance
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("MAINT_CODE")]
        public int MaintCode { get; set; }

        /// <summary>FK al equipo IT al que corresponde este mantenimiento.</summary>
        [Column("ITEQUIP_CODE")]
        public int ItequipCode { get; set; }

        /// <summary>FK al usuario responsable de ejecutar el mantenimiento.</summary>
        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        /// <summary>
        /// Tipo de mantenimiento. Valores típicos: <c>'PREVENTIVO'</c>, <c>'CORRECTIVO'</c>.
        /// </summary>
        [Column("MAINT_TYPE")]
        [StringLength(20)]
        public string MaintType { get; set; } = "PREVENTIVO";

        /// <summary>
        /// Estado actual del mantenimiento.
        /// Valores: <c>'PENDIENTE'</c>, <c>'COMPLETADO'</c>, <c>'CANCELADO'</c>.
        /// </summary>
        [Column("MAINT_STATUS")]
        [StringLength(10)]
        public string MaintStatus { get; set; } = "PENDIENTE";

        /// <summary>Fecha programada para la ejecución del mantenimiento.</summary>
        [Column("MAINT_SCHEDULED")]
        public DateOnly MaintScheduled { get; set; }

        /// <summary>Fecha real en que se completó el mantenimiento. Nulo si aún está pendiente.</summary>
        [Column("MAINT_COMPLETED")]
        public DateOnly? MaintCompleted { get; set; }

        /// <summary>Instrucciones o descripción del trabajo a realizar (máx. 200 caracteres).</summary>
        [Column("MAINT_NOTES")]
        [StringLength(200)]
        public string? MaintNotes { get; set; }

        /// <summary>Resultado o informe del mantenimiento completado (máx. 200 caracteres).</summary>
        [Column("MAINT_RESULT")]
        [StringLength(200)]
        public string? MaintResult { get; set; }

        /// <summary>Fecha y hora en que se creó el registro de mantenimiento.</summary>
        [Column("CREATED_DATE")]
        public DateTime CreatedDate { get; set; }

        /// <summary>FK al usuario que creó el registro.</summary>
        [Column("CREATED_BY")]
        public int? CreatedBy { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Equipo IT al que corresponde este mantenimiento.</summary>
        [ForeignKey("ItequipCode")]
        public ItEquip? ItEquip { get; set; }

        /// <summary>Usuario responsable de ejecutar el mantenimiento.</summary>
        [ForeignKey("UserCode")]
        public User? User { get; set; }
    }
}
