using ValheimServerGUI.App.ViewModels;
using Xunit;

namespace ValheimServerGUI.App.Tests.ViewModels;

// Pins the player-list import / conflict user copy character-for-character (including role(s)/player(s), the
// literal {n}/{filepath} placeholders, punctuation, and the blank line) so future edits can't paraphrase it.
public class ImportCopyTests
{
    [Fact]
    public void Import_dialog_copy_is_verbatim()
    {
        Assert.Equal("Import player lists", MainWindowViewModel.ImportDialogTitle);
        Assert.Equal("No files to import.", MainWindowViewModel.ImportNoFilesMessage);
        Assert.Equal("No roles to update.", MainWindowViewModel.ImportNoRolesMessage);
        Assert.Equal("{n} role(s) will be updated from player list files.", MainWindowViewModel.ImportConfirmMessage);
        Assert.Equal("Updated {n} role(s).", MainWindowViewModel.ImportUpdatedMessage);
        Assert.Equal("Failed to import player lists. See application logs for details.", MainWindowViewModel.ImportFailedMessage);
    }

    [Fact]
    public void Permitted_file_error_copy_is_verbatim()
    {
        Assert.Equal("Permitted List File Error", MainWindowViewModel.PermittedFileErrorTitle);
        Assert.Equal(
            "The server is set to launch without a permitted players list, but {filepath} is present on disk, and could not be moved. Please move or delete this file, or change the server configuration to use the permitted list.",
            MainWindowViewModel.PermittedFileErrorMessage);
    }

    [Fact]
    public void Role_conflict_copy_is_verbatim()
    {
        Assert.Equal("Player Role Conflicts", MainWindowViewModel.RoleConflictTitle);
        Assert.Equal(
            "{n} player(s) are present in player list files with conflicting roles. What would you like to do?\n\nSee application logs for more details.",
            MainWindowViewModel.RoleConflictMessage);
    }
}
