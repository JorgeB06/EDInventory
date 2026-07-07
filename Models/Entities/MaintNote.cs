using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Nota/observación de bitácora para un mantenimiento TI o Servicio.
    /// Tabla: TB_MAINT_NOTE
    /// MAINT_CODE no tiene FK porque puede referenciar TB_ITEQUIP_MAINT o TB_ENG_MAINT según ENTITY_TYPE.
    /// </summary>
    [Table("TB_MAINT_NOTE")]
    public class MaintNote
    {
        [Key]
        [Column("NOTE_CODE")]
        public int NoteCode { get; set; }

        /// <summary>TI = mantenimiento de equipo IT, SVC = mantenimiento de activo clínico.</summary>
        [Column("ENTITY_TYPE")]
        [StringLength(3)]
        public string EntityType { get; set; } = "TI";

        [Column("MAINT_CODE")]
        public int MaintCode { get; set; }

        [Column("NOTE_TEXT")]
        public string NoteText { get; set; } = string.Empty;

        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        [Column("NOTE_DATE")]
        public DateTime NoteDate { get; set; } = DateTime.Now;

        // ── Navegación ─────────────────────────────────────────
        [ForeignKey("UserCode")]
        public User? User { get; set; }
    }
}
