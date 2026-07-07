using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Registro de calibración de un activo clínico (Servicio).
    /// Tabla: TB_CALIBRATION
    /// </summary>
    [Table("TB_CALIBRATION")]
    public class Calibration
    {
        [Key]
        [Column("CALIB_CODE")]
        public int CalibCode { get; set; }

        [Column("ASSET_CODE")]
        public int AssetCode { get; set; }

        [Column("CALIB_DATE")]
        public DateOnly CalibDate { get; set; }

        [Column("CALIB_NEXT_DATE")]
        public DateOnly? CalibNextDate { get; set; }

        [Column("CALIB_LAB")]
        [StringLength(100)]
        public string? CalibLab { get; set; }

        [Column("CALIB_CERT_NUM")]
        [StringLength(40)]
        public string? CalibCertNum { get; set; }

        [Column("CALIB_TYPE")]
        [StringLength(60)]
        public string? CalibType { get; set; }

        /// <summary>APROBADO | RECHAZADO | CONDICIONAL</summary>
        [Column("CALIB_RESULT")]
        [StringLength(12)]
        public string CalibResult { get; set; } = "APROBADO";

        [Column("CALIB_NOTES")]
        [StringLength(300)]
        public string? CalibNotes { get; set; }

        [Column("CALIB_REG_DATE")]
        public DateTime CalibRegDate { get; set; } = DateTime.Now;

        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        /// <summary>Técnico responsable asignado por el administrador.</summary>
        [Column("TECH_CODE")]
        public int? TechCode { get; set; }

        // ── Navegación ─────────────────────────────────────────
        [ForeignKey("AssetCode")]
        public EngAsset? Asset { get; set; }

        [ForeignKey("UserCode")]
        public User? User { get; set; }

        [ForeignKey("TechCode")]
        public User? TechUser { get; set; }
    }
}
