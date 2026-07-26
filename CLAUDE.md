# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

Build and run (from repo root or `PropertySalesMVC/`):
```
dotnet build PropertySalesMVC/PropertySalesMVC.csproj
dotnet run --project PropertySalesMVC/PropertySalesMVC.csproj
```
There is no test project in this solution.

Database (`db_acc7ad_ronakestatedb` on `SQL5063.site4now.net`) has no ORM migrations — schema changes are hand-written, idempotent SQL in `PropertySalesMVC/Database/Schema.sql` (every `CREATE TABLE`/`ALTER TABLE` is guarded with `IF NOT EXISTS`, safe to re-run against a live DB with data in it). There is no code that runs this automatically on startup; apply it manually against the target DB whenever it changes. `PropertySalesMVC/Database/Seed.sql` has starter data (locations, admin/company profile) — note it does **not** seed `AdminLoginDetails`, since that table stores a PBKDF2 hash, not plaintext; create that row via the app or a one-off script.

Local secrets: `ConnectionStrings:DefaultConnection` is empty in `appsettings.json`/`appsettings.Development.json` on purpose — set the real value via `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."` (the `.csproj` already has a `UserSecretsId`). In production this must come from an environment variable (`ConnectionStrings__DefaultConnection`), never committed.

## Architecture

Layered: **Controllers → Services → Repositories**, all interface-based and registered as `Scoped` in `Program.cs`. Controllers stay thin (bind → call one service method → map to ViewBag/View); business logic and orchestration lives in `Services/`; all SQL lives in `Repositories/` via raw ADO.NET (`Microsoft.Data.SqlClient`) — there is no ORM despite the `Nullable`/`ImplicitUsings` project setup looking EF-ready. `Repositories/` is the *only* place `new SqlConnection(...)` should appear, always built from `IConfiguration`, never a hardcoded string.

**Namespace constraint on Models**: every class under `Models/` (both `Models/ViewModels/*` and the flat files directly in `Models/`) lives in the single flat namespace `PropertySalesMVC.Models`, regardless of folder. This is deliberate, not an oversight — `Views/_ViewImports.cshtml` only imports `PropertySalesMVC` and `PropertySalesMVC.Models`, and is the one file in this repo you should not need to touch; nesting namespaces to match folders would silently break Razor views. The one exception is `AddPropertyViewModel`, which lives in the *global* namespace (no `namespace` block at all) — that's what `AdminController`/`AddProperty.cshtml` actually bind against; there is no other copy.

**Admin auth is not ASP.NET Identity/`[Authorize]`.** It's a custom `Filters/AdminAuthorizeAttribute` (a plain `ActionFilterAttribute` checking a session flag from `Helpers/SessionKeys.cs`). Critically, **`[AllowAnonymous]` has no effect on it** — that attribute only short-circuits the built-in ASP.NET Core authorization system, not a custom `ActionFilterAttribute`. Public actions must simply omit `[AdminAuthorize]`; do not add `[AllowAnonymous]` expecting it to override a class-level `[AdminAuthorize]`.

**`PropertyMode` / `LookingFor` mapping**: `Properties.LookingFor` (`int`) is `Rent=1, Buy=2, Sell=3` (see `Models/PropertyMode.cs`). This exact mapping is relied on by stored data — don't "fix" it to a different enum ordering without a data migration.

**View resolution gotcha**: `PropertyController.Rent()/Buy()/Sell()` call `Listing()` as a plain C# method (not a redirect) so all three share one query implementation. Because of this, `return View(properties)` inside `Listing()` resolves the view by whichever action was *originally routed to* (`Rent`/`Buy`/`Sell`), not by the literal method name `Listing` — so `Views/Property/Rent.cshtml`, `Buy.cshtml`, and `Sell.cshtml` must all exist and each independently match what `Listing()` produces. If you add a new mode this way, you need a matching view file even though no code explicitly names it.

**Images live on the web server's local disk** (`wwwroot/uploads/properties/{propertyId}/...`, written by `Services/FileStorageService.cs`), never in the DB — only the path string is stored in `PropertyImages.ImagePath`. This is intentional given the DB's small storage budget. There used to be a legacy `ImageBase64` DB-storage path; it has been fully removed (column dropped, all fallback code deleted) — don't reintroduce it.

**Error logging**: `Logging/DatabaseLoggerProvider`/`DatabaseLogger` is a custom `ILoggerProvider` registered as a singleton in `Program.cs`, capturing `Error`/`Critical`-level logs only (from anywhere in the app — explicit `_logger.LogError(...)` calls and unhandled exceptions alike) into the `ErrorLogs` table, viewable at `/Admin/ErrorLogs`. It **self-purges rows older than 30 days on every write** — this is deliberate, to keep the log from growing unbounded against the DB's storage budget; don't remove the purge without adding a replacement retention mechanism.

**Contact/WhatsApp source of truth**: `AdminMaster.WhatsApp` (via `IAdminService.GetActiveAdminContactAsync()`) is the one number used everywhere a customer can message the admin (Contact page, property inquiries, Sell submissions). Company/office info (name, addresses, socials) comes from `AdminDetails` via `IAdminService.GetAdminDetailsForLayoutAsync()`. Both are editable at `/Admin/Profile`, which upserts (`UPDATE`; if 0 rows affected, `INSERT`) since exactly one active row of each is expected — do not add a second active row of either table.

**Lead capture**: every customer-facing contact action (Contact form, a property's Call/WhatsApp buttons, the "submit my property to sell" form) writes a row to `PropertyEnquiries` via `IEnquiryService`, viewable at `/Admin/Enquiries`. The property WhatsApp/Call buttons log via a fire-and-forget `POST /Property/LogEnquiry` beacon so the log write never delays the outbound WhatsApp/tel: navigation.
