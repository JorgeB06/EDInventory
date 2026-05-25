using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Cargo o rol clínico dentro de un departamento hospitalario
    /// (ej. Médico Jefe, Enfermera, Técnico de Imágenes).
    /// Permite registrar los contactos responsables de los equipos instalados.
    /// Tabla: TB_HOSP_ROLE
    /// </summary>
    [Table("TB_HOSP_ROLE")]
    public class HospitalRole
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("HOSP_ROLE_CODE")]
        public int HospRoleCode { get; set; }

        /// <summary>Nombre del cargo clínico (máx. 45 caracteres).</summary>
        [Column("HOSP_ROLE_NAME")]
        [StringLength(45)]
        public string? HospRoleName { get; set; }

        /// <summary>FK al departamento hospitalario al que pertenece este cargo.</summary>
        [Column("HOSP_DEP_CODE")]
        public int? HospDepCode { get; set; }

        /// <summary>Indica si el cargo está activo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Departamento hospitalario al que pertenece este cargo.</summary>
        [ForeignKey("HospDepCode")]
        public HospitalDepartment? HospitalDepartment { get; set; }

        /// <summary>Médicos o técnicos que ocupan este cargo.</summary>
        public ICollection<HospitalDoctor> HospitalDoctors { get; set; } = [];
    }
}
