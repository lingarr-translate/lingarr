**You are an AI agent working in the Lingarr repository.**  
Read this file top‑to‑bottom once per session before you start changing code.  
If anything written here disagrees with the code, **the code is right and this file is wrong**.

---

## 1. Mental Model of This Repo

Lingarr is a server‑driven subtitle translation system:

- **Backend** – `Lingarr.Server` (ASP.NET Core 9 / Web API)
  - Controllers in `Lingarr.Server/Controllers`
  - Domain / business services in `Lingarr.Server/Services/**`
  - Background work and automation in `Lingarr.Server/Jobs`
  - SignalR hubs in `Lingarr.Server/Hubs`
  - Logging and in‑memory log streaming in `Lingarr.Server/Providers` + `LogsController`
- **Domain/Core** – `Lingarr.Core`
  - Entities and enums under `Lingarr.Core/Entities` + `Lingarr.Core/Enum`
  - EF Core DbContext at `Lingarr.Core/Data/LingarrDbContext.cs`
  - DB provider configuration in `Lingarr.Core/Configuration/DatabaseConfiguration.cs`
  - Setting keys in `Lingarr.Core/Configuration/SettingKeys.cs`
  - Logging infrastructure in `Lingarr.Core/Logging/**`
- **Database migrations**
  - SQLite migrations: `Lingarr.Migrations.SQLite/Migrations`
  - MySQL migrations: `Lingarr.Migrations.MySQL/Migrations`
  - Both target the same model (`LingarrDbContext`) and must stay aligned.
- **Frontend** – `Lingarr.Client` (Vue 3 + Vite + Tailwind + TypeScript)
  - Entry at `Lingarr.Client/src/main.ts`, app shell in `src/App.vue`
  - Route‑level screens under `src/pages`
    - `TranslationPage.vue`, `MoviePage.vue`, `ShowPage.vue`, `SettingPage.vue`, `DashboardPage.vue`
  - Pinia stores under `src/store`
  - HTTP service wrappers under `src/services`
  - Shared types and interfaces in `src/ts/**`
  - SignalR client integration in `src/ts/composables/signalR.ts`
- **Dev & tooling**
  - Dev stack via `docker-compose.dev.yml`
    - Server, Client, LibreTranslate, MariaDB, phpMyAdmin, Sonarr, Radarr
  - Migrations helper: `create-migrations.ps1`
  - Docker image builder: `build-and-push.ps1`
  - Contribution rules: `CONTRIBUTING.md`
  - Configuration docs: `Settings.MD` and `Readme.MD`

If you are working in a subfolder (e.g. `Lingarr.Client` only), still keep this whole picture in mind; many changes are cross‑cutting.

---

## 2. How to Start a Session (for agents)

Before editing anything:

1. **Check high‑level docs**
   - Skim `Readme.MD` to recall what Lingarr does and how it’s run.
   - Skim `Settings.MD` for the list of environment variables and what they are meant to control.
   - Skim `CONTRIBUTING.md` to remember how branches and commit messages should look.

2. **Locate the relevant code**
   - **API or UI change?**
     - Find the controller in `Lingarr.Server/Controllers/...Controller.cs`
     - Find the matching frontend service in `Lingarr.Client/src/services`
     - Find the store/page using that service in `Lingarr.Client/src/store` and `src/pages`
   - **Domain / DB change?**
     - Find entities and enums in `Lingarr.Core/Entities` and `Lingarr.Core/Enum`
     - Check how they’re used in services/jobs/controllers
     - Inspect existing migrations for both SQLite and MySQL
   - **Background process / automation?**
     - Jobs under `Lingarr.Server/Jobs`
     - Scheduling in `Lingarr.Server/Services/ScheduleService.cs`
     - Settings‑driven triggers in `Lingarr.Server/Listener/SettingChangedListener.cs`

3. **Plan in small steps**
   - Use the `update_plan` tool when you expect more than one edit:
     - 3–5 bullets; each should be short (“adjust TranslationJob”, “update Vue store”, “add test”).
     - Keep exactly one item `in_progress`.
   - Prefer a narrow plan over a big one; it is okay to add steps later.

4. **Announce tool use**
   - Before each shell or patch tool call, explain in 1–2 sentences what you are about to do:
     - Examples: “Inspecting TranslationJob and MediaSubtitleProcessor for the new setting.” / “Updating TranslationPage store interactions for the new API response.”

---

## 3. Non‑Negotiable Rules for Changes

These are the guardrails you must respect, even if the user does not mention them explicitly.

### 3.1 Keep behavior stable unless the task says otherwise

- Do **not** remove or disable:
  - Existing translation providers (`LibreService`, `OpenAiService`, `AnthropicService`, `LocalAiService`, `GoogleGeminiService`, `DeepSeekService`, etc.)
  - Integrations with Radarr/Sonarr (services in `Lingarr.Server/Services/Integration`, settings, jobs)
  - Hangfire jobs and queues (names and queues in `[Queue("...")]` attributes)
- If a method or class looks unused, search the whole solution (`rg` + `git grep`) before you assume it is dead. Jobs and SignalR events especially may be triggered indirectly.

### 3.2 Keep database models and migrations in lockstep

- Entities and DbSet definitions in:
  - `Lingarr.Core/Entities/*.cs`
  - `Lingarr.Core/Data/LingarrDbContext.cs`
- Both migration projects must be consistent:
  - `Lingarr.Migrations.SQLite/Migrations`
  - `Lingarr.Migrations.MySQL/Migrations`
- When you add or modify entities, relationships, or configuration:
  - Use `./create-migrations.ps1 -MigrationName "MeaningfulName"` from the repo root.
  - This script generates migrations for **both** SQLite and MySQL based on the current model.
  - Do not hand‑edit only one provider’s migrations and leave the other broken.

### 3.3 Respect settings and environment variables

Settings are central to how the app behaves:

- The authoritative key list is `Lingarr.Core/Configuration/SettingKeys.cs`.
- Environment variables are mapped into DB settings in `Lingarr.Server/Services/StartupService.cs`.
- `Settings.MD` must describe anything that users are expected to set via env vars.

When you:
- **Add a new setting**
  - Add a constant to `SettingKeys` in the appropriate nested class.
  - Decide whether there is an env var that should populate it on startup; if yes, add mapping in `StartupService.ApplySettingsFromEnvironment`.
  - If the setting affects automation or jobs, consider whether `SettingChangedListener` must react to it.
  - Update `Settings.MD` and any related UI in `Lingarr.Client/src/pages/SettingPage.vue` and its stores.
- **Change meaning or default of an existing setting**
  - Audit where it is read (services, jobs, controllers, client).
  - Update docs and any validations that rely on assumptions about that value.

### 3.4 Treat user media and secrets as sensitive

- Subtitle and media paths (`/movies`, `/tv`, `/anime`, etc.):
  - Do not add code that deletes or moves user media files.
  - Writes should be limited to subtitle output files created by `SubtitleService`.
  - Any new file operations must be clearly justified and robust against missing directories.
- Credentials and API keys:
  - Never write secrets (API keys, DB passwords, tokens) to logs.
  - Log only the fact that a particular provider is “configured” or “missing configuration”.

### 3.5 Keep work small and focused

- Avoid “while I’m here…” refactors.
- One axis of change at a time:
  - Example: if you are fixing a bug in `TranslationJob`, do not also redesign the Statistics API in the same branch.
- If the user asks for several loosely related things, propose separate branches/PRs.

---

## 4. Subsystem Guides

### 4.1 Translation flow

Rough path of a file translation:

1. Media appears in DB (`Movie`, `Show`, `Season`, `Episode`) via sync jobs (`SyncMovieJob`, `SyncShowJob`).
2. Subtitle discovery and translation request creation:
   - `Lingarr.Server/Services/MediaSubtitleProcessor.cs`
   - Uses language settings (`SOURCE_LANGUAGES`, `TARGET_LANGUAGES`, `ignore_captions`, etc.) stored as settings.
   - Computes a hash over media + language configuration to avoid re‑processing the same material.
3. Translation job execution:
   - `Lingarr.Server/Jobs/TranslationJob.cs`
   - Reads multiple settings (translation service type, batching, context prompts, formatting flags, validation options).
   - Uses a concrete translation service instance created by `Lingarr.Server/Services/Translation/TranslationFactory.cs`.
   - Uses `SubtitleService` and `SubtitleTranslationService` to parse, translate, and write subtitles.
   - Updates `TranslationRequest` status, statistics, and emits progress via `IProgressService` (SignalR).

When changing anything in this flow:

- Make sure the settings you rely on exist in `SettingKeys` and are properly populated.
- Be careful with cancellation and retries in `TranslationJob`:
  - Respect `CancellationToken` arguments.
  - Make sure you clear media hashes and update active request counts on failure or cancel.
- Confirm that frontend expectations (e.g. progress values, status transitions) still hold:
  - Stores in `Lingarr.Client/src/store/translationRequest.ts`
  - Types in `Lingarr.Client/src/ts` and UI in `TranslationPage.vue`

### 4.2 Automation and schedules

Automation is Hangfire‑driven:

- Recurring job setup and monitoring:
  - `Lingarr.Server/Services/ScheduleService.cs`
  - Uses `RecurringJob.AddOrUpdate` to schedule:
    - `SyncMovieJob`, `SyncShowJob`, `AutomatedTranslationJob`, `CleanupJob`, `StatisticsJob`
  - Tracks job state and pushes updates to clients via `JobProgressHub`.
- Initial and reactive config:
  - `ScheduleInitializationService` runs on startup to configure jobs.
  - `SettingChangedListener` reacts when relevant settings change and:
    - Schedules indexers when Radarr/Sonarr settings become complete.
    - Toggles automation schedules on or off.
    - Clears media hashes or adjusts schedules when appropriate.

If you change any cron expression, job name, or queue:

- Update both `ScheduleService` and `SettingChangedListener` where applicable.
- Adjust any client UI showing schedule info (`Lingarr.Client/src/pages` and stores).
- Consider migration/upgrade behavior: existing persisted Hangfire jobs may already exist with old IDs.

### 4.3 Logging and live log stream

- Regular logging:
  - Most services/or jobs use `ILogger<T>` and the colored console logger from `Lingarr.Core/Logging`.
- In‑memory log store:
  - `Lingarr.Server/Providers/InMemoryLogProvider.cs` (log sink + `InMemoryLoggerProvider`)
  - In non‑DEBUG builds, this provider is added in `ServiceCollectionExtensions.ConfigureLogging`.
  - `LogsController.GetLogStreamAsync` exposes a server‑sent events endpoint at `/api/logs/stream`.

When you add new logging:

- Prefer structured messages with placeholders (`{PropertyName}`).
- Use color markers `|Green|`, `|Orange|`, `|Red|` only as seen in existing logs.
- Do not log full stack traces unnecessarily on expected errors; do log enough to debug in production.

### 4.4 Frontend structure and data flow

- HTTP layer:
  - `Lingarr.Client/src/services/index.ts` wires axios into per‑domain service modules.
  - Service interfaces live in `Lingarr.Client/src/ts/services.ts`.
  - Each new backend endpoint should be represented as a method on one of these service interfaces and implemented in `src/services`.
- State and side effects:
  - Pinia stores in `Lingarr.Client/src/store` handle state, API calls, and SignalR event handling.
  - Example: `translationRequest` store interacts with `translationRequest` service and updates UI state.
- Components and pages:
  - `src/pages` are container components; they call into stores and services rather than doing API calls by themselves.
  - Shared UI components live in `src/components`.

Frontend change rules:

- Add new API calls via the services layer; do not scatter axios usage.
- Keep Pinia stores responsible for coordinating network + state; pages should mostly orchestrate UI.
- Keep TypeScript types in sync with backend DTOs; update interfaces in `src/ts/**` if backend responses change.

---

## 5. Using Tools Inside This Environment

When you work as an AI agent inside this repo:

- **Search code**: use `rg "pattern" -n` at the repo root to find relevant usages.
- **Read files**: prefer reading specific files or partial outputs (≤ 250 lines) instead of dumping extremely large files.
- **Apply edits**: always use `apply_patch`; keep your diffs as small as possible and focused on the task.
- **Do not**:
  - Run destructive commands (`git reset --hard`, `rm -rf` on folders) unless the user explicitly asks.
  - Modify Git configuration or remotes.

---

## 6. Testing and Verification

Whenever you change code, think in terms of three layers: **build**, **tests**, and **manual verification**.

### 6.1 Backend

From the repo root:

- Build solution:
  - `dotnet build Lingarr.sln`
- Run server tests:
  - `dotnet test Lingarr.Server.Tests/Lingarr.Server.Tests.csproj`

Guidance:

- New or changed business logic in services or jobs should have unit tests in `Lingarr.Server.Tests`.
- Use the existing patterns:
  - xUnit for test cases.
  - Moq for dependencies.
  - EF InMemory for database interactions where needed.

### 6.2 Frontend

From `Lingarr.Client`:

- Ensure dependencies are installed: `npm install`
- Build:
  - `npm run build`
- For basic sanity (optional):
  - `npm run dev` and visit http://localhost:9876 if appropriate for the task.

There is no formal frontend test suite at the moment. Do not introduce one unless the user asks for it; if they do, keep it minimal and consistent with Vue ecosystem tools.

### 6.3 Migrations

If you changed the schema:

- Run `./create-migrations.ps1 -MigrationName "DescriptiveName"` to regenerate migrations for both providers.
- Optionally validate:
  - Apply migrations locally to a SQLite/MariaDB instance (can be done via docker compose).
  - Ensure the app starts and can perform basic operations (see Readme for endpoints).

---

## 7. Notes on Git, Branches, and PRs (for when changes leave this environment)

This file doesn’t execute Git commands for you, but you should assume contributions will follow the pattern in `CONTRIBUTING.md`:

- Branch naming:
  - `feat/short-description` for features
  - `fix/short-description` for bug fixes
- Commit messages:
  - Follow Conventional Commits (`feat: ...`, `fix: ...`, etc.).
- Pull requests:
  - Clearly state what you changed, why, and how you verified it.
  - Mention any schema or setting changes and their impact on existing installations.

As an AI agent you **must not**:

- Edit commit history (`git rebase`, `git push --force`) unless the user explicitly instructs you to.
- Push directly to the main branch of the upstream repository.

---

## 8. Quick Checklists

### 8.1 Before finishing any backend change

- [ ] Controllers, services, jobs, and entities you touched are internally consistent.
- [ ] If you modified entities or relationships, migrations exist in **both** migration projects.
- [ ] Settings you rely on are declared in `SettingKeys`, populated by `StartupService`, and documented in `Settings.MD`.
- [ ] Long‑running work is done in a Hangfire job rather than a controller where appropriate.
- [ ] You did not introduce any logging of secrets or media file deletion.
- [ ] `dotnet build` and relevant tests would pass based on the changes you made.

### 8.2 Before finishing any frontend change

- [ ] All new API calls go through `src/services` and have matching TypeScript interfaces.
- [ ] Pinia stores are updated if data shapes or flows changed.
- [ ] Components compile and the TypeScript checker would be happy (`npm run build`).
- [ ] You did not introduce new global dependencies or tooling without reason.

### 8.3 Before ending your assistant run

- [ ] You described what you changed and where (paths).
- [ ] You pointed out any follow‑up steps the user should run (build, tests, manual checks).
- [ ] You highlighted any potentially breaking changes (schema, settings, jobs, API contracts).

If you are unsure whether a change is safe, favor the smaller and more conservative edit, and explain the trade‑offs to the user.  
This project is built around automation and background work; subtle changes can have wide‑ranging effects. When in doubt, read more code and do less. 

