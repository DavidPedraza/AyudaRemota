using RemoteCare.Api.Models;
using RemoteCare.Api.Dtos.Auth;
using RemoteCare.Api.Data;
using RemoteCare.Api.Utilities;
using RemoteCare.Api.Exceptions;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace RemoteCare.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task LogoutAsync(int userId, string refreshToken);
    }

    public class AuthService : IAuthService
    {
        private readonly RemoteCareContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            RemoteCareContext context,
            IJwtTokenGenerator jwtTokenGenerator,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Validar que el email no existe
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                _logger.LogWarning($"Intento de registro con email existente: {request.Email}");
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Email", new[] { "El correo ya está registrado" } }
                });
            }

            // Validar contraseña
            var passwordValidation = ValidatePassword(request.Password);
            if (!passwordValidation.IsValid)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Password", new[] { passwordValidation.Error } }
                });
            }

            // Crear usuario
            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
                Role = request.Role ?? "Senior",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generar tokens
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            // Guardar refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Usuario registrado: {user.Email}");

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Jwt = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900 // 15 minutos
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Buscar usuario
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning($"Intento de login con email no encontrado: {request.Email}");
                throw new AuthenticationException("Email o contraseña incorrectos");
            }

            // Validar contraseña
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning($"Contraseña incorrecta para: {request.Email}");
                throw new AuthenticationException("Email o contraseña incorrectos");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning($"Intento de login con usuario inactivo: {request.Email}");
                throw new AuthenticationException("Usuario desactivado");
            }

            // Actualizar último login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generar tokens
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role);
            var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            // Guardar refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Usuario logeado: {user.Email}");

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Jwt = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900 // 15 minutos
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            // Buscar token
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null || refreshToken.IsRevoked)
            {
                _logger.LogWarning("Intento de refresh con token inválido");
                throw new AuthenticationException("Token inválido");
            }

            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Intento de refresh con token expirado");
                throw new AuthenticationException("Token expirado");
            }

            var user = refreshToken.User;

            // Generar nuevo access token
            var accessToken = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email, user.Role);
            var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            // Revocar token anterior
            refreshToken.IsRevoked = true;

            // Crear nuevo token
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(newRefreshTokenEntity);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Token refreshed para: {user.Email}");

            return new AuthResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                Jwt = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 900
            };
        }

        public async Task LogoutAsync(int userId, string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

            if (token != null)
            {
                token.IsRevoked = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Usuario deslogeado: {userId}");
            }
        }

        private (bool IsValid, string Error) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "La contraseña es requerida");

            if (password.Length < 8)
                return (false, "La contraseña debe tener mínimo 8 caracteres");

            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
                return (false, "La contraseña debe contener una mayúscula");

            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]"))
                return (false, "La contraseña debe contener una minúscula");

            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"))
                return (false, "La contraseña debe contener un número");

            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^*]"))
                return (false, "La contraseña debe contener un carácter especial");

            return (true, string.Empty);
        }
    }
}
