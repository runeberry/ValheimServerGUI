using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Serverless.Controllers
{
    [ApiController]
    public class VsgController : ControllerBase
    {
        private readonly ILogger Logger;
        private readonly IConfiguration Configuration;

        public VsgController(ILogger<VsgController> logger, IConfiguration configuration)
        {
            Logger = logger;
        }

        [HttpPost("crash-report")]
        public async Task<IActionResult> CreateCrashReport([FromBody] CrashReport request)
        {
            Logger.LogInformation("Receiving crash report");

            //var awsAccessKey = Configuration.GetValue<string>("AWSAccessKey");
            //var awsSecretKey = Configuration.GetValue<string>("AWSSecretKey");
            var s3BucketUrl = Configuration.GetValue<string>("AWSS3BucketUrl");
            
            var config = new AmazonS3Config { ServiceURL = s3BucketUrl };
            var client = new AmazonS3Client(config);

            var s3Request = new PutObjectRequest {  };
            var s3Response = await client.PutObjectAsync(s3Request);

            return StatusCode((int)s3Response.HttpStatusCode, request);
        }

        [HttpGet("player-steam-info")]
        public async Task<IActionResult> GetPlayerSteamInfo([FromQuery] string steamId)
        {
            return Ok("Player steam info");
        }
    }
}
