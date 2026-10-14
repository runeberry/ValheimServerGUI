using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ValheimServerGUI.Tools.Logging;
using Xunit;

namespace ValheimServerGUI.Core.Tests.Tools;

// String substitutions are rendered literally (Serilog's default quotes scalar strings, which double-quotes
// a template that already quotes the token). Quotes should only ever come from the template.
public class LoggerRenderingTests
{
    private static readonly IServiceProvider Core =
        new ServiceCollection().AddValheimCore().BuildServiceProvider();

    [Fact]
    public void String_substitutions_are_not_auto_quoted()
    {
        var logger = Core.GetRequiredService<IApplicationLogger>();
        var name = $"Prof-{Guid.NewGuid():N}";

        logger.Information("Loading profile '{name}' on port {port}", name, 2456);

        var line = logger.LogBuffer.Last(l => l.Contains(name));
        Assert.Contains($"Loading profile '{name}' on port 2456", line);
        Assert.DoesNotContain($"\"{name}\"", line);   // no Serilog auto-quotes
    }
}
