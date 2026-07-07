using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EDInventory.Models.ViewModels
{
    /// <summary>ViewModel para crear una sesión de auditoría de inventario (alcance caja o línea).</summary>
    public class AuditSessionCreateVM
    {
        /// <summary>Alcance de la sesión. Valores: <c>"CAJA"</c> o <c>"LINEA"</c>.</summary>
        [Required(ErrorMessage = "Seleccione el alcance.")]
        public string AuditScope { get; set; } = "CAJA";

        /// <summary>Línea a auditar (obligatorio si el alcance es LINEA).</summary>
        public int? LineCode { get; set; }

        /// <summary>Caja a auditar (obligatorio si el alcance es CAJA).</summary>
        public int? BoxCode { get; set; }

        [Required(ErrorMessage = "Seleccione el usuario asignado.")]
        public int? AssignedUserCode { get; set; }

        [StringLength(300)]
        public string? Notes { get; set; }

        public IEnumerable<SelectListItem> Lines { get; set; } = [];
        public IEnumerable<SelectListItem> Boxes { get; set; } = [];
        public IEnumerable<SelectListItem> Users { get; set; } = [];
    }

    /// <summary>ViewModel para reasignar una sesión de auditoría a otro usuario.</summary>
    public class AuditReassignVM
    {
        public int AuditCode { get; set; }
        public string? ScopeLabel { get; set; }

        [Required(ErrorMessage = "Seleccione el usuario asignado.")]
        public int? AssignedUserCode { get; set; }

        public IEnumerable<SelectListItem> Users { get; set; } = [];
    }

    /// <summary>ViewModel para registrar el conteo físico de los repuestos de una caja auditada.</summary>
    public class ContarCajaVM
    {
        public int AuditBoxCode { get; set; }
        public int AuditCode { get; set; }
        public string? LineName { get; set; }
        public int BoxNumber { get; set; }
        public List<ContarCajaPartRow> Parts { get; set; } = [];
    }

    /// <summary>Fila de conteo de un repuesto individual dentro de <see cref="ContarCajaVM"/>.</summary>
    public class ContarCajaPartRow
    {
        public int AuditPartCode { get; set; }
        public int PartCode { get; set; }
        public string? PartName { get; set; }
        public string? PartRef { get; set; }

        /// <summary>Cantidad en sistema al momento del conteo (solo informativa).</summary>
        public int SystemQty { get; set; }

        /// <summary>Cantidad física ingresada por el usuario.</summary>
        public int? CountedQty { get; set; }
    }

    /// <summary>
    /// ViewModel para el retiro/devolución rápida desde el módulo móvil. Igual que
    /// <see cref="PartWithdrawVM"/> pero con <see cref="Confirmado"/> para el paso de
    /// confirmación cuando el stock resultante queda en umbral bajo (0 o &lt;= 2).
    /// </summary>
    public class MovilWithdrawVM
    {
        public int PartCode { get; set; }
        public int BoxCode { get; set; }
        public string? PartName { get; set; }
        public string? PartRef { get; set; }
        public int CurrentQty { get; set; }

        [Required(ErrorMessage = "Ingrese la cantidad.")]
        [Range(1, 9999, ErrorMessage = "La cantidad debe ser al menos 1.")]
        public int Qty { get; set; } = 1;

        [Required]
        public string MovType { get; set; } = "RETIRO";

        public int? HospCode { get; set; }

        [StringLength(200)]
        public string? Notes { get; set; }

        /// <summary>Marcado (por el usuario) tras ver la advertencia de stock bajo/agotado.</summary>
        public bool Confirmado { get; set; }

        public IEnumerable<SelectListItem> Hospitals { get; set; } = [];
    }
}
