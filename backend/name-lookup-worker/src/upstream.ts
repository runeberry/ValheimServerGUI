import type { Env } from "./env";

/**
 * Thin I/O wrappers over the two upstream name APIs. Each takes an explicit `fetchImpl` so tests can inject
 * a fake without touching the global. They return a trimmed non-empty name, or null when the upstream has no
 * name for the ID; they throw only on misconfiguration or an upstream HTTP failure.
 */

interface SteamSummaries {
  response?: { players?: Array<{ personaname?: string }> };
}

export async function lookupSteamName(
  steamId: string,
  env: Env,
  fetchImpl: typeof fetch,
): Promise<string | null> {
  if (!env.STEAM_API_KEY) throw new Error("STEAM_API_KEY is not configured");

  const url =
    "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/" +
    `?key=${encodeURIComponent(env.STEAM_API_KEY)}&steamids=${encodeURIComponent(steamId)}`;

  const res = await fetchImpl(url);
  if (!res.ok) throw new Error(`Steam API returned ${res.status}`);

  const data = (await res.json()) as SteamSummaries;
  return firstNonEmpty(data.response?.players?.[0]?.personaname);
}

interface OpenXblSummary {
  people?: Array<{ displayName?: string; gamertag?: string }>;
}

export async function lookupXboxName(
  xuid: string,
  env: Env,
  fetchImpl: typeof fetch,
): Promise<string | null> {
  if (!env.OPENXBL_API_KEY) throw new Error("OPENXBL_API_KEY is not configured");

  const res = await fetchImpl(`https://xbl.io/api/v2/player/summary/${encodeURIComponent(xuid)}`, {
    headers: { "X-Authorization": env.OPENXBL_API_KEY, Accept: "application/json" },
  });
  if (!res.ok) throw new Error(`OpenXBL returned ${res.status}`);

  const data = (await res.json()) as OpenXblSummary;
  const person = data.people?.[0];
  return firstNonEmpty(person?.displayName, person?.gamertag);
}

function firstNonEmpty(...values: Array<string | undefined>): string | null {
  for (const value of values) {
    if (value && value.trim().length > 0) return value;
  }
  return null;
}
