using ValheimServerGUI.App.ViewModels;
using ValheimServerGUI.Game;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

public class ServerFormViewModelTests
{
    [Fact]
    public void Round_trips_scalar_fields()
    {
        var original = new ServerPreferences
        {
            ProfileName = "P",
            Name = "S",
            Port = 2470,
            Password = "secret",
            Public = true,
            Crossplay = true,
            SaveInterval = 900,
            BackupCount = 7,
            AutoStart = true,
            AdditionalArgs = "-foo",
            WriteServerLogsToFile = false,
        };

        var form = new ServerFormViewModel();
        form.LoadFieldsFrom(original);
        var restored = form.ToPreferences(new ServerPreferences { ProfileName = "P" });

        Assert.Equal("S", restored.Name);
        Assert.Equal(2470, restored.Port);
        Assert.Equal("secret", restored.Password);
        Assert.True(restored.Public);
        Assert.True(restored.Crossplay);
        Assert.Equal(900, restored.SaveInterval);
        Assert.Equal(7, restored.BackupCount);
        Assert.True(restored.AutoStart);
        Assert.Equal("-foo", restored.AdditionalArgs);
        Assert.False(restored.WriteServerLogsToFile);
    }

    [Fact]
    public void Selected_world_uses_new_name_when_creating()
    {
        var form = new ServerFormViewModel { UseNewWorld = true, NewWorldName = "Fresh" };
        Assert.Equal("Fresh", form.SelectedWorldName);
    }

    [Fact]
    public void Selected_world_strips_cloud_suffix()
    {
        var form = new ServerFormViewModel { UseNewWorld = false, ExistingWorld = "Farlands (cloud)" };
        Assert.Equal("Farlands", form.SelectedWorldName);
        Assert.True(form.IsSelectedWorldCloud);
    }

    [Fact]
    public void Local_existing_world_is_not_cloud()
    {
        var form = new ServerFormViewModel { UseNewWorld = false, ExistingWorld = "Farlands" };
        Assert.Equal("Farlands", form.SelectedWorldName);
        Assert.False(form.IsSelectedWorldCloud);
    }
}
