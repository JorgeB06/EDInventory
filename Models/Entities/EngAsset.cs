using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Activo clínico de ingeniería (equipo médico gestionado por el Dpto. de Ingeniería).
    /// Espejo de <see cref="ItEquip"/> pero para la división Servicio.
    /// Puede estar ubicado en un Hospital (con departamento y posición), Bodega o Sede
    /// (opciones mutuamente excluyentes). El cambio de ubicación genera automáticamente
    /// una entrada en <see cref="EngAssetHistory"/>.
    /// Tabla: TB_ENG_ASSET
    /// </summary>
    [Table("TB_ENG_ASSET")]
    public class EngAsset
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("ASSET_CODE")]
        public int AssetCode { get; set; }

        /// <summary>Descripción del activo clínico (máx. 100 caracteres).</summary>
        [Column("ASSET_DESC")]
        [StringLength(100)]
        public string? AssetDesc { get; set; }

        // ── Línea de servicio ──────────────────────────────────

        /// <summary>FK a la línea de servicio de ingeniería a la que pertenece este activo.</summary>
        [Column("LINE_CODE")]
        public int? LineCode { get; set; }

        // ── Ubicación: Hospital ────────────────────────────────

        /// <summary>FK al hospital donde está instalado el activo.</summary>
        [Column("HOSP_CODE")]
        public int? HospCode { get; set; }

        /// <summary>FK al departamento del hospital donde está instalado (obligatorio si HospCode tiene valor).</summary>
        [Column("HOSP_DEP_CODE")]
        public int? HospDepCode { get; set; }

        /// <summary>Posición exacta dentro del departamento (ej. "Sala 2 - Unidad 4").</summary>
        [Column("ASSET_HOSP_POS")]
        [StringLength(60)]
        public string? AssetHospPos { get; set; }

        // ── Ubicación: Bodega ──────────────────────────────────

        /// <summary>FK a la bodega donde está almacenado el activo.</summary>
        [Column("WARE_CODE")]
        public int? WareCode { get; set; }

        /// <summary>Rack o pasillo dentro de la bodega (máx. 30 caracteres).</summary>
        [Column("WARE_RACK")]
        [StringLength(30)]
        public string? WareRack { get; set; }

        /// <summary>Estante dentro del rack en la bodega (máx. 30 caracteres).</summary>
        [Column("WARE_ESTANTE")]
        [StringLength(30)]
        public string? WareEstante { get; set; }

        // ── Ubicación: Sede ────────────────────────────────────

        /// <summary>FK a la sede donde está ubicado el activo.</summary>
        [Column("SITE_CODE")]
        public int? SiteCode { get; set; }

        // ── Identificación ─────────────────────────────────────

        /// <summary>FK al modelo del activo (Marca + Modelo + Tipo de activo).</summary>
        [Column("MODEL_CODE")]
        public int? ModelCode { get; set; }

        /// <summary>Número de serie del fabricante. Debe ser único en el sistema.</summary>
        [Column("ASSET_SN")]
        [StringLength(40)]
        public string? AssetSN { get; set; }

        /// <summary>FK a la licitación o contrato bajo el cual fue adquirido el activo.</summary>
        [Column("LIC_CODE")]
        public int? LicCode { get; set; }

        /// <summary>Número interno del activo asignado por Diagnostika.</summary>
        [Column("ASSET_NUM")]
        [StringLength(25)]
        public string? AssetNum { get; set; }

        /// <summary>Fecha de inicio de la licitación (copiada del contrato al registrar).</summary>
        [Column("ASSET_DSLIC")]
        public DateOnly? AssetDslic { get; set; }

        /// <summary>Fecha de vencimiento de la licitación asociada a este activo.</summary>
        [Column("ASSET_DELIC")]
        public DateOnly? AssetDelic { get; set; }

        /// <summary>Número o referencia de la garantía del fabricante.</summary>
        [Column("ASSET_GNUM")]
        [StringLength(40)]
        public string? AssetGnum { get; set; }

        /// <summary>Fecha en que el activo fue dado de baja del inventario activo.</summary>
        [Column("ASSET_DJEQUIP")]
        public DateOnly? AssetDjequip { get; set; }

        /// <summary>Especificaciones técnicas u observaciones adicionales (máx. 200 caracteres).</summary>
        [Column("ASSET_ADDATA")]
        [StringLength(200)]
        public string? AssetAddata { get; set; }

        /// <summary>Fecha en que el activo fue registrado en el sistema.</summary>
        [Column("ASSET_DNEW")]
        public DateOnly? AssetDnew { get; set; }

        /// <summary>Fecha de la última modificación del registro.</summary>
        [Column("ASSET_DMOD")]
        public DateOnly? AssetDmod { get; set; }

        /// <summary>Indica si el activo está activo en el inventario.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; } = true;

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Línea de servicio de ingeniería a la que pertenece este activo.</summary>
        [ForeignKey("LineCode")]
        public EngLine? Line { get; set; }

        /// <summary>Sede donde está ubicado el activo.</summary>
        [ForeignKey("SiteCode")]
        public Site? Site { get; set; }

        /// <summary>Hospital donde está instalado el activo.</summary>
        [ForeignKey("HospCode")]
        public Hospital? Hospital { get; set; }

        /// <summary>Departamento del hospital donde está instalado el activo.</summary>
        [ForeignKey("HospDepCode")]
        public HospitalDepartment? HospitalDepartment { get; set; }

        /// <summary>Bodega donde está almacenado el activo.</summary>
        [ForeignKey("WareCode")]
        public Warehouse? Warehouse { get; set; }

        /// <summary>Modelo del activo (incluye marca y tipo de activo).</summary>
        [ForeignKey("ModelCode")]
        public Model? Model { get; set; }

        /// <summary>Licitación bajo la cual fue adquirido este activo.</summary>
        [ForeignKey("LicCode")]
        public Licitacion? Licitacion { get; set; }

        /// <summary>Historial de cambios de ubicación del activo.</summary>
        public ICollection<EngAssetHistory> History { get; set; } = [];

        /// <summary>Mantenimientos programados y completados para este activo.</summary>
        public ICollection<EngMaintenance> Maintenances { get; set; } = [];

        /// <summary>Registros de retiro/devolución del activo.</summary>
        public ICollection<EngAssetMovement> Movements { get; set; } = [];
    }
}
