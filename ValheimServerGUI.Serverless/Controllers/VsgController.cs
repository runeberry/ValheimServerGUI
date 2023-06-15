using Amazon;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using ValheimServerGUI.Serverless.Services;
using ValheimServerGUI.Tools.Models;

namespace ValheimServerGUI.Serverless.Controllers
{
    [ApiController]
    public class VsgController : ControllerBase
    {
        private readonly ILogger Logger;
        private readonly IConfiguration Configuration;
        private readonly ISteamApiClient SteamApiClient;
        private readonly IXboxApiClient XboxApiClient;

        private ILambdaContext LambdaContext => HttpContext.Items["LambdaContext"] as ILambdaContext;

        public VsgController(
            ILogger<VsgController> logger,
            IConfiguration configuration,
            ISteamApiClient steamApiClient,
            IXboxApiClient xboxApiClient)
        {
            Logger = logger;
            Configuration = configuration;
            SteamApiClient = steamApiClient;
            XboxApiClient = xboxApiClient;
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

            Logger.LogError(exception, "An exception of type '{typeName}' occurred during S3 upload\n{message}\n{stackTrace}",
                exception.GetType().Name,
                exception.Message,
                exception.StackTrace);

            return StatusCode(statusCode, new { message = exception.Message });
        }

        [HttpGet("player-info")]
        public async Task<IActionResult> GetPlayerInfo([FromQuery] string platform, [FromQuery] string playerId)
        {
            if (string.IsNullOrWhiteSpace(platform) || string.IsNullOrWhiteSpace(playerId))
            {
                return StatusCode(400, new ErrorResponse("'platform' and 'playerId' are required"));
            }

            try
            {
                switch (platform)
                {
                    case PlayerPlatforms.Steam:
                        var steamPlayer = await SteamApiClient.GetPlayerSummary(playerId);
                        return Ok(steamPlayer);
                    case PlayerPlatforms.Xbox:
                        var xboxPlayer = await XboxApiClient.GetPlayerSummary(playerId);
                        return Ok(xboxPlayer);
                    default:
                        return StatusCode(400, new ErrorResponse($"Unsupported value: platform={platform}"));
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, new ErrorResponse(e.Message));
            }
        }
    }
}
