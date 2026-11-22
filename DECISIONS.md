# Decisions

Why Vehicle Explorer is built the way it is, what I assumed, what I chose not to build,
and how I used AI. How to run it is in the [README](README.md).

## Architecture

Three .NET projects with one boundary between them:

| Project | Holds | Depends on |
|---|---|---|
| `VehicleExplorer.Api` | Controllers, error handling, rate limiting, startup warm-up | Core, Infrastructure |
| `VehicleExplorer.Core` | Make search ranking, the model year overview logic, the `INhtsaClient` interface | nothing |
| `VehicleExplorer.Infrastructure` | The NHTSA HTTP client, retries, caching | Core |

`INhtsaClient` is the only seam. Core has no HTTP code, so its logic is tested with a fake client,
and caching is added as a decorator (`CachedNhtsaClient`) without changing the real client.
I kept it to three projects: more layers (CQRS, MediatR, repositories) would add files without solving a problem this app has.

## Key decisions

**Make search runs in memory.** NHTSA has no search endpoint, only "all makes" (about 12,000).
The API loads that list once at startup (and every 24 hours) and searches it in memory, so each keystroke costs no NHTSA call.
Results are ranked: exact match, then starts with, then a word starts with, then contains, with well-known makes first in each group.
"mercedes benz" and "Mercedes-Benz" match the same make.

**The overview fans out.** NHTSA's model list for a make and year says nothing about vehicle types.
To show which models are cars, trucks or motorcycles, the API asks for the models once per vehicle type
(in parallel) plus the full list, and merges them. Models that NHTSA puts under no type still appear.
Trade-off: the first load of a make and year is several NHTSA calls (usually 5 to 8); after that it is cached.
The alternative, loading one vehicle type at a time when the user clicks it, makes fewer calls but adds a wait on every click.

**Caching.** Each NHTSA call is cached on its own with HybridCache: vehicle types 24 hours, model lists 12 hours,
empty answers only 10 minutes (a new model year may fill them soon). Errors are never cached.
HybridCache also makes simultaneous requests for the same data share one NHTSA call.

**NHTSA failures.** Every NHTSA call has a 5-second timeout, up to 2 retries, and a 20-second total limit.
If one vehicle type still fails, the overview returns what it has, marks itself incomplete, and the UI says which types are missing.
If NHTSA is down completely, the API returns 503 with a clear message instead of a generic 500.
If NHTSA is down at startup, the app still starts and loads the make list on the first search.

**Errors and limits.** All errors use the standard ProblemDetails format (400, 404, 429, 503).
An unknown make id returns 404: NHTSA itself answers with an empty list, which would look like "no models".
Each IP may make 100 API requests per minute, which is plenty for a person and stops simple abuse of the NHTSA API through mine.

**One container.** The API serves the built Angular app from `wwwroot`. One image, one port, and no CORS setup.

**Deployment.** CI builds the Docker image and pushes it to GitHub Container Registry. One EC2 instance pulls it and runs it
behind Caddy, which gets the HTTPS certificate by itself (using an `sslip.io` name, so no domain had to be bought).
No AWS keys are stored in the repository or in CI. Details: [docs/deploy.md](docs/deploy.md).

## Interface and motion

**Look.** A dark, calm interface like a car's night cockpit, inspired by premium product sites (Linear) and car brands (Rivian):
almost no colour, one amber accent for focus and matched letters, and numbers in a monospace font like instrument dials.
The light theme follows the system setting. All colours, sizes, radii and timings are CSS variables in `styles.scss`.

**One signature moment.** When a make is chosen from the start screen, the search field glides up into the top bar
(a native View Transition, 450 ms). In zoneless Angular the page must render inside the transition, so the code
calls `ApplicationRef.tick()` after navigating. Without View Transition support, or with reduced motion on, it simply switches.

**Motion rules.** Everything else is short (100–220 ms, ease-out) and only fades or moves; nothing animates layout,
nothing keeps moving on its own, and frequent actions (typing, arrow keys, filter chips) barely move.
The loading skeleton appears only if loading takes longer than 300 ms, so fast answers never flash.

**The URL holds the state** (`?makeId=474&modelYear=2015&vehicleType=Motorcycle`): a view can be shared and refreshed,
and back/forward works. The page only reads the URL and changes it through the router.

**Accessibility.** The make search is an ARIA combobox (↑ ↓ Enter Esc, and "/" to focus it). Touch targets are at least
44 px, focus rings are always visible, and the text colours pass WCAG AA contrast in both themes.

**Fonts are self-hosted** (`@fontsource` packages), so visitors' browsers make no requests to Google.

## Assumptions

- **Model years run from 1981 to next year.** Checked live: NHTSA has no models before 1981, and it answers any
  future year with its newest models, so later years would show misleading data.
- **Vehicle types are used by their exact NHTSA name** (for example "Multipurpose Passenger Vehicle (MPV)"),
  because that is what the model endpoint filters on.
- **NHTSA data changes rarely**, which is why caching for hours is safe.

## Not built, on purpose

- **Redis or a database.** One instance, read-only public data: the in-memory cache is enough.
  HybridCache can use Redis later by registering it at startup; the caching code stays the same.
- **Authentication.** The data is public and the app has no user data.
- **ESLint.** Prettier checks the formatting; for a few Angular files a linter would add setup for little gain.
- **Automated deploy.** Deploying is one command (`deploy/deploy.sh`). Automating it would mean giving CI access to the server.
- **A UI kit or animation library** (Angular Material, GSAP). Native CSS and one View Transition do every job here,
  and there is less code to explain. The combobox is about 100 lines, so the Angular CDK would not save code.
- **Vehicle photos.** vPIC has no images. NHTSA's Safety Ratings API has photos only for crash-tested cars, and they are
  crash-test photos; showing a damaged car on most cards and no picture on the rest would look worse than none.
- **Branches and pull requests.** I worked alone, so I committed straight to `main`, one commit per task, each with green checks.

## Next steps

- Deploy automatically from CI with AWS SSM or OIDC (no stored keys).
- Add Redis if the app runs on more than one instance.
- Add ESLint and an end-to-end test of the main flow.
- A model details page with NHTSA's safety ratings (and their crash-test photos where they exist).

## How I used AI

I used Claude Code (Anthropic) throughout: to plan the work, write code and tests, review each story,
guide the Docker, CI and AWS setup, and design the interface (two coded design directions, measured against
live reference sites, then an independent design review). I set the requirements and rules, checked the results against the live NHTSA API,
and every commit had to build with zero warnings, pass the format check, and pass the tests.
I kept a log of the cases where the AI was wrong. Three of them:

1. **A timeout would have crashed the app at startup.** The warm-up service was written to stop on
   `OperationCanceledException`, meant for app shutdown. But an HTTP timeout is also an `OperationCanceledException`,
   and since .NET 8 an unhandled exception in a background service stops the whole app. So a slow NHTSA at startup
   would have taken the site down. Fix: the loop now checks whether the app is really shutting down (the stopping token), not the exception type.
2. **The second retry could never run.** The retry settings were shortened (2 retries, 20 seconds in total), but the default
   10-second timeout per attempt was kept, so two slow attempts used up the whole 20 seconds. Caught in a review; fixed with a 5-second attempt timeout.
3. **"My IP" picked the wrong address.** The AI told me to limit SSH on the server to "My IP" in the AWS console.
   The console chose my IPv6 address, but the server only has IPv4, so SSH was blocked. Fixed by entering my IPv4 address directly;
   the deploy guide now says so.
