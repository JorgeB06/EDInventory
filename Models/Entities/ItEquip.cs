using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Equipo de tecnología (PC, impresora, UPS, etc.) del inventario TI.
    /// Puede estar ubicado en una Sede, Hospital o Bodega (mutuamente excluyentes).
    /// El cambio de ubicación registra automáticamente una entrada en <see cref="ItEquipHistory"/>.
    /// Tabla: TB_ITEQUIP
    /// </summary>
    [Table("TB_ITEQUIP")]
    public class ItEquip
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("ITEQUIP_CODE")]
        public int ItequipCode { get; set; }

        /// <summary>Descripción del equipo (máx. 100 caracteres).</summary>
        [Column("ITEQUIP_DESC")]
        [StringLength(100)]
        public string? ItequipDesc { get; set; }

        // ── Ubicacion ──────────────────────────────────────────
        /// <summary>FK a sede. Se usa cuando el equipo está en instalaciones propias.</summary>
        [Column("SITE_CODE")]
        public int? SiteCode { get; set; }

        /// <summary>FK a hospital. Se usa cuando el equipo está instalado en un cliente.</summary>
        [Column("HOSP_CODE")]
        public int? HospCode { get; set; }

        /// <summary>FK al departamento dentro del hospital (obligatorio si HospCode tiene valor).</summary>
        [Column("HOSP_DEP_CODE")]
        public int? HospDepCode { get; set; }

        /// <summary>Posición exacta dentro del departamento del hospital (ej. "Cuarto 3 - Cama 2").</summary>
        [Column("ITEQUIP_HOSP_POS")]
        [StringLength(60)]
        public string? ItequipHospPos { get; set; }

        /// <summary>FK a bodega. Se usa cuando el equipo está almacenado internamente.</summary>
        [Column("WARE_CODE")]
        public int? WareCode { get; set; }

        /// <summary>Rack o pasillo dentro de la bodega (ej. "A1").</summary>
        [Column("WARE_RACK")]
        [StringLength(30)]
        public string? WareRack { get; set; }

        /// <summary>Estante dentro del rack en la bodega.</summary>
        [Column("WARE_ESTANTE")]
        [StringLength(30)]
        public string? WareEstante { get; set; }

        /// <summary>Caja o contenedor específico dentro del estante (opcional).</summary>
        [Column("WARE_CAJA")]
        [StringLength(30)]
        public string? WareCaja { get; set; }

        // ── Identificacion ─────────────────────────────────────
        /// <summary>FK al modelo del equipo (Marca + Modelo + Tipo de activo).</summary>
        [Column("MODEL_CODE")]
        public int? ModelCode { get; set; }

        /// <summary>Número de serie del fabricante. Debe ser único en el sistema.</summary>
        [Column("ITEQUIP_SN")]
        [StringLength(40)]
        public string? ItequipSn { get; set; }

        /// <summary>FK a la licitación o contrato bajo el cual fue adquirido.</summary>
        [Column("LIC_CODE")]
        public int? LicCode { get; set; }

        /// <summary>Número interno del equipo asignado por Diagnostika.</summary>
        [Column("ITEQUIP_NUM")]
        [StringLength(25)]
        public string? ItequipNum { get; set; }

        /// <summary>Fecha de inicio de la licitación (copiada del contrato al registrar).</summary>
        [Column("ITEQUIP_DSLIC")]
        public DateOnly? ItequipDslic { get; set; }

        /// <summary>Fecha de vencimiento de la licitación asociada a este equipo.</summary>
        [Column("ITEQUIP_DELIC")]
        public DateOnly? ItequipDelic { get; set; }

        /// <summary>Número o referencia de la garantía del fabricante.</summary>
        [Column("ITEQUIP_GNUM")]
        [StringLength(40)]
        public string? ItequipGnum { get; set; }

        /// <summary>Fecha en que el equipo fue dado de baja del inventario activo.</summary>
        [Column("ITEQUIP_DJEQUIP")]
        public DateOnly? ItequipDjequip { get; set; }

        /// <summary>Especificaciones técnicas u observaciones adicionales (máx. 200 caracteres).</summary>
        [Column("ITEQUIP_ADDATA")]
        [StringLength(200)]
        public string? ItequipAddata { get; set; }

        /// <summary>Fecha en que el equipo fue registrado en el sistema.</summary>
        [Column("ITEQUIP_DNEW")]
        public DateOnly? ItequipDnew { get; set; }

        /// <summary>Fecha de la última modificación del registro.</summary>
        [Column("ITEQUIP_DMOD")]
        public DateOnly? ItequipDmod { get; set; }

        /// <summary>Indica si el equipo está activo en el inventario.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Estado del ciclo de vida: EN_SERVICIO | EN_REPARACION | EN_EVALUACION | DADO_DE_BAJA</summary>
        [Column("EQUIP_STATUS")]
        [StringLength(20)]
        public string EquipStatus { get; set; } = "EN_SERVICIO";

        // ── Responsable ────────────────────────────────────────
        [Column("RESPONSIBLE_USER")]
        public int? ResponsibleUser { get; set; }

        [Column("RESPONSIBLE_EXT")]
        [StringLength(80)]
        public string? ResponsibleExt { get; set; }

        // ── Costo / Depreciacion ───────────────────────────────
        [Column("ACQUIRE_COST")]
        public decimal? AcquireCost { get; set; }

        [Column("ACQUIRE_DATE")]
        public DateOnly? AcquireDate { get; set; }

        /// <summary>Vida util en años para depreciacion lineal.</summary>
        [Column("DEPRE_YEARS")]
        public int? DepreYears { get; set; }

        // ── Red ────────────────────────────────────────────────
        [Column("NET_HOSTNAME")]
        [StringLength(60)]
        public string? NetHostname { get; set; }

        [Column("NET_IN_DOMAIN")]
        public bool NetInDomain { get; set; }

        [Column("NET_ENABLED")]
        public bool NetEnabled { get; set; }

        [Column("NET_IP")]
        [StringLength(45)]
        public string? NetIp { get; set; }

        /// <summary>DHCP o STATIC</summary>
        [Column("NET_TYPE")]
        [StringLength(6)]
        public string? NetType { get; set; }

        // ── Navegacion ─────────────────────────────────────────
        /// <summary>Sede donde está ubicado el equipo (exclusivo con Hospital y Bodega).</summary>
        [ForeignKey("SiteCode")]
        public Site? Site { get; set; }

        /// <summary>Hospital donde está instalado el equipo.</summary>
        [ForeignKey("HospCode")]
        public Hospital? Hospital { get; set; }

        /// <summary>Departamento del hospital donde está instalado el equipo.</summary>
        [ForeignKey("HospDepCode")]
        public HospitalDepartment? HospitalDepartment { get; set; }

        /// <summary>Bodega donde está almacenado el equipo.</summary>
        [ForeignKey("WareCode")]
        public Warehouse? Warehouse { get; set; }

        /// <summary>Modelo del equipo (incluye marca y tipo de activo).</summary>
        [ForeignKey("ModelCode")]
        public Model? Model { get; set; }

        /// <summary>Licitación bajo la cual fue adquirido este equipo.</summary>
        [ForeignKey("LicCode")]
        public Licitacion? Licitacion { get; set; }

        /// <summary>Detalles técnicos adicionales del equipo.</summary>
        public ICollection<ItEquipDetail> Details { get; set; } = [];

        /// <summary>Historial de cambios de ubicación del equipo.</summary>
        public ICollection<ItEquipHistory> History { get; set; } = [];

        /// <summary>Registros de retiro/devolución del equipo.</summary>
        public ICollection<ItEquipMovement> Movements { get; set; } = [];

        /// <summary>Mantenimientos programados y completados para este equipo.</summary>
        public ICollection<ItEquipMaintenance> Maintenances { get; set; } = [];

        /// <summary>Incidentes o reparaciones registrados para este equipo.</summary>
        public ICollection<Incident> Incidents { get; set; } = [];

        /// <summary>Documentos adjuntos a este equipo.</summary>
        public ICollection<Document> Documents { get; set; } = [];
    }
}
