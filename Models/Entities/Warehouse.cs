using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Bodega física donde se almacenan equipos IT, activos clínicos y cajas de repuestos.
    /// La ubicación exacta se complementa con rack y estante en cada entidad de activo.
    /// Tabla: TB_WARE
    /// </summary>
    [Table("TB_WARE")]
    public class Warehouse
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("WARE_CODE")]
        public int WareCode { get; set; }

        /// <summary>Nombre descriptivo de la bodega (máx. 80 caracteres).</summary>
        [Column("WARE_NAME")]
        [StringLength(80)]
        public string? WareName { get; set; }

        /// <summary>Descripción o ubicación adicional de la bodega (máx. 150 caracteres).</summary>
        [Column("WARE_DESC")]
        [StringLength(150)]
        public string? WareDesc { get; set; }

        /// <summary>Indica si la bodega está operativa.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Equipos IT almacenados en esta bodega.</summary>
        public ICollection<ItEquip> ItEquips { get; set; } = [];

        /// <summary>Cajas de repuestos de ingeniería almacenadas en esta bodega.</summary>
        public ICollection<EngBox> EngBoxes { get; set; } = [];
    }
}
