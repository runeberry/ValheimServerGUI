import { describe, expect, it, vi } from "vitest";
import type { Env } from "../src/env";
import { handleRequest } from "../src/handler";

/** In-memory stand-in for the KV binding. */
class FakeKV {
  readonly store = new Map<string, string>();
  async get(key: string): Promise<string | null> {
    return this.store.has(key) ? this.store.get(key)! : null;
  }
  async put(key: string, value: string): Promise<void> {
    this.store.set(key, value);
  }
}

function makeEnv(overrides: Partial<Env> = {}): { env: Env; kv: FakeKV } {
  const kv = new FakeKV();
  const env = {
    NAME_CACHE: kv as unknown as Env["NAME_CACHE"],
    STEAM_API_KEY: "steam-key",
    OPENXBL_API_KEY: "xbl-key",
    ...overrides,
  } as Env;
  return { env, kv };
}

function jsonResponse(body: unknown, ok = true): Response {
  return new Response(JSON.stringify(body), { status: ok ? 200 : 500 });
}

function get(path: string, headers?: Record<string, string>): Request {
  return new Request(`https://worker.example${path}`, { method: "GET", headers });
}

describe("player-info", () => {
  it("resolves a Steam name from upstream and caches it", async () => {
    const { env, kv } = makeEnv();
    const fetchImpl = vi.fn(async () =>
      jsonResponse({ response: { players: [{ personaname: "Odin" }] } }),
    );

    const res = await handleRequest(get("/player-info?platform=Steam&playerId=123"), env, {
      fetch: fetchImpl as unknown as typeof fetch,
    });

    expect(res.status).toBe(200);
    expect(await res.json()).toEqual({ id: "123", name: "Odin", platform: "Steam" });
    expect(kv.store.get("Steam:123")).toBe("Odin");
    expect(fetchImpl).toHaveBeenCalledOnce();
  });

  it("serves a cache hit without calling upstream", async () => {
    const { env, kv } = makeEnv();
    kv.store.set("Steam:123", "Cached");
    const fetchImpl = vi.fn();

    const res = await handleRequest(get("/player-info?platform=Steam&playerId=123"), env, {
      fetch: fetchImpl as unknown as typeof fetch,
    });

    expect(await res.json()).toEqual({ id: "123", name: "Cached", platform: "Steam" });
    expect(fetchImpl).not.toHaveBeenCalled();
  });

  it("resolves an Xbox displayName", async () => {
    const { env } = makeEnv();
    const fetchImpl = vi.fn(async () =>
      jsonResponse({ people: [{ displayName: "GamerTag", gamertag: "fallback" }] }),
    );

    const res = await handleRequest(get("/player-info?platform=Xbox&playerId=XUID"), env, {
      fetch: fetchImpl as unknown as typeof fetch,
    });

    expect(await res.json()).toEqual({ id: "XUID", name: "GamerTag", platform: "Xbox" });
  });

  it.each(["PlayStation", "Nintendo", "switch"])(
    "returns a clean no-name for %s without any upstream call",
    async (platform) => {
      const { env } = makeEnv();
      const fetchImpl = vi.fn();

      const res = await handleRequest(get(`/player-info?platform=${platform}&playerId=1`), env, {
        fetch: fetchImpl as unknown as typeof fetch,
      });

      expect(res.status).toBe(404);
      expect(fetchImpl).not.toHaveBeenCalled();
    },
  );

  it("negative-caches an unresolvable ID and reuses it", async () => {
    const { env, kv } = makeEnv();
    const fetchImpl = vi.fn(async () => jsonResponse({ response: { players: [] } }));

    const first = await handleRequest(get("/player-info?platform=Steam&playerId=999"), env, {
      fetch: fetchImpl as unknown as typeof fetch,
    });
    expect(first.status).toBe(404);
    expect(kv.store.get("Steam:999")).toBe("");

    const second = await handleRequest(get("/player-info?platform=Steam&playerId=999"), env, {
      fetch: fetchImpl as unknown as typeof fetch,
    });
    expect(second.status).toBe(404);
    expect(fetchImpl).toHaveBeenCalledOnce(); // second served from the negative cache
  });

  it("400s on missing params and unknown platform", async () => {
    const { env } = makeEnv();
    const deps = { fetch: vi.fn() as unknown as typeof fetch };

    expect((await handleRequest(get("/player-info?platform=Steam"), env, deps)).status).toBe(400);
    expect((await handleRequest(get("/player-info?playerId=1"), env, deps)).status).toBe(400);
    expect(
      (await handleRequest(get("/player-info?platform=Epic&playerId=1"), env, deps)).status,
    ).toBe(400);
  });

  it("502s (and does not cache) on an upstream failure", async () => {
    const { env, kv } = makeEnv();
    const fetchImpl = vi.fn(async () => jsonResponse({ message: "boom" }, false));

    const res = await handleRequest(get("/player-info?platform=Steam&playerId=5"), env, {
      fetch: fetchImpl as unknown as typeof fetch,
    });

    expect(res.status).toBe(502);
    expect(kv.store.has("Steam:5")).toBe(false);
  });
});

describe("crash-report", () => {
  it("accepts a POSTed crash report", async () => {
    const { env } = makeEnv();
    const req = new Request("https://worker.example/crash-report", {
      method: "POST",
      body: JSON.stringify({ message: "it crashed" }),
    });

    const res = await handleRequest(req, env, { fetch: vi.fn() as unknown as typeof fetch });

    expect(res.status).toBe(200);
  });
});

describe("api key", () => {
  it("rejects a wrong/missing key when CLIENT_API_KEY is configured", async () => {
    const { env } = makeEnv({ CLIENT_API_KEY: "secret", API_KEY_HEADER: "x-runeberry-api-key" });
    const deps = { fetch: vi.fn() as unknown as typeof fetch };

    expect((await handleRequest(get("/player-info?platform=Steam&playerId=1"), env, deps)).status).toBe(401);
    expect(
      (
        await handleRequest(
          get("/player-info?platform=Steam&playerId=1", { "x-runeberry-api-key": "wrong" }),
          env,
          deps,
        )
      ).status,
    ).toBe(401);
  });

  it("allows a request with the correct key", async () => {
    const { env } = makeEnv({ CLIENT_API_KEY: "secret" });
    const fetchImpl = vi.fn(async () =>
      jsonResponse({ response: { players: [{ personaname: "OK" }] } }),
    );

    const res = await handleRequest(
      get("/player-info?platform=Steam&playerId=1", { "x-runeberry-api-key": "secret" }),
      env,
      { fetch: fetchImpl as unknown as typeof fetch },
    );

    expect(res.status).toBe(200);
  });

  it("is open when no CLIENT_API_KEY is configured", async () => {
    const { env } = makeEnv();
    const fetchImpl = vi.fn(async () =>
      jsonResponse({ response: { players: [{ personaname: "Open" }] } }),
    );

    const res = await handleRequest(get("/player-info?platform=Steam&playerId=1"), env, {
      fetch: fetchImpl as unknown as typeof fetch,
    });

    expect(res.status).toBe(200);
  });
});
