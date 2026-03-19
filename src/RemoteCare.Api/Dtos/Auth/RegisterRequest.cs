using System.ComponentModel.DataAnnotations;

namespace RemoteCare.Api.Dtos.Auth
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be 8-100 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } // Senior, Support
    }
}
