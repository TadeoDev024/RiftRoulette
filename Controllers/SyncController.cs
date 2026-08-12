using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace RiftRoulette.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Solo usuarios autenticados; puedes agregar rol admin más adelante
    public class SyncController : ControllerBase
    {
        private readonly RiotDataService _riotDataService;

        public SyncController(RiotDataService riotDataService)
        {
            _riotDataService = riotDataService;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncSkins()
        {
            try
            {
                await _riotDataService.SyncRiotData();
                return Ok(new { message = "Sincronización completada" });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error durante la sincronización: " + ex.Message });
            }
        }
    }
}