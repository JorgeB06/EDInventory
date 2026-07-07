using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Nota/observación de bitácora para una calibración.
    /// Tabla: TB_CALIB_NOTE
    /// </summary>
    [Table("TB_CALIB_NOTE")]
    public class CalibNote
    {
        [Key]
        [Column("NOTE_CODE")]
        public int NoteCode { get; set; }

        [Column("CALIB_CODE")]
        public int CalibCode { get; set; }

        [Column("NOTE_TEXT")]
        public string NoteText { get; set; } = string.Empty;

        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        [Column("NOTE_DATE")]
        public DateTime NoteDate { get; set; } = DateTime.Now;

        // ── Navegación ─────────────────────────────────────────
        [ForeignKey("CalibCode")]
        public Calibration? Calibration { get; set; }

        [ForeignKey("UserCode")]
        public User? User { get; set; }
    }
}
