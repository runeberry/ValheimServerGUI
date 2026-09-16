import type { Env } from "./env";
import { hasNameLookup, normalizePlatform } from "./platforms";
import { lookupSteamName, lookupXboxName } from "./upstream";

const DEFAULT_API_KEY_HEADER = "x-runeberry-api-key";

// KV TTLs. Positive results are cheap to keep for days; misses are cached briefly so an unresolvable ID
// doesn't hammer the upstream, without hiding a name that later becomes available.
const POSITIVE_TTL_SECONDS = 60 * 60 * 48; // 48h
const NEGATIVE_TTL_SECONDS = 60 * 60; // 1h

/** Injected I/O so the handler is testable without the network. */
export interface Deps {
  fetch: typeof fetch;
}

/**
 * The Worker's request handler. Implements the app-facing contract verbatim (so only the base URL changes
 * in the client): `GET /player-info?platform=&playerId=` -> {id,name,platform} | {message}; `POST
 * /crash-report` accepts the client's crash payload. The client API key is enforced only when CLIENT_API_KEY
 * is configured.
 */
export async function handleRequest(
  request: Request,
  env: Env,
  deps: Deps = { fetch },
): Promise<Response> {
  const keyHeader = env.API_KEY_HEADER || DEFAULT_API_KEY_HEADER;
  if (env.CLIENT_API_KEY && request.headers.get(keyHeader) !== env.CLIENT_API_KEY) {
    return json({ message: "Unauthorized" }, 401);
  }

  const url = new URL(request.url);

  if (request.method === "POST" && url.pathname === "/crash-report") {
    return handleCrashReport(request);
  }

  if (request.method === "GET" && url.pathname === "/player-info") {
    return handlePlayerInfo(url, env, deps);
  }

  return json({ message: "Not found" }, 404);
}

async function handlePlayerInfo(url: URL, env: Env, deps: Deps): Promise<Response> {
  const platformParam = url.searchParams.get("platform");
  const playerId = url.searchParams.get("playerId");

  if (!platformParam || !playerId) {
    return json({ message: "Both 'platform' and 'playerId' query parameters are required." }, 400);
  }

  const platform = normalizePlatform(platformParam);
  if (!platform) {
    return json({ message: `Unsupported platform '${platformParam}'.` }, 400);
  }

  // PlayStation / Nintendo have no reliable public name source: return a clean "no name" (the client falls
  // back to a masked ID). No upstream call, no cache needed -- it is deterministic.
  if (!hasNameLookup(platform)) {
    return json({ message: `No name lookup is available for platform '${platform}'.` }, 404);
  }

  const cacheKey = `${platform}:${playerId}`;
  const cached = await env.NAME_CACHE.get(cacheKey);
  if (cached !== null) {
    return cached === ""
      ? json({ message: "No name found for this ID (cached)." }, 404)
      : json(playerInfo(playerId, cached, platform));
  }

  let name: string | null;
  try {
    name =
      platform === "Steam"
        ? await lookupSteamName(playerId, env, deps.fetch)
        : await lookupXboxName(playerId, env, deps.fetch);
  } catch (err) {
    // Misconfiguration or upstream failure: do NOT cache, so it retries once fixed.
    return json({ message: `Name lookup failed: ${errorMessage(err)}` }, 502);
  }

  if (name) {
    await env.NAME_CACHE.put(cacheKey, name, { expirationTtl: POSITIVE_TTL_SECONDS });
    return json(playerInfo(playerId, name, platform));
  }

  await env.NAME_CACHE.put(cacheKey, "", { expirationTtl: NEGATIVE_TTL_SECONDS });
  return json({ message: "No name found for this ID." }, 404);
}

async function handleCrashReport(request: Request): Promise<Response> {
  // Accept the client's crash payload so repointing the base URL doesn't break crash reporting. We log a
  // truncated copy; durable storage (R2) can be added later without changing the contract.
  try {
    const body = await request.text();
    console.log("crash-report received:", body.slice(0, 4000));
  } catch {
    // Accept even a malformed/empty body -- the client only checks for a success status.
  }
  return json({ message: "Crash report received." });
}

function playerInfo(id: string, name: string, platform: string): { id: string; name: string; platform: string } {
  return { id, name, platform };
}

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function errorMessage(err: unknown): string {
  return err instanceof Error ? err.message : String(err);
}
