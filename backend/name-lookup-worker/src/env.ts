import type { KVNamespace } from "@cloudflare/workers-types";

/**
 * Worker bindings + secrets. Secrets are set with `wrangler secret put` (never committed); the KV
 * namespace and the API-key header name are declared in wrangler.toml.
 */
export interface Env {
  /** Cache of `platform:playerId` -> display name (empty string = negative-cached miss). */
  NAME_CACHE: KVNamespace;

  /** Steam Web API key (ISteamUser/GetPlayerSummaries). */
  STEAM_API_KEY?: string;

  /** OpenXBL API key (xbl.io player summary). */
  OPENXBL_API_KEY?: string;

  /** The client API key the app must present; when set, requests without a matching header are rejected. */
  CLIENT_API_KEY?: string;

  /** Header name the client sends its API key in. Defaults to "x-runeberry-api-key". */
  API_KEY_HEADER?: string;
}
