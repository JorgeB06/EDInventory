using System.ComponentModel.DataAnnotations;

namespace EDInventory.Models.ViewModels
{
    /// <summary>
    /// ViewModel utilizado por el formulario de cambio de contraseña del usuario autenticado.
    /// Valida la contraseña actual antes de permitir el cambio; la nueva se hashea con BCrypt.
    /// </summary>
    public class ChangePasswordViewModel
    {
        /// <summary>Contraseña actual del usuario. Se verifica contra el hash BCrypt en BD.</summary>
        [Required(ErrorMessage = "Ingrese su contrasena actual.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>Nueva contraseña deseada. Mínimo 6 caracteres.</summary>
        [Required(ErrorMessage = "Ingrese la nueva contrasena.")]
        [MinLength(6, ErrorMessage = "La contrasena debe tener al menos 6 caracteres.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>Confirmación de la nueva contraseña. Debe ser idéntica a <see cref="NewPassword"/>.</summary>
        [Required(ErrorMessage = "Confirme la nueva contrasena.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Las contrasenas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
