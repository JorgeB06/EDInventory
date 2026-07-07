using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EDInventory.Models.ViewModels
{
    /// <summary>ViewModel para reasignar un mantenimiento existente a otro técnico.</summary>
    public class MaintReassignVM
    {
        public int MaintCode { get; set; }
        public int AssetCode { get; set; }
        public string? AssetDesc { get; set; }
        public string? MaintType { get; set; }
        public DateOnly MaintScheduled { get; set; }
        public int? UserCode { get; set; }
        public IEnumerable<SelectListItem> Technicians { get; set; } = [];
    }

    /// <summary>ViewModel para creación masiva de mantenimientos por hospital.</summary>
    public class MaintBulkCreateVM
    {
        [Required(ErrorMessage = "Seleccione un hospital.")]
        public int? HospCode { get; set; }

        [Required(ErrorMessage = "Seleccione un técnico.")]
        public int? UserCode { get; set; }

        [Required]
        public string MaintType { get; set; } = "PREVENTIVO";

        [Required(ErrorMessage = "Ingrese la fecha programada.")]
        public DateOnly MaintScheduled { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        [StringLength(200)]
        public string? MaintNotes { get; set; }

        public IEnumerable<SelectListItem> Hospitals { get; set; } = [];
        public IEnumerable<SelectListItem> Technicians { get; set; } = [];
    }

    /// <summary>ViewModel para creación masiva de calibraciones por hospital.</summary>
    public class CalibBulkCreateVM
    {
        [Required(ErrorMessage = "Seleccione un hospital.")]
        public int? HospCode { get; set; }

        public int? TechCode { get; set; }

        [Required]
        public DateOnly CalibDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public DateOnly? CalibNextDate { get; set; }

        [Required]
        public string CalibResult { get; set; } = "APROBADO";

        [StringLength(60)]
        public string? CalibType { get; set; }

        [StringLength(100)]
        public string? CalibLab { get; set; }

        public IEnumerable<SelectListItem> Hospitals { get; set; } = [];
        public IEnumerable<SelectListItem> TechUsers { get; set; } = [];
    }
}
