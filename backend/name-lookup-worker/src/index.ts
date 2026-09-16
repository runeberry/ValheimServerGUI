import type { Env } from "./env";
import { handleRequest } from "./handler";

/** Worker entry point. All logic lives in handleRequest (testable with injected I/O). */
export default {
  fetch(request: Request, env: Env): Promise<Response> {
    return handleRequest(request, env);
  },
};
