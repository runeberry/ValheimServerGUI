# VSG name-lookup Worker

A Cloudflare Worker that resolves platform IDs to display names for ValheimServerGUI, replacing the retired
Runeberry AWS API. It speaks the **same app-facing contract** the client already uses, so repointing the app
is a one-line base-URL change (see the app's `CoreConstants.UrlRuneberryApi`). Adds a KV cache the old API
lacked.

## Contract

- `GET /player-info?platform={Steam|Xbox|PlayStation|Nintendo}&playerId={id}`
  - `200 → { "id", "name", "platform" }`
  - `4xx/5xx → { "message" }`
  - Steam names come from the Steam Web API (`GetPlayerSummaries.personaname`); Xbox from OpenXBL
    (`/player/summary/{xuid}.displayName`). **PlayStation and Nintendo have no reliable public name
    source**, so the Worker returns a clean `404 { message }` and the app falls back to a masked ID.
- `POST /crash-report` — accepts the client's crash payload (logged; durable R2 storage can be added later).
- The client sends its API key in the `x-runeberry-api-key` header (configurable via the `API_KEY_HEADER`
  var). Enforced only when the `CLIENT_API_KEY` secret is set — otherwise the Worker runs open.

## Caching

Results are cached in KV under `platform:playerId` (48h). Misses are negative-cached for 1h so an
unresolvable ID doesn't hammer the upstream. Upstream/misconfig failures are **not** cached (they retry).

## Develop

```bash
npm install
npm run typecheck      # tsc --noEmit
npm test               # vitest (no network; injected fetch + fake KV)
cp .dev.vars.example .dev.vars   # fill in keys for a live local run
npm run dev            # wrangler dev, then: curl "localhost:8787/player-info?platform=Steam&playerId=<id>"
```

## Deploy (needs a Cloudflare account + `wrangler login`)

```bash
wrangler kv namespace create NAME_CACHE   # paste the id into wrangler.toml
wrangler secret put STEAM_API_KEY
wrangler secret put OPENXBL_API_KEY
wrangler secret put CLIENT_API_KEY        # = ClientSecrets.RuneberryClientApiKey in the app
wrangler deploy
```

After deploy, set the app's `CoreConstants.UrlRuneberryApi` to the Worker's URL (`https://<name>.<subdomain>.workers.dev`
or a custom route) and put the same `CLIENT_API_KEY` into `SolutionResources/ClientSecrets.Values.cs`.
