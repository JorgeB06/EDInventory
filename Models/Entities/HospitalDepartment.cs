using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Departamento o zona dentro de un hospital (ej. Emergencias, UCI, Radiología).
    /// Los equipos IT y activos clínicos se ubican hasta el nivel de departamento + posición.
    /// Tabla: TB_HOSP_DEP
    /// </summary>
    [Table("TB_HOSP_DEP")]
    public class HospitalDepartment
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("HOSP_DEP_CODE")]
        public int HospDepCode { get; set; }

        /// <summary>Nombre del departamento o zona (máx. 45 caracteres).</summary>
        [Column("HOSP_DEP_NAME")]
        [StringLength(45)]
        public string? HospDepName { get; set; }

        /// <summary>FK al hospital al que pertenece este departamento.</summary>
        [Column("HOSP_CODE")]
        public int? HospCode { get; set; }

        /// <summary>Indica si el departamento está activo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Hospital al que pertenece este departamento.</summary>
        [ForeignKey("HospCode")]
        public Hospital? Hospital { get; set; }

        /// <summary>Roles o cargos hospitalarios definidos dentro de este departamento.</summary>
        public ICollection<HospitalRole> HospitalRoles { get; set; } = [];
    }
}
