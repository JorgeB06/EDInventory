using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Empresa propietaria de los activos registrados en el sistema (ej. Diagnostika S.A.).
    /// Tabla: TB_COMPANY
    /// </summary>
    [Table("TB_COMPANY")]
    public class Company
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("COMPANY_CODE")]
        public int CompanyCode { get; set; }

        /// <summary>Razón social de la empresa (máx. 100 caracteres).</summary>
        [Column("COMPANY_NAME")]
        [StringLength(100)]
        public string? CompanyName { get; set; }

        /// <summary>FK al tipo de documento legal (<see cref="DocumentTypeCompany"/>).</summary>
        [Column("DOCTYPE_CODE")]
        public int? DoctypeCode { get; set; }

        /// <summary>Número de documento legal (cédula jurídica, RUC, etc.).</summary>
        [Column("DOCUMENT_TYPE")]
        [StringLength(45)]
        public string? DocumentType { get; set; }

        /// <summary>Indica si la empresa está activa en el sistema.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Tipo de documento asociado a esta empresa.</summary>
        [ForeignKey("DoctypeCode")]
        public DocumentTypeCompany? DocumentTypeCompany { get; set; }

        /// <summary>Sedes físicas pertenecientes a esta empresa.</summary>
        public ICollection<Site> Sites { get; set; } = [];
    }
}
