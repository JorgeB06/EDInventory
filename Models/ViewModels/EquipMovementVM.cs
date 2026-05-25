using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel para registrar un retiro o devolución de un equipo IT o activo clínico.
    /// Es polimórfico: <see cref="EntityType"/> determina si se trata de un
    /// <c>EngAsset</c> (Servicio) o un <c>ItEquip</c> (TI).
    /// El controlador usa este ViewModel para crear el movimiento correspondiente.
    /// </summary>
    public class EquipMovementVM
    {
        /// <summary>Código del equipo o activo involucrado en el movimiento.</summary>
        public int EntityCode { get; set; }

        /// <summary>Descripción del equipo o activo, solo para mostrar en la vista.</summary>
        public string? EntityDesc { get; set; }

        /// <summary>
        /// Tipo de entidad. Valores: <c>"ASSET"</c> (EngAsset) o <c>"ITEQUIP"</c> (ItEquip).
        /// Determina en qué tabla se persiste el movimiento.
        /// </summary>
        public string EntityType { get; set; } = "ASSET";

        /// <summary>
        /// Tipo de movimiento. Valores: <c>"RETIRO"</c> (sale del inventario activo)
        /// o <c>"DEVOLUCION"</c> (regresa al inventario activo).
        /// </summary>
        [Required]
        public string MovType { get; set; } = "RETIRO";

        /// <summary>FK al hospital destino o de origen del movimiento.</summary>
        public int? HospCode { get; set; }

        /// <summary>Destino libre cuando el hospital no está registrado en el sistema (máx. 100 caracteres).</summary>
        [StringLength(100)]
        public string? MovDest { get; set; }

        /// <summary>Observaciones adicionales del movimiento (máx. 200 caracteres).</summary>
        [StringLength(200)]
        public string? MovNotes { get; set; }

        /// <summary>Lista de hospitales para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Hospitals { get; set; } = [];
    }
}
