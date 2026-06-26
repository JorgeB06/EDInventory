using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Documento adjunto a un equipo IT o activo clínico.
    /// EntityType = "TI" → vinculado a ItEquip; "SVC" → vinculado a EngAsset.
    /// Tabla: TB_DOCUMENT
    /// </summary>
    [Table("TB_DOCUMENT")]
    public class Document
    {
        [Key]
        [Column("DOC_CODE")]
        public int DocCode { get; set; }

        /// <summary>TI | SVC</summary>
        [Column("ENTITY_TYPE")]
        [StringLength(3)]
        public string EntityType { get; set; } = "TI";

        [Column("ITEQUIP_CODE")]
        public int? ItequipCode { get; set; }

        [Column("ASSET_CODE")]
        public int? AssetCode { get; set; }

        /// <summary>MANUAL | CERTIFICADO | ACTA | REPORTE | CONTRATO | OTRO</summary>
        [Column("DOC_TYPE")]
        [StringLength(12)]
        public string DocType { get; set; } = "OTRO";

        [Column("DOC_NAME")]
        [StringLength(150)]
        public string DocName { get; set; } = string.Empty;

        [Column("DOC_FILE_PATH")]
        [StringLength(300)]
        public string DocFilePath { get; set; } = string.Empty;

        [Column("DOC_FILE_SIZE")]
        public int? DocFileSize { get; set; }

        [Column("DOC_MIME_TYPE")]
        [StringLength(80)]
        public string? DocMimeType { get; set; }

        [Column("DOC_UPLOAD_DATE")]
        public DateTime DocUploadDate { get; set; } = DateTime.Now;

        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        [Column("DOC_NOTES")]
        [StringLength(200)]
        public string? DocNotes { get; set; }

        // ── Navegación ─────────────────────────────────────────
        [ForeignKey("ItequipCode")]
        public ItEquip? ItEquip { get; set; }

        [ForeignKey("AssetCode")]
        public EngAsset? EngAsset { get; set; }

        [ForeignKey("UserCode")]
        public User? User { get; set; }
    }
}
