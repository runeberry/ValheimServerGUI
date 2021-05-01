using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        public async Task<IActionResult> CreateBugReport([FromBody] object request)
        {
            return Ok("Received bug report");
        }

        [HttpGet("player-steam-info")]
        public async Task<IActionResult> GetPlayerSteamInfo([FromQuery] object request)
        {
            return Ok("Player steam info");
        }
    }
}
