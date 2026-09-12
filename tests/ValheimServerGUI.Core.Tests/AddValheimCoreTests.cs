using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.Core.Tests
{
    /// <summary>
    /// The composition root wires up: AddValheimCore registers the whole headless service graph, and
    /// every service resolves once the shell supplies the bucket-B seams (here, a fake IUserPrompt).
    /// On Linux the OS-dispatched resolvers select the Linux implementations.
    /// </summary>
    public class AddValheimCoreTests
    {
        private static ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            services.AddValheimCore();
            // The shell supplies the bucket-B implementations; stub the one the Core graph consumes.
            services.AddSingleton<IUserPrompt>(new FakeUserPrompt());
            return services.BuildServiceProvider();
        }

        [Theory]
        [InlineData(typeof(ValheimServer))]
        [InlineData(typeof(IPlayerDataRepository))]
        [InlineData(typeof(IUserPreferencesProvider))]
        [InlineData(typeof(IServerPreferencesProvider))]
        [InlineData(typeof(IWorldPreferencesProvider))]
        [InlineData(typeof(ISteamCloudWorldProvider))]
        [InlineData(typeof(ISoftwareUpdateProvider))]
        [InlineData(typeof(IExceptionHandler))]
        [InlineData(typeof(IGitHubClient))]
        [InlineData(typeof(IIpAddressProvider))]
        public void EveryCoreService_Resolves(System.Type serviceType)
        {
            using var provider = BuildProvider();
            Assert.NotNull(provider.GetRequiredService(serviceType));
        }

        [Fact]
        public void OnLinux_SelectsLinuxResolvers()
        {
            using var provider = BuildProvider();

            Assert.IsType<LinuxValheimPathResolver>(provider.GetRequiredService<IValheimPathResolver>());
            Assert.IsType<LinuxSteamPathResolver>(provider.GetRequiredService<ISteamPathResolver>());
        }
    }
}
