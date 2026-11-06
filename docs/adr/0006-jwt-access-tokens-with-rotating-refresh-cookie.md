# JWT access tokens with a rotating refresh cookie

The API issues short-lived (10 min) ES256 JWT access tokens, which the browser keeps **in memory only**, and an opaque refresh token in an HttpOnly, Secure, SameSite=Strict cookie. The refresh token rotates on every use, and reusing an old one revokes the whole token family (one family = one Session). We chose bearer tokens over cookie-only auth so that future services and mobile clients can validate tokens offline via a JWKS endpoint. The in-memory and rotation rules recover most of the browser safety that cookie auth gives.

## Consequences

- Concurrent refreshes from several browser tabs would look like token theft. To prevent this, the client serializes refreshes with the Web Locks API and shares new tokens over a BroadcastChannel. The server also allows a 10-second grace window, in which the just-rotated token returns the same successor.
- Authenticated routes render client-side only: SSR has no access token. Public routes stay server-rendered.
- The API and web app must be served from the same origin (`/api` behind a reverse proxy, the Vite proxy in dev), so the refresh cookie is first-party and CORS is never needed.
- Refresh tokens last 14 days sliding, with a 90-day absolute cap per family.
