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

    // ---- access roles + permitted-list flag ----

    private static PlayerInfo Player(string id) => new()
    {
        Platform = "Steam",
        PlatformRaw = "Steam",
        PlayerId = id,
    };

    [Fact]
    public void Setting_a_role_dirties_and_raises_RoleStateChanged()
    {
        var form = new ServerFormViewModel();
        var raised = 0;
        form.RoleStateChanged += (_, _) => raised++;

        form.SetRole(Player("1"), PlayerRole.Admin);

        Assert.True(form.IsDirty);
        Assert.Equal(1, raised);
        Assert.Equal(PlayerRole.Admin, form.GetRole("Steam:1"));
    }

    [Fact]
    public void Clearing_a_role_removes_it_and_a_no_op_clear_does_nothing()
    {
        var form = new ServerFormViewModel();
        form.SetRole(Player("1"), PlayerRole.Banned);

        var reloaded = new ServerFormViewModel();
        var raised = 0;
        reloaded.RoleStateChanged += (_, _) => raised++;
        reloaded.SetRole(Player("1"), null); // no role to clear -> no change, no dirty

        Assert.False(reloaded.IsDirty);
        Assert.Equal(0, raised);

        form.SetRole(Player("1"), null); // had a role -> cleared
        Assert.Null(form.GetRole("Steam:1"));
    }

    [Fact]
    public void Toggling_permitted_flag_dirties()
    {
        var form = new ServerFormViewModel();
        Assert.False(form.UsePermittedList);

        form.UsePermittedList = true;
        Assert.True(form.IsDirty);
    }

    [Fact]
    public void Roles_and_flag_round_trip_through_prefs()
    {
        var form = new ServerFormViewModel { UsePermittedList = true };
        form.SetRole(Player("1"), PlayerRole.Admin);
        form.SetRole(Player("2"), PlayerRole.Banned);

        var prefs = form.ToPreferences(new ServerPreferences { ProfileName = "P" });

        Assert.True(prefs.UsePermittedList);
        Assert.Equal(PlayerRole.Admin, prefs.PlayerRoles["Steam:1"].Role);
        Assert.Equal(PlayerRole.Banned, prefs.PlayerRoles["Steam:2"].Role);

        var loaded = new ServerFormViewModel();
        loaded.LoadFieldsFrom(prefs);
        Assert.True(loaded.UsePermittedList);
        Assert.Equal(PlayerRole.Admin, loaded.GetRole("Steam:1"));
        Assert.Equal(PlayerRole.Banned, loaded.GetRole("Steam:2"));
    }

    [Fact]
    public void Load_leaves_the_form_clean()
    {
        var prefs = new ServerPreferences { ProfileName = "P", UsePermittedList = true };
        prefs.PlayerRoles["Steam:1"] = new PlayerRoleEntry(PlayerRole.Admin, "Steam");

        var form = new ServerFormViewModel();
        form.LoadFieldsFrom(prefs);

        Assert.False(form.IsDirty); // a load (roles + flag included) never dirties
    }
}
