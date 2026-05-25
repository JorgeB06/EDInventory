using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Movimiento de stock de un repuesto de ingeniería (<see cref="EngPart"/>).
    /// Registra cada retiro o devolución de piezas, el hospital destino y la cantidad
    /// resultante en stock (<see cref="PartQtyAfter"/>).
    /// Tabla: TB_ENG_PART_MOV
    /// </summary>
    [Table("TB_ENG_PART_MOV")]
    public class EngPartMovement
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("MOV_CODE")]
        public int MovCode { get; set; }

        /// <summary>FK al repuesto afectado por este movimiento.</summary>
        [Column("PART_CODE")]
        public int PartCode { get; set; }

        /// <summary>FK al usuario que registró el movimiento.</summary>
        [Column("USER_CODE")]
        public int? UserCode { get; set; }

        /// <summary>Fecha y hora en que se realizó el movimiento.</summary>
        [Column("MOV_DATE")]
        public DateTime MovDate { get; set; }

        /// <summary>Cantidad de unidades afectadas por el movimiento (positivo para entrada, negativo para salida).</summary>
        [Column("MOV_QTY")]
        public int MovQty { get; set; }

        /// <summary>
        /// Tipo de movimiento. Valores: <c>'RETIRO'</c> (sale del stock),
        /// <c>'DEVOLUCION'</c> (regresa al stock).
        /// </summary>
        [Column("MOV_TYPE")]
        [StringLength(10)]
        public string MovType { get; set; } = "RETIRO";

        /// <summary>FK al hospital al que se destinó el repuesto (en caso de retiro).</summary>
        [Column("HOSP_CODE")]
        public int? HospCode { get; set; }

        /// <summary>Observaciones del movimiento (máx. 200 caracteres).</summary>
        [Column("MOV_NOTES")]
        [StringLength(200)]
        public string? MovNotes { get; set; }

        /// <summary>Cantidad en stock del repuesto inmediatamente después de este movimiento (snapshot de auditoría).</summary>
        [Column("PART_QTY_AFTER")]
        public int PartQtyAfter { get; set; }

        // ── Navegación ─────────────────────────────────────────

        /// <summary>Repuesto afectado por este movimiento.</summary>
        [ForeignKey("PartCode")]
        public EngPart? Part { get; set; }

        /// <summary>Usuario que registró el movimiento.</summary>
        [ForeignKey("UserCode")]
        public User? User { get; set; }

        /// <summary>Hospital destino o de origen del repuesto.</summary>
        [ForeignKey("HospCode")]
        public Hospital? Hospital { get; set; }
    }
}
