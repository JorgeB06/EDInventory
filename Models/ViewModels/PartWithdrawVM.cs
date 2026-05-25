using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel para registrar un retiro o devolución de stock de un repuesto de ingeniería.
    /// Valida que la cantidad solicitada sea positiva y muestra el stock actual
    /// para que el usuario tome la decisión informada.
    /// </summary>
    public class PartWithdrawVM
    {
        /// <summary>Clave primaria del repuesto afectado.</summary>
        public int PartCode { get; set; }

        /// <summary>Clave primaria de la caja a la que pertenece el repuesto, para redireccionamiento tras guardar.</summary>
        public int BoxCode { get; set; }

        /// <summary>Nombre del repuesto, solo para mostrar en la vista.</summary>
        public string? PartName { get; set; }

        /// <summary>Referencia interna del repuesto, solo para mostrar en la vista.</summary>
        public string? PartRef { get; set; }

        /// <summary>Cantidad disponible en stock antes del movimiento, solo informativa.</summary>
        public int CurrentQty { get; set; }

        /// <summary>Cantidad de unidades a retirar o devolver (entre 1 y 9999).</summary>
        [Required(ErrorMessage = "Ingrese la cantidad.")]
        [Range(1, 9999, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public int Qty { get; set; } = 1;

        /// <summary>
        /// Tipo de movimiento. Valores: <c>"RETIRO"</c> (reduce stock)
        /// o <c>"DEVOLUCION"</c> (aumenta stock).
        /// </summary>
        [Required]
        public string MovType { get; set; } = "RETIRO";

        /// <summary>FK al hospital al que se destina el repuesto (en caso de retiro).</summary>
        public int? HospCode { get; set; }

        /// <summary>Observaciones o justificación del movimiento (máx. 200 caracteres).</summary>
        [StringLength(200)]
        public string? Notes { get; set; }

        /// <summary>Lista de hospitales para el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Hospitals { get; set; } = [];
    }
}
