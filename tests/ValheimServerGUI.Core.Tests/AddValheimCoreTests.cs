using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.Core.Tests.Fakes;
using ValheimServerGUI.Game;
using ValheimServerGUI.Tools;
using Xunit;

namespace ValheimServerGUI.Core.Tests
{
    /// <summary>
    /// The composition root wires up: AddValheimCore registers the whole headless service graph, and
    /// every service resolves once the shell supplies the seams Core leaves to it (here, a fake IUserPrompt).
    /// The OS-dispatched resolvers are verified for both platforms through the injected OS flag, so the
    /// assertions hold on whichever OS runs the suite.
    /// </summary>
    public class AddValheimCoreTests
    {
        private static ServiceProvider BuildProvider() => BuildProvider(services => services.AddValheimCore());

        private static ServiceProvider BuildProvider(bool onWindows) =>
            BuildProvider(services => services.AddValheimCore(onWindows));

        private static ServiceProvider BuildProvider(System.Action<IServiceCollection> addCore)
        {
            var services = new ServiceCollection();
            addCore(services);
            // The shell supplies the UI-side seams; stub the one the Core graph consumes.
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
        [InlineData(typeof(IRuneberryApiClient))]
        [InlineData(typeof(IIpAddressProvider))]
        public void EveryCoreService_Resolves(System.Type serviceType)
        {
            using var provider = BuildProvider();
            Assert.NotNull(provider.GetRequiredService(serviceType));
        }

        [Fact]
        public void OnLinux_SelectsLinuxResolvers()
        {
            using var provider = BuildProvider(onWindows: false);

            Assert.IsType<LinuxValheimPathResolver>(provider.GetRequiredService<IValheimPathResolver>());
            Assert.IsType<LinuxSteamPathResolver>(provider.GetRequiredService<ISteamPathResolver>());
        }

        [Fact]
        public void OnWindows_SelectsWindowsResolvers()
        {
            using var provider = BuildProvider(onWindows: true);

            Assert.IsType<WindowsValheimPathResolver>(provider.GetRequiredService<IValheimPathResolver>());
            Assert.IsType<WindowsSteamPathResolver>(provider.GetRequiredService<ISteamPathResolver>());
        }
    }
}
