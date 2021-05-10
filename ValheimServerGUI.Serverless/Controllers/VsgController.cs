using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.Serverless.Controllers
{
    [ApiController]
    public class VsgController : ControllerBase
    {
        private readonly ILogger Logger;
        private readonly IConfiguration Configuration;
        
        private ILambdaContext LambdaContext => this.HttpContext.Items["LambdaContext"] as ILambdaContext;

        public VsgController(ILogger<VsgController> logger, IConfiguration configuration)
        {
            Logger = logger;
            Configuration = configuration;
        }

        [HttpPost("crash-report")]
        public async Task<IActionResult> CreateCrashReport([FromBody] CrashReport request)
        {
            Logger.LogInformation("Receiving crash report (standard logger)");

            Exception exception;
            int statusCode;

            try
            {
                var s3BucketName = Configuration.GetValue<string>("S3BucketName");
                var s3BucketRegion = Configuration.GetValue<string>("S3BucketRegion");

                var client = new AmazonS3Client(RegionEndpoint.GetBySystemName(s3BucketRegion));

                // Ensure that each crash report has an ID
                request.CrashReportId ??= Guid.NewGuid().ToString();
                request.Source ??= "CrashReport";
                request.Timestamp ??= DateTimeOffset.UtcNow;
                var filename = $"{request.Source}-{request.Timestamp.Value.ToFileTime()}-{request.CrashReportId}.json";

                var s3Request = new PutObjectRequest
                {
                    BucketName = s3BucketName,
                    Key = $"crash-reports/{filename}",
                    ContentType = "application/json",
                    ContentBody = JsonConvert.SerializeObject(request),
                };
                var s3Response = await client.PutObjectAsync(s3Request);

                Logger.LogInformation($"Crash report created: {request.CrashReportId}");

                return Accepted(request);
            }
            catch (AmazonS3Exception e)
            {
                exception = e;
                statusCode = (int)e.StatusCode;
            }
            catch (Exception e)
            {
                exception = e;
                statusCode = 500;
            }

            Logger.LogException(exception, $"{exception.GetType().Name} occurred during S3 upload");
            Logger.LogError(exception.Message);
            Logger.LogError(exception.StackTrace);

            return StatusCode(statusCode, new { message = exception.Message });
        }

        [HttpGet("player-steam-info")]
        public async Task<IActionResult> GetPlayerSteamInfo([FromQuery] string steamId)
        {
            return Ok("Player steam info");
        }
    }
}
