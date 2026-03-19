namespace RemoteCare.Api.Dtos.Auth
{
    public class AuthResponse
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string Jwt { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; } // seconds
    }
}
