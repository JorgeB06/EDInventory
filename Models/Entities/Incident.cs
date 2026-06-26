using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Incidente o reparación registrado para un equipo IT o activo clínico.
    /// EntityType = "TI" → vinculado a ItEquip; "SVC" → vinculado a EngAsset.
    /// Tabla: TB_INCIDENT
    /// </summary>
    [Table("TB_INCIDENT")]
    public class Incident
    {
        [Key]
        [Column("INCIDENT_CODE")]
        public int IncidentCode { get; set; }

        /// <summary>TI | SVC</summary>
        [Column("ENTITY_TYPE")]
        [StringLength(3)]
        public string EntityType { get; set; } = "TI";

        [Column("ITEQUIP_CODE")]
        public int? ItequipCode { get; set; }

        [Column("ASSET_CODE")]
        public int? AssetCode { get; set; }

        /// <summary>CORRECTIVO | PREVENTIVO</summary>
        [Column("INCIDENT_TYPE")]
        [StringLength(12)]
        public string IncidentType { get; set; } = "CORRECTIVO";

        /// <summary>BAJA | MEDIA | ALTA | CRITICA</summary>
        [Column("INCIDENT_PRIORITY")]
        [StringLength(8)]
        public string IncidentPriority { get; set; } = "MEDIA";

        /// <summary>ABIERTA | EN_PROCESO | CERRADA | CANCELADA</summary>
        [Column("INCIDENT_STATUS")]
        [StringLength(12)]
        public string IncidentStatus { get; set; } = "ABIERTA";

        [Column("INCIDENT_TITLE")]
        [StringLength(150)]
        public string IncidentTitle { get; set; } = string.Empty;

        [Column("INCIDENT_DESC")]
        public string? IncidentDesc { get; set; }

        [Column("INCIDENT_RESOLUTION")]
        public string? IncidentResolution { get; set; }

        [Column("REPORTED_USER")]
        public int? ReportedUser { get; set; }

        [Column("ASSIGNED_USER")]
        public int? AssignedUser { get; set; }

        [Column("INCIDENT_DATE")]
        public DateOnly IncidentDate { get; set; }

        [Column("CLOSED_DATE")]
        public DateOnly? ClosedDate { get; set; }

        [Column("INCIDENT_NOTES")]
        [StringLength(300)]
        public string? IncidentNotes { get; set; }

        // ── Navegación ─────────────────────────────────────────
        [ForeignKey("ItequipCode")]
        public ItEquip? ItEquip { get; set; }

        [ForeignKey("AssetCode")]
        public EngAsset? EngAsset { get; set; }

        [ForeignKey("ReportedUser")]
        public User? Reporter { get; set; }

        [ForeignKey("AssignedUser")]
        public User? Assignee { get; set; }
    }
}
