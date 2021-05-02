using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Serverless.Controllers
{
    [ApiController]
    public class VsgController : ControllerBase
    {
        private readonly ILogger Logger;

        public VsgController(ILogger<VsgController> logger)
        {
            Logger = logger;
        }

        [HttpPost("crash-report")]
        public async Task<IActionResult> CreateCrashReport([FromBody] CrashReport request)
        {
            Logger.LogInformation("Receiving crash report");

            return Accepted(request);
        }

        [HttpGet("player-steam-info")]
        public async Task<IActionResult> GetPlayerSteamInfo([FromQuery] string steamId)
        {
            return Ok("Player steam info");
        }
    }
}
