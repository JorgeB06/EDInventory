using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Registro histórico de cada cambio de ubicación de un activo clínico de ingeniería.
    /// Generado automáticamente al modificar los campos de ubicación en <see cref="EngAsset"/>.
    /// Guarda una instantánea completa de la ubicación registrada (Sede, Hospital o Bodega).
    /// Tabla: TB_ENG_ASSET_HIST
    /// </summary>
    [Table("TB_ENG_ASSET_HIST")]
    public class EngAssetHistory
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("HIST_CODE")]
        public int HistCode { get; set; }

        /// <summary>FK al activo clínico cuya ubicación cambió.</summary>
        [Column("ASSET_CODE")]
        public int AssetCode { get; set; }

        /// <summary>Fecha y hora exacta en que se registró el cambio de ubicación.</summary>
        [Column("HIST_DATE")]
        public DateTime HistDate { get; set; }

        /// <summary>FK al usuario del sistema que realizó el cambio.</summary>
        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        /// <summary>
        /// Tipo de ubicación registrada. Valores: <c>'BODEGA'</c>, <c>'HOSPITAL'</c>, <c>'SEDE'</c>.
        /// Determina cuáles campos de ubicación tienen valor en este registro.
        /// </summary>
        [Column("LOC_TYPE")]
        [StringLength(10)]
        public string? LocType { get; set; }

        // ── Bodega ───────────────────────────────────────────────

        /// <summary>FK a la bodega donde fue ubicado el activo (solo si LocType = 'BODEGA').</summary>
        [Column("WARE_CODE")]
        public int? WareCode { get; set; }

        /// <summary>Rack o pasillo dentro de la bodega al momento del cambio.</summary>
        [Column("WARE_RACK")]
        [StringLength(30)]
        public string? WareRack { get; set; }

        /// <summary>Estante dentro del rack al momento del cambio.</summary>
        [Column("WARE_ESTANTE")]
        [StringLength(30)]
        public string? WareEstante { get; set; }

        // ── Hospital ─────────────────────────────────────────────

        /// <summary>FK al hospital donde fue instalado el activo (solo si LocType = 'HOSPITAL').</summary>
        [Column("HOSP_CODE")]
        public int? HospCode { get; set; }

        /// <summary>FK al departamento del hospital al momento del cambio.</summary>
        [Column("HOSP_DEP_CODE")]
        public int? HospDepCode { get; set; }

        /// <summary>Posición exacta dentro del departamento al momento del cambio.</summary>
        [Column("ASSET_HOSP_POS")]
        [StringLength(60)]
        public string? AssetHospPos { get; set; }

        // ── Sede ──────────────────────────────────────────────────

        /// <summary>FK a la sede donde fue ubicado el activo (solo si LocType = 'SEDE').</summary>
        [Column("SITE_CODE")]
        public int? SiteCode { get; set; }

        /// <summary>Notas o justificación del cambio de ubicación (máx. 200 caracteres).</summary>
        [Column("HIST_NOTES")]
        [StringLength(200)]
        public string? HistNotes { get; set; }

        // ── Navegación ────────────────────────────────────────────

        /// <summary>Activo clínico al que pertenece este registro de historial.</summary>
        [ForeignKey("AssetCode")]
        public EngAsset? Asset { get; set; }

        /// <summary>Usuario del sistema que registró el cambio.</summary>
        [ForeignKey("UserCode")]
        public User? User { get; set; }

        /// <summary>Bodega destino del movimiento.</summary>
        [ForeignKey("WareCode")]
        public Warehouse? Warehouse { get; set; }

        /// <summary>Hospital destino del movimiento.</summary>
        [ForeignKey("HospCode")]
        public Hospital? Hospital { get; set; }

        /// <summary>Departamento del hospital destino del movimiento.</summary>
        [ForeignKey("HospDepCode")]
        public HospitalDepartment? HospitalDepartment { get; set; }

        /// <summary>Sede destino del movimiento.</summary>
        [ForeignKey("SiteCode")]
        public Site? Site { get; set; }
    }
}
