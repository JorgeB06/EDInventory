using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel para programar un nuevo mantenimiento en un activo clínico o equipo IT.
    /// Usado tanto por <c>EngineeringController</c> (EngAsset) como por <c>EquipmentController</c> (ItEquip).
    /// </summary>
    public class MaintCreateVM
    {
        /// <summary>
        /// Código del activo o equipo al que se le programa el mantenimiento.
        /// Puede ser <c>AssetCode</c> (EngAsset) o <c>ItequipCode</c> (ItEquip) según el controlador.
        /// </summary>
        public int AssetCode { get; set; }

        /// <summary>Descripción del activo o equipo, solo para mostrar en la vista.</summary>
        public string? AssetDesc { get; set; }

        /// <summary>
        /// Tipo de mantenimiento. Valores típicos: <c>'PREVENTIVO'</c>, <c>'CORRECTIVO'</c>.
        /// </summary>
        [Required]
        public string MaintType { get; set; } = "PREVENTIVO";

        /// <summary>Fecha programada para la ejecución del mantenimiento. Por defecto: 7 días desde hoy.</summary>
        [Required(ErrorMessage = "Ingrese la fecha programada.")]
        public DateOnly MaintScheduled { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        /// <summary>FK al usuario técnico que ejecutará el mantenimiento.</summary>
        public int? UserCode { get; set; }

        /// <summary>Instrucciones o descripción del trabajo a realizar (máx. 200 caracteres).</summary>
        [StringLength(200)]
        public string? MaintNotes { get; set; }

        /// <summary>Lista de técnicos disponibles para asignación en el selector desplegable.</summary>
        public IEnumerable<SelectListItem> Technicians { get; set; } = [];
    }

    /// <summary>
    /// ViewModel para completar o cerrar un mantenimiento programado.
    /// Registra la fecha real de finalización y el resultado del trabajo.
    /// </summary>
    public class MaintCompleteVM
    {
        /// <summary>Clave primaria del mantenimiento que se está completando.</summary>
        public int MaintCode { get; set; }

        /// <summary>Código del activo o equipo asociado al mantenimiento.</summary>
        public int AssetCode { get; set; }

        /// <summary>Descripción del activo o equipo, solo para mostrar en la vista.</summary>
        public string? AssetDesc { get; set; }

        /// <summary>Tipo del mantenimiento (PREVENTIVO / CORRECTIVO), solo informativo.</summary>
        public string? MaintType { get; set; }

        /// <summary>Fecha originalmente programada para el mantenimiento, solo informativa.</summary>
        public DateOnly MaintScheduled { get; set; }

        /// <summary>Informe o descripción del trabajo realizado (máx. 200 caracteres).</summary>
        [StringLength(200)]
        public string? MaintResult { get; set; }

        /// <summary>Fecha real de finalización del mantenimiento. Por defecto: hoy.</summary>
        public DateOnly MaintCompleted { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    }
}
