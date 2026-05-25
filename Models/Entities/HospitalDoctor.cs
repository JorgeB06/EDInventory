using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Médico, técnico o contacto clínico dentro de un hospital.
    /// Asociado a un cargo (<see cref="HospitalRole"/>) y es el interlocutor
    /// del Departamento de Ingeniería ante el hospital.
    /// Tabla: TB_HOSP_DOC
    /// </summary>
    [Table("TB_HOSP_DOC")]
    public class HospitalDoctor
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("HOSP_DOC_CODE")]
        public int HospDocCode { get; set; }

        /// <summary>Nombre del contacto (máx. 45 caracteres).</summary>
        [Column("HOSP_DOC_NAME")]
        [StringLength(45)]
        public string? HospDocName { get; set; }

        /// <summary>Apellido del contacto (máx. 45 caracteres).</summary>
        [Column("HOSP_DOC_SURNAME")]
        [StringLength(45)]
        public string? HospDocSurname { get; set; }

        /// <summary>Correo electrónico del contacto (máx. 60 caracteres).</summary>
        [Column("HOSP_DOC_MAIL")]
        [StringLength(60)]
        public string? HospDocMail { get; set; }

        /// <summary>FK al cargo clínico que ocupa este contacto.</summary>
        [Column("HOSP_ROLE_CODE")]
        public int? HospRoleCode { get; set; }

        /// <summary>Notas o datos adicionales del contacto (máx. 100 caracteres).</summary>
        [Column("HOSP_DOC_ADDATA")]
        [StringLength(100)]
        public string? HospDocAddata { get; set; }

        /// <summary>Indica si el contacto está activo.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Cargo clínico que ocupa este contacto.</summary>
        [ForeignKey("HospRoleCode")]
        public HospitalRole? HospitalRole { get; set; }
    }
}
