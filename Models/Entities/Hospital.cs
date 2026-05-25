using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Hospital o clínica cliente donde se instalan y dan servicio los equipos.
    /// Es el destino principal de equipos IT (<see cref="ItEquip"/>) y activos clínicos (<see cref="EngAsset"/>).
    /// Tabla: TB_HOSP
    /// </summary>
    [Table("TB_HOSP")]
    public class Hospital
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("HOSP_CODE")]
        public int HospCode { get; set; }

        /// <summary>Nombre del hospital o clínica (máx. 50 caracteres).</summary>
        [Column("HOSP_NAME")]
        [StringLength(50)]
        public string? HospName { get; set; }

        /// <summary>Dirección física del hospital (máx. 100 caracteres).</summary>
        [Column("HOSP_ADRESS")]
        [StringLength(100)]
        public string? HospAddress { get; set; }

        /// <summary>Indica si el hospital está activo como destino de equipos.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>FK a la unidad de procesamiento (red hospitalaria) a la que pertenece.</summary>
        [Column("PU_CODE")]
        public int? PuCode { get; set; }

        /// <summary>Unidad de procesamiento a la que pertenece este hospital.</summary>
        [ForeignKey("PuCode")]
        public ProcessingUnit? ProcessingUnit { get; set; }

        /// <summary>Departamentos o zonas dentro del hospital (Emergencias, UCI, etc.).</summary>
        public ICollection<HospitalDepartment> HospitalDepartments { get; set; } = [];

        /// <summary>Equipos IT instalados en este hospital.</summary>
        public ICollection<ItEquip> ItEquips { get; set; } = [];
    }
}
