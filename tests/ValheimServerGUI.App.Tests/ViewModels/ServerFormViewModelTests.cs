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

    [Fact]
    public void Editing_a_field_sets_dirty()
    {
        var form = new ServerFormViewModel();
        Assert.False(form.IsDirty);

        form.Name = "Changed";
        Assert.True(form.IsDirty);
    }

    [Fact]
    public void Load_clears_dirty()
    {
        var form = new ServerFormViewModel { Name = "typed" };
        Assert.True(form.IsDirty);

        form.LoadFieldsFrom(new ServerPreferences { ProfileName = "P", Name = "Loaded" });
        Assert.False(form.IsDirty);
    }

    [Fact]
    public void Show_password_toggle_is_not_a_dirtying_edit()
    {
        var form = new ServerFormViewModel();
        form.ToggleShowPasswordCommand.Execute(null);

        Assert.True(form.ShowPassword);
        Assert.False(form.IsDirty); // view-only, never counts as an edit
    }

    [Fact]
    public void RunClean_suppresses_dirty_but_direct_edits_still_trip_it()
    {
        var form = new ServerFormViewModel();
        form.RunClean(() => form.ExistingWorld = "W"); // app-driven mutation
        Assert.False(form.IsDirty);

        form.ExistingWorld = "W2"; // direct user edit
        Assert.True(form.IsDirty);
    }
}
