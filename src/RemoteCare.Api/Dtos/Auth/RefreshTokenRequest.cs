using System.ComponentModel.DataAnnotations;

namespace RemoteCare.Api.Dtos.Auth
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; }
    }
}
