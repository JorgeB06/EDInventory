using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EDInventory.Models.Entities
{
    /// <summary>
    /// Cuenta de acceso al sistema. Vinculada a un <see cref="Employee"/> y a un
    /// <see cref="UserRole"/> que determina sus permisos.
    /// La contraseña se almacena como hash BCrypt (no texto plano).
    /// Tabla: TB_USERS
    /// </summary>
    [Table("TB_USERS")]
    public class User
    {
        /// <summary>Clave primaria autogenerada.</summary>
        [Key]
        [Column("USER_CODE")]
        public int UserCode { get; set; }

        /// <summary>Nombre de usuario para iniciar sesión (máx. 15 caracteres).</summary>
        [Column("USER_LOGIN")]
        [StringLength(15)]
        public string? UserLogin { get; set; }

        /// <summary>
        /// Hash BCrypt de la contraseña (máx. 72 caracteres).
        /// Nunca se almacena texto plano. Se genera con BCrypt.Net-Next.
        /// </summary>
        [Column("USER_PASSWORD")]
        [StringLength(72)]
        public string? UserPassword { get; set; }

        /// <summary>FK al empleado asociado a esta cuenta.</summary>
        [Column("EMP_CODE")]
        public int? EmpCode { get; set; }

        /// <summary>FK al rol de sistema que determina los permisos del usuario.</summary>
        [Column("UROLE_CODE")]
        public int? UroleCode { get; set; }

        /// <summary>Indica si la cuenta está habilitada para iniciar sesión.</summary>
        [Column("ACTIVE")]
        public bool Active { get; set; }

        /// <summary>Empleado al que pertenece esta cuenta de usuario.</summary>
        [ForeignKey("EmpCode")]
        public Employee? Employee { get; set; }

        /// <summary>Rol de acceso al sistema asignado a este usuario.</summary>
        [ForeignKey("UroleCode")]
        public UserRole? UserRole { get; set; }
    }
}
