using Microsoft.AspNetCore.Mvc;
using RemoteCare.Api.Services;
using RemoteCare.Api.Dtos.Auth;
using RemoteCare.Api.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace RemoteCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Registrar nuevo usuario
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _authService.RegisterAsync(request);
                return CreatedAtAction(nameof(Register), response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario");
                return StatusCode(500, new { error = "Error al registrar usuario" });
            }
        }

        /// <summary>
        /// Iniciar sesión
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión");
                return StatusCode(500, new { error = "Error al iniciar sesión" });
            }
        }

        /// <summary>
        /// Refrescar token JWT
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var response = await _authService.RefreshTokenAsync(request);
                return Ok(response);
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al refrescar token");
                return StatusCode(500, new { error = "Error al refrescar token" });
            }
        }

        /// <summary>
        /// Cerrar sesión
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                await _authService.LogoutAsync(userId, request.RefreshToken);
                return Ok(new { message = "Sesión cerrada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesión");
                return StatusCode(500, new { error = "Error al cerrar sesión" });
            }
        }
    }
}
