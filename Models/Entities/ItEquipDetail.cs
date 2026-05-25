using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Detalle técnico adicional de un equipo IT (por ejemplo, dirección IP, versión de firmware,
    /// capacidad de disco, etc.). Permite registrar características libres no contempladas en
    /// los campos estructurados de <see cref="ItEquip"/>.
    /// Tabla: TB_ITEQUIP_DETAIL
    /// </summary>
    [Table("TB_ITEQUIP_DETAIL")]
    public class ItEquipDetail
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("ITEQUIP_DETAIL_CODE")]
        public int ItequipDetailCode { get; set; }

        /// <summary>FK al equipo IT al que pertenece este detalle.</summary>
        [Column("ITEQUIP_DETAIL_ID")]
        public int? ItequipDetailId { get; set; }

        /// <summary>Texto del detalle técnico (máx. 45 caracteres).</summary>
        [Column("ITEQUIP_DETAIL")]
        [StringLength(45)]
        public string? Detail { get; set; }

        /// <summary>Equipo IT al que pertenece este detalle.</summary>
        [ForeignKey("ItequipDetailId")]
        public ItEquip? ItEquip { get; set; }
    }
}
