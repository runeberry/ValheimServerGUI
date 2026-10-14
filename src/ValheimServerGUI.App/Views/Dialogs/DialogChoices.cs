namespace ValheimServerGUI.App.Views.Dialogs;

/// <summary>The unsaved-changes choice for Manage Players / WorldPreferences / Directories (§13.3).</summary>
public enum UnsavedChangesChoice { Save, Discard, Cancel }

/// <summary>The user's answer to the start-time player-role conflict prompt.</summary>
public enum RoleConflictChoice { UseServerProfile, UseRolesFromFile, Cancel }
