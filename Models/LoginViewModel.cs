using System.ComponentModel.DataAnnotations;

namespace toDo.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150, ErrorMessage = "The email address is too long.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The password is too long.")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
