/** VSG's canonical platform names (must match PlayerPlatforms in the app). */
export type Platform = "Steam" | "Xbox" | "PlayStation" | "Nintendo";

/**
 * Normalizes an incoming platform query value to a canonical name, or null if unsupported. Mirrors the
 * app's PlayerPlatforms.TryGetValidPlatform (the game's "Switch" token maps to Nintendo).
 */
export function normalizePlatform(input: string): Platform | null {
  switch (input.trim().toLowerCase()) {
    case "steam":
      return "Steam";
    case "xbox":
      return "Xbox";
    case "playstation":
      return "PlayStation";
    case "nintendo":
    case "switch":
      return "Nintendo";
    default:
      return null;
  }
}

/** Whether a public display-name lookup exists for this platform (PS/Switch have none). */
export function hasNameLookup(platform: Platform): platform is "Steam" | "Xbox" {
  return platform === "Steam" || platform === "Xbox";
}
