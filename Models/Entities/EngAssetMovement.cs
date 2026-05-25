using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Registro de retiro o devolución de un activo clínico de ingeniería a/desde un hospital.
    /// Captura los movimientos operativos del activo (préstamo temporal, devolución tras
    /// reparación, etc.), complementando el historial de ubicación en <see cref="EngAssetHistory"/>.
    /// Tabla: TB_ENG_ASSET_MOV
    /// </summary>
    [Table("TB_ENG_ASSET_MOV")]
    public class EngAssetMovement
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("MOV_CODE")]
        public int MovCode { get; set; }

        /// <summary>FK al activo clínico involucrado en el movimiento.</summary>
        [Column("ASSET_CODE")]
        public int AssetCode { get; set; }

        /// <summary>FK al usuario que registró el movimiento.</summary>
        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        /// <summary>Fecha y hora en que se registró el movimiento.</summary>
        [Column("MOV_DATE")]
        public DateTime MovDate { get; set; }

        /// <summary>
        /// Tipo de movimiento. Valores: <c>'RETIRO'</c> (sale del inventario activo),
        /// <c>'DEVOLUCION'</c> (regresa al inventario activo).
        /// </summary>
        [Column("MOV_TYPE")]
        [StringLength(10)]
        public string MovType { get; set; } = "RETIRO";

        /// <summary>FK al hospital destino o de origen del movimiento.</summary>
        [Column("HOSP_CODE")]
        public int? HospCode { get; set; }

        /// <summary>Destino o ubicación de referencia cuando no es un hospital registrado (máx. 100 caracteres).</summary>
        [Column("MOV_DEST")]
        [StringLength(100)]
        public string? MovDest { get; set; }

        /// <summary>Observaciones adicionales del movimiento (máx. 200 caracteres).</summary>
        [Column("MOV_NOTES")]
        [StringLength(200)]
        public string? MovNotes { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Activo clínico involucrado en el movimiento.</summary>
        [ForeignKey("AssetCode")]
        public EngAsset? Asset { get; set; }

        /// <summary>Usuario que registró el movimiento.</summary>
        [ForeignKey("UserCode")]
        public User? User { get; set; }

        /// <summary>Hospital asociado al movimiento.</summary>
        [ForeignKey("HospCode")]
        public Hospital? Hospital { get; set; }
    }
}
