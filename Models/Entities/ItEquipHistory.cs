using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Registro histórico de cada cambio de ubicación de un equipo IT.
    /// Se genera automáticamente al modificar los campos de ubicación en <see cref="ItEquip"/>.
    /// Guarda una instantánea completa de la ubicación anterior (Sede, Hospital o Bodega).
    /// Tabla: TB_ITEQUIP_HIST
    /// </summary>
    [Table("TB_ITEQUIP_HIST")]
    public class ItEquipHistory
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("HIST_CODE")]
        public int HistCode { get; set; }

        /// <summary>FK al equipo IT cuya ubicación cambió.</summary>
        [Column("ITEQUIP_CODE")]
        public int ItequipCode { get; set; }

        /// <summary>Fecha y hora exacta en que se registró el cambio de ubicación.</summary>
        [Column("HIST_DATE")]
        public DateTime HistDate { get; set; }

        /// <summary>FK al usuario del sistema que realizó el cambio.</summary>
        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        /// <summary>FK al empleado que realizó el cambio (alternativa al usuario del sistema).</summary>
        [Column("EMP_CODE")]
        public int? EmpCode { get; set; }

        /// <summary>
        /// Tipo de ubicación registrada. Valores: <c>'BODEGA'</c>, <c>'HOSPITAL'</c>, <c>'SEDE'</c>.
        /// Determina cuáles campos de ubicación tienen valor en este registro.
        /// </summary>
        [Column("LOC_TYPE")]
        [StringLength(10)]
        public string? LocType { get; set; }

        // ── Bodega ──────────────────────────────────────────────

        /// <summary>FK a la bodega donde fue ubicado el equipo (solo si LocType = 'BODEGA').</summary>
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

        /// <summary>Caja o contenedor dentro del estante al momento del cambio.</summary>
        [Column("WARE_CAJA")]
        [StringLength(30)]
        public string? WareCaja { get; set; }

        // ── Hospital ─────────────────────────────────────────────

        /// <summary>FK al hospital donde fue instalado el equipo (solo si LocType = 'HOSPITAL').</summary>
        [Column("HOSP_CODE")]
        public int? HospCode { get; set; }

        /// <summary>FK al departamento del hospital al momento del cambio.</summary>
        [Column("HOSP_DEP_CODE")]
        public int? HospDepCode { get; set; }

        /// <summary>Posición exacta dentro del departamento al momento del cambio.</summary>
        [Column("ITEQUIP_HOSP_POS")]
        [StringLength(60)]
        public string? ItequipHospPos { get; set; }

        // ── Sede ─────────────────────────────────────────────────

        /// <summary>FK a la sede donde fue ubicado el equipo (solo si LocType = 'SEDE').</summary>
        [Column("SITE_CODE")]
        public int? SiteCode { get; set; }

        /// <summary>Notas o justificación del cambio (máx. 200 caracteres).</summary>
        [Column("HIST_NOTES")]
        [StringLength(200)]
        public string? HistNotes { get; set; }

        /// <summary>Tipo de cambio: UBICACION | RED | DATOS | CREACION</summary>
        [Column("HIST_TYPE")]
        [StringLength(12)]
        public string? HistType { get; set; }

        // ── Snapshot de red al momento del cambio ─────────────────
        [Column("NET_HOSTNAME")]
        [StringLength(60)]
        public string? NetHostname { get; set; }

        [Column("NET_IN_DOMAIN")]
        public bool? NetInDomain { get; set; }

        [Column("NET_ENABLED")]
        public bool? NetEnabled { get; set; }

        [Column("NET_IP")]
        [StringLength(45)]
        public string? NetIp { get; set; }

        [Column("NET_TYPE")]
        [StringLength(6)]
        public string? NetType { get; set; }

        // ── Navegación ────────────────────────────────────────────

        /// <summary>Equipo IT al que pertenece este registro de historial.</summary>
        [ForeignKey("ItequipCode")]
        public ItEquip? ItEquip { get; set; }

        /// <summary>Usuario del sistema que registró el cambio.</summary>
        [ForeignKey("UserCode")]
        public User? User { get; set; }

        /// <summary>Empleado que ejecutó físicamente el cambio de ubicación.</summary>
        [ForeignKey("EmpCode")]
        public Employee? Employee { get; set; }

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
