using System.ComponentModel.DataAnnotations;

namespace toDo.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Enter your name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Use between 2 and 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "The email address is too long.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Choose a password.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
