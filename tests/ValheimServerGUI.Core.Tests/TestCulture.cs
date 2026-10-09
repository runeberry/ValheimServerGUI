using System.Globalization;
using System.Runtime.CompilerServices;

namespace ValheimServerGUI.Core.Tests;

internal static class TestCulture
{
    // Pin the UI culture to the key-echo TEST culture (qps-ploc: every string is "⟦Key⟧"), so no test can
    // depend on English copy; assertions compare against the Strings accessor instead. Number/date
    // formatting stays en-US. Runs at assembly load, before xUnit creates any worker thread.
    [ModuleInitializer]
    internal static void PinCulture()
    {
        var en = CultureInfo.GetCultureInfo("en-US");
        var test = CultureInfo.GetCultureInfo("qps-ploc");
        CultureInfo.DefaultThreadCurrentCulture = en;
        CultureInfo.DefaultThreadCurrentUICulture = test;
        CultureInfo.CurrentCulture = en;
        CultureInfo.CurrentUICulture = test;
    }
}
