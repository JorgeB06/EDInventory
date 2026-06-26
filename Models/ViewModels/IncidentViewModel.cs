using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EDInventory.Models.ViewModels
{
    public class IncidentViewModel
    {
        public int IncidentCode { get; set; }

        [Required]
        [Display(Name = "Division")]
        public string EntityType { get; set; } = "TI";

        [Display(Name = "Equipo IT")]
        public int? ItequipCode { get; set; }

        [Display(Name = "Activo Clinico")]
        public int? AssetCode { get; set; }

        [Required]
        [Display(Name = "Tipo")]
        public string IncidentType { get; set; } = "CORRECTIVO";

        [Required]
        [Display(Name = "Prioridad")]
        public string IncidentPriority { get; set; } = "MEDIA";

        [Display(Name = "Estado")]
        public string IncidentStatus { get; set; } = "ABIERTA";

        [Required]
        [Display(Name = "Titulo")]
        [StringLength(150)]
        public string IncidentTitle { get; set; } = string.Empty;

        [Display(Name = "Descripcion")]
        public string? IncidentDesc { get; set; }

        [Display(Name = "Resolucion")]
        public string? IncidentResolution { get; set; }

        [Display(Name = "Reportado por")]
        public int? ReportedUser { get; set; }

        [Display(Name = "Asignado a")]
        public int? AssignedUser { get; set; }

        [Required]
        [Display(Name = "Fecha")]
        public DateOnly IncidentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Display(Name = "Fecha de cierre")]
        public DateOnly? ClosedDate { get; set; }

        [Display(Name = "Notas")]
        [StringLength(300)]
        public string? IncidentNotes { get; set; }

        // Selectlists
        public IEnumerable<SelectListItem> ItEquips { get; set; } = [];
        public IEnumerable<SelectListItem> EngAssets { get; set; } = [];
        public IEnumerable<SelectListItem> Users { get; set; } = [];
    }

    public class CalibrationViewModel
    {
        public int CalibCode { get; set; }

        [Required]
        [Display(Name = "Activo Clinico")]
        public int AssetCode { get; set; }

        [Required]
        [Display(Name = "Fecha de Calibracion")]
        public DateOnly CalibDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Display(Name = "Proxima Calibracion")]
        public DateOnly? CalibNextDate { get; set; }

        [Display(Name = "Laboratorio")]
        [StringLength(100)]
        public string? CalibLab { get; set; }

        [Display(Name = "Numero de Certificado")]
        [StringLength(40)]
        public string? CalibCertNum { get; set; }

        [Display(Name = "Tipo de Calibracion")]
        [StringLength(60)]
        public string? CalibType { get; set; }

        [Required]
        [Display(Name = "Resultado")]
        public string CalibResult { get; set; } = "APROBADO";

        [Display(Name = "Notas")]
        [StringLength(300)]
        public string? CalibNotes { get; set; }

        public IEnumerable<SelectListItem> Assets { get; set; } = [];
    }
}
