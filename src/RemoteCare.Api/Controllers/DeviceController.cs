using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using RemoteCare.Api.Services;
using RemoteCare.Api.Dtos.Device;
using RemoteCare.Api.Exceptions;

namespace RemoteCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly IDevicePairingService _pairingService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(
            IDeviceService deviceService,
            IDevicePairingService pairingService,
            ILogger<DeviceController> logger)
        {
            _deviceService = deviceService;
            _pairingService = pairingService;
            _logger = logger;
        }

        /// <summary>
        /// Registrar nuevo dispositivo
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<DeviceDto>> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                var device = await _deviceService.RegisterDeviceAsync(userId, request);
                return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, device);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar dispositivo");
                return StatusCode(500, new { error = "Error al registrar dispositivo" });
            }
        }

        /// <summary>
        /// Generar código QR para emparejamiento
        /// </summary>
        [HttpPost("generate-qr")]
        public async Task<ActionResult<GenerateQrResponse>> GenerateQr([FromBody] GenerateQrRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                var response = await _pairingService.GenerateQrAsync(userId, request.DeviceId);
                return Ok(response);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar QR");
                return StatusCode(500, new { error = "Error al generar QR" });
            }
        }

        /// <summary>
        /// Emparejar dispositivos usando código QR
        /// </summary>
        [HttpPost("pair")]
        public async Task<ActionResult<PairDevicesResponse>> PairDevices([FromBody] PairDevicesRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                var response = await _pairingService.PairDevicesAsync(userId, request);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al emparejar dispositivos");
                return StatusCode(500, new { error = "Error al emparejar dispositivos" });
            }
        }

        /// <summary>
        /// Obtener mis dispositivos
        /// </summary>
        [HttpGet("my-devices")]
        public async Task<ActionResult<IEnumerable<DeviceDto>>> GetMyDevices([FromQuery] string role = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                var devices = await _deviceService.GetUserDevicesAsync(userId, role);
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dispositivos");
                return StatusCode(500, new { error = "Error al obtener dispositivos" });
            }
        }

        /// <summary>
        /// Obtener dispositivo por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DeviceDto>> GetDevice(int id)
        {
            try
            {
                var device = await _deviceService.GetDeviceAsync(id);
                return Ok(device);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dispositivo");
                return StatusCode(500, new { error = "Error al obtener dispositivo" });
            }
        }

        /// <summary>
        /// Eliminar dispositivo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDevice(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (userId == 0)
                    return Unauthorized();

                await _deviceService.DeleteDeviceAsync(userId, id);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar dispositivo");
                return StatusCode(500, new { error = "Error al eliminar dispositivo" });
            }
        }
    }
}
