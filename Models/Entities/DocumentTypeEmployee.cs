using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Tipo de documento de identidad de un empleado (ej. Cédula, Pasaporte, DIMEX).
    /// Tabla: TB_DOCMENT_TYPE_EMP
    /// </summary>
    [Table("TB_DOCMENT_TYPE_EMP")]
    public class DocumentTypeEmployee
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("DOCTYPE_CODE_EMP")]
        public int DoctypeCodeEmp { get; set; }

        /// <summary>Abreviatura del tipo de documento (máx. 9 caracteres, ej. "CED", "PAS").</summary>
        [Column("DOCTYPE_DES_EMP")]
        [StringLength(9)]
        public string? DoctypeDesEmp { get; set; }

        /// <summary>Indica si este tipo de documento está vigente.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Empleados que tienen este tipo de documento.</summary>
        public ICollection<Employee> Employees { get; set; } = [];
    }
}
