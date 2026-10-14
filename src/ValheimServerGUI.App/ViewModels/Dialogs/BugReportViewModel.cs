using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimServerGUI.Tools;

namespace ValheimServerGUI.App.ViewModels.Dialogs;

/// <summary>Bug Report dialog (§8/§10.1): a description submitted to the Runeberry backend as source "BugReport".</summary>
public partial class BugReportViewModel : ObservableObject
{
    private readonly IRuneberryApiClient _client;

    public BugReportViewModel(IRuneberryApiClient client) => _client = client;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _description = string.Empty;

    /// <summary>Optional contact details submitted with the report so we can follow up.</summary>
    [ObservableProperty]
    private string _contactInfo = string.Empty;

    public bool CanSubmit => !string.IsNullOrWhiteSpace(Description);

    public Task SubmitAsync()
    {
        var report = AssemblyHelper.BuildCrashReport();
        report.Source = "BugReport";
        report.AdditionalInfo = new Dictionary<string, string>
        {
            ["Description"] = Description,
            ["ContactInfo"] = ContactInfo,
        };
        return _client.SendCrashReportAsync(report);
    }
}
