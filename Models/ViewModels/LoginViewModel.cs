using System.ComponentModel.DataAnnotations;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel utilizado por el formulario de inicio de sesión.
    /// Contiene las credenciales del usuario para autenticación por cookie.
    /// </summary>
    public class LoginViewModel
    {
        /// <summary>Nombre de usuario del sistema (máx. 15 caracteres).</summary>
        [Required(ErrorMessage = "El usuario es requerido")]
        [Display(Name = "Usuario")]
        public string UserLogin { get; set; } = string.Empty;

        /// <summary>Contraseña del usuario. Se compara contra el hash BCrypt almacenado en BD.</summary>
        [Required(ErrorMessage = "La contrasena es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contrasena")]
        public string Password { get; set; } = string.Empty;
    }
}
