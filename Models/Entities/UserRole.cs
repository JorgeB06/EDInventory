using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Rol de acceso al sistema (ej. TI.Administrador, Servicio.Tecnico).
    /// Define qué módulos y acciones puede realizar un usuario.
    /// Los valores posibles están definidos como constantes en <see cref="EDInventory.Models.AppRoles"/>.
    /// Tabla: TB_UROLES
    /// </summary>
    [Table("TB_UROLES")]
    public class UserRole
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("UROLE_CODE")]
        public int UroleCode { get; set; }

        /// <summary>
        /// Nombre del rol de sistema (máx. 45 caracteres).
        /// Debe coincidir exactamente con las constantes de <see cref="EDInventory.Models.AppRoles"/>.
        /// </summary>
        [Column("UROLE_DESC")]
        [StringLength(45)]
        public string? UroleDesc { get; set; }

        /// <summary>Indica si este rol está habilitado.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Usuarios que tienen asignado este rol de sistema.</summary>
        public ICollection<User> Users { get; set; } = [];
    }
}
