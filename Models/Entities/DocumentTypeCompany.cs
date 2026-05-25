using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Tipo de documento legal de una empresa (ej. Cédula Jurídica, RUC).
    /// Tabla: TB_DOCUMENT_TYPE_COM
    /// </summary>
    [Table("TB_DOCUMENT_TYPE_COM")]
    public class DocumentTypeCompany
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("DOCTYPE_CODE")]
        public int DoctypeCode { get; set; }

        /// <summary>Descripción del tipo de documento (máx. 25 caracteres).</summary>
        [Column("DOCTYPE_DESC")]
        [StringLength(25)]
        public string? DoctypeDesc { get; set; }

        /// <summary>Indica si el tipo de documento está vigente en el sistema.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Empresas que utilizan este tipo de documento.</summary>
        public ICollection<Company> Companies { get; set; } = [];
    }
}
