# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Home Screen Sections (HSS) is a Jellyfin server plugin that replaces the web client's home screen with configurable "Netflix-style" sections. It is a single C# project (`src/Jellyfin.Plugin.HomeScreenSections`) with no test project. At runtime it depends on two other plugins by the same author, **File Transformation** and **Plugin Pages**, which it talks to only via reflection.

## Build

```sh
dotnet build src/Jellyfin.Plugin.HomeScreenSections.sln
dotnet build src/Jellyfin.Plugin.HomeScreenSections.sln -p:JellyfinVersion=10.11.0   # build against a different Jellyfin
```

- `JellyfinVersion` in the csproj (default `12.1.0`) decides the target framework (`10.11.*` → net9.0, `12.*` → net10.0), the Jellyfin NuGet package versions, and which `JellyfinVersionSpecific/<ver>/` folder gets compiled. The folders for other versions are excluded from compilation.
- The version is also baked into the assembly as `JellyfinVersionAttribute`, and `TransformationPatches` reads it at runtime to choose the minified jellyfin-web variable names.
- PostBuild copies output into `C:\ProgramData\Jellyfin\Server_*\plugins\HomeScreenSections` only when that folder exists (the author's Windows setup), so it does nothing anywhere else.
- No tests or linter are configured. To check a change, build it against each supported Jellyfin version (the README asks contributors to check 10.10.7 and the latest).
- `.gitmodules` points to a `jellyfin-web` fork submodule, but it isn't checked out here.

## Architecture

**Bootstrapping**
- `PluginServiceRegistrator` registers the singletons in DI. When it builds `HomeScreenManager`, it also loads any extra `*.dll` from the plugin config dir and registers every `IHomeScreenSection` type it finds in them.
- The `HomeScreenSectionsPlugin` constructor calls `RegisterBuiltInResultsDelegates()`.
- `ModuleInitializer` loads embedded `.dll` resources into a private `AssemblyLoadContext`.

**Web client injection** (`Services/StartupService.cs`, an `IScheduledTask` in the "Startup Services" category)
- Scans `WebPath` for `*.chunk.js` files that contain `,loadSections:`, then uses reflection to call `Jellyfin.Plugin.FileTransformation.PluginInterface.RegisterTransformation`.
- The callbacks in `Helpers/TransformationPatches.cs` do the actual patching:
  - `LoadSections` splices the embedded `Controllers/loadSections.js` into the web bundle. It replaces `{{..._hook}}` placeholders with minifier-specific variable names that are hardcoded per Jellyfin version, so these break whenever jellyfin-web's build changes.
  - `IndexHtml` injects `/HomeScreen/home-screen-sections.{js,css}` with a cache-bust query string (`DeveloperMode` / `CacheBustCounter`).
- It also registers the user settings page with Plugin Pages via reflection.

**Sections**
- Every section implements `IHomeScreenSection` (`Library/IHomeScreenManager.cs`) and lives under `HomeScreen/Sections/`.
- Shared behaviour sits in base classes: `LatestSectionBase`, `RecentlyAddedSectionBase`, `UpcomingSectionBase` (\*arr calendars via `ArrApiService`) and `Persons/PersonsSectionBase`.
- `Section` is the unique key. `CreateInstances(userId, count)` lets a section appear several times with different `AdditionalData` (for example one per genre).
- `GetPluginConfigurationOptions()` declares per-section admin options, built with `Helpers/PluginConfigurationHelper`.
- **To add a built-in section:** create the class (sections are `internal` and get dependencies through constructor DI via `ActivatorUtilities`), then add a `RegisterResultsDelegate<T>()` line in `HomeScreenManager.RegisterBuiltInResultsDelegates()`.
- External plugins register sections in one of two ways:
  - reflection into `PluginInterface.RegisterSection` (payload: `Model/SectionRegisterPayload`), which runs a results class/method from the caller's assembly or POSTs to a results endpoint;
  - `POST /HomeScreen/RegisterSection` (admin only).
  Either way the section is wrapped in `PluginDefinedSection`.

**Request flow**
- `HomeScreenController` exposes `GET /HomeScreen/Sections` (paged section list) and `GET /HomeScreen/Section/{sectionType}` (items for one section).
- `HomeScreenSectionService.MonitorLiveUpdatedSectionsForUser` builds the per-user ordered section list. It combines user settings, admin `SectionSettings` (enabled, locked via `AllowUserOverride`, order) and lazy-load paging, then caches the result in `Data/UserSectionsDataCache` keyed by a `pageHash`.
- `ModularHomeViewsController` serves the user settings UI and reads/writes per-user settings. These are stored as JSON files under `PluginConfigurationsPath/Jellyfin.Plugin.HomeScreenSections/` (`ModularHomeSettings.json`, `userFeatureEnabled.json`).
- **Auth rule:** user-facing endpoints resolve the caller through `Helpers/UserAuthorizationHelper.TryGetUserId`. A `userId` parameter is optional, but if present it must match the authenticated user (admins included), otherwise the endpoint returns 403. Keep this pattern on any new endpoint.
- **Library access:** filter by user library permissions with `FilterToUserPermitted` (`Helpers/MiscExtensions.cs`).

**Caching**
- `Services/PerUserComputedStatsCache.TryGetOrCompute` caches expensive per-user computations (candidate pools, watch-history stats). Entries expire after a max age and, by default, when the user's data changes.
- `ImageCacheService` / `ImageCacheCleanupTask` cache external (Jellyseerr/\*arr) images, served from `/HomeScreen/CachedImage/{key}`.

**Frontend assets** (all embedded resources, registered in the csproj)
- `Inject/HomeScreenSections.{js,css}`: global client script, for example Jellyseerr request buttons.
- `Controllers/loadSections.js`: the replacement `loadSections` function.
- `Config/settings.html`: the user settings page.
- `Configuration/config.{html,css}`: the admin dashboard page.
- Adding a new asset file means adding an `EmbeddedResource` entry for it.

**Version-specific code**
- APIs that differ between Jellyfin versions go through same-named classes in `JellyfinVersionSpecific/10.11/` and `JellyfinVersionSpecific/12.x/` (`ExtensionsHelper`, `CollectionManagerProxy`, `BecauseYouWatchedHelper`, `StartupServiceHelper`, `GlobalUsings`).
- When an API differs, add a method to both versions rather than using `#if`.

**Localization**
- `_Localization/*.json`, with file names using ISO 639-1 codes (optionally `-REGION`).
- Genre keys go below the separator line in each file, written as PascalCase English with no spaces or dashes (e.g. `SciFi`). Full "`<Genre> Movies`" translations use keys like `ComedyMovies`, placed at the very bottom.
- `TranslationManager` handles the files and can fall back to LibreTranslate.

## Code style (from README; the maintainer enforces it)

- Allman braces (opening brace on its own line), and braces even on single-line bodies.
- Explicit types; avoid `var` unless a type's namespace differs between Jellyfin versions.
- Mutated Hungarian naming: `m_` for private fields, `s_` for statics, `c_` for constants. Injected services are often private properties (`UserManager`, `LibraryManager`).
- No whitespace-only changes or reformatting of untouched code, so don't run auto-formatters.
- Contributors are asked to declare AI usage in PRs. Larger changes may be asked to rebase onto the `experimental` branch.
