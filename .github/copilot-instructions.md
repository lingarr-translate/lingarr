# Lingarr Copilot Instructions

## Project Overview
Lingarr is a subtitle translation application that leverages multiple translation services to automatically translate subtitle files. Built with .NET 9.0 backend and Vue.js 3 frontend.

## Architecture

### Backend Stack
- **Framework**: ASP.NET Core 9.0
- **Database**: SQLite (default) or MySQL
- **ORM**: Entity Framework Core 9.0
- **Background Jobs**: Hangfire
- **APIs**: RESTful endpoints with Swagger documentation

### Frontend Stack
- **Framework**: Vue.js 3 with Composition API
- **State Management**: Pinia stores
- **Build Tool**: Vite
- **Language**: TypeScript with strict type checking
- **Styling**: Tailwind CSS
- **UI Components**: Custom components following consistent patterns

### Project Structure
```
Lingarr/
├── Lingarr.Server/          # Backend API (.NET 9.0)
│   ├── Controllers/         # API endpoints
│   ├── Services/            # Business logic
│   ├── Jobs/                # Hangfire background jobs
│   ├── Models/              # DTOs and API models
│   └── Interfaces/          # Service contracts
├── Lingarr.Core/            # Domain layer
│   ├── Entities/            # Database entities
│   ├── Enum/                # Enumerations
│   └── Interfaces/          # Core interfaces
├── Lingarr.Client/          # Vue.js frontend
│   ├── src/
│   │   ├── pages/           # Page components
│   │   ├── components/      # Reusable UI components
│   │   ├── store/           # Pinia stores
│   │   ├── services/        # API services
│   │   ├── router/          # Vue Router
│   │   └── ts/              # TypeScript interfaces
│   └── public/              # Static assets
├── Lingarr.Migrations.SQLite/  # SQLite migrations
├── Lingarr.Migrations.MySQL/   # MySQL migrations
└── deployment/              # Ansible deployment scripts
```

## Development Guidelines

### Code Style

#### Backend (.NET/C#)
- Follow standard C# conventions (PascalCase for public members)
- Use `async/await` for all I/O operations
- Implement proper exception handling with logging
- Use dependency injection for all services
- Entity properties use PascalCase, DB columns use snake_case (configured via EF Core)

#### Frontend (Vue.js/TypeScript)
- Use Composition API with `<script setup lang="ts">`
- Interface names start with `I` (e.g., `IMovie`, `IShow`)
- Use TypeScript for all new code
- Component file names use PascalCase (e.g., `MoviePage.vue`)
- Store files use lowercase (e.g., `movie.ts`, `show.ts`)
- Follow Vue 3 best practices with reactive refs and computed properties

### Key Patterns

#### Type Safety
- Backend entities use `bool` for boolean properties
- Frontend interfaces must match backend types exactly
- Example: `ExcludeFromTranslation` is `bool` in C#, `boolean` in TypeScript (NOT string)

#### API Communication
- All API calls go through service classes in `Lingarr.Client/src/services/`
- Use `PagedResult<T>` for paginated endpoints
- Include global counts (IncludedCount, ExcludedCount) in pagination responses

#### State Management
- Each domain has its own Pinia store (movies, shows, settings, etc.)
- Stores expose computed getters for derived state
- Use store actions for API calls, not direct calls from components

#### Database
- Use snake_case for column names (configured in EntityConfiguration classes)
- Example: `ExcludeFromTranslation` property → `exclude_from_translation` column
- Always include proper EF Core migrations for schema changes

### Translation Services
Supported services (11 total):
1. LibreTranslate
2. Google Translate
3. Bing Translator
4. Microsoft Azure Translator
5. Yandex Translate
6. DeepL
7. OpenAI
8. Anthropic
9. LocalAI (Ollama compatible)
10. DeepSeek
11. Gemini

### Recent Features

#### Include/Exclude Toggle (Issue #159)
- Movies and TV shows can be excluded from translation
- Cascading behavior: toggling a show affects all seasons and episodes
- Global counters show included/excluded counts across all items
- Sort by included status with ascending/descending toggle
- Implementation:
  - Backend: `ExcludeFromTranslation` boolean property on entities
  - Frontend: Toggle components with proper boolean state management
  - API: `/api/Media/include` endpoint for individual items
  - API: `/api/Media/includeAll` endpoint for bulk operations

#### Sonarr-Style Sort Controls
- Custom dropdown menu (not native `<select>`)
- Visual arrow indicators (↑/↓) show in menu next to active sort option
- Click same option to toggle direction
- Click different option to change sort field
- Follows Sonarr UX pattern for consistency

#### Translation Error Display
- Shows error messages for failed translations
- Displays "Translation failed" even when errorMessage is null
- Prevents exposure of sensitive API keys in error messages

### Database Schema

#### Key Entities
```csharp
public class Movie : BaseEntity {
    public int RadarrId { get; set; }
    public string Title { get; set; }
    public string? Path { get; set; }
    public DateTime? DateAdded { get; set; }
    public bool ExcludeFromTranslation { get; set; }
    public int? TranslationAgeThreshold { get; set; }
    public List<Image> Images { get; set; }
}

public class Show : BaseEntity {
    public int SonarrId { get; set; }
    public string Title { get; set; }
    public string? Path { get; set; }
    public DateTime? DateAdded { get; set; }
    public bool ExcludeFromTranslation { get; set; }
    public int? TranslationAgeThreshold { get; set; }
    public List<Season> Seasons { get; set; }
    public List<Image> Images { get; set; }
}
```

### Deployment
- Ansible-based deployment to NAS
- Automatic database backup (keeps last 10 backups)
- Docker buildx for multi-platform builds (amd64, arm64)
- Location: `/home/boril/repos/lingarr/deployment/`
- Script: `./deploy.sh`

### Testing Checklist
Before pushing to git:
1. ✅ Run TypeScript type checking: `npm run build` in Lingarr.Client
2. ✅ Check for compilation errors in .NET backend
3. ✅ Test deployment locally with `./deployment/deploy.sh`
4. ✅ Verify database migrations apply cleanly
5. ✅ Test UI functionality in browser
6. ✅ Check for console errors in browser dev tools
7. ✅ Verify API responses match TypeScript interfaces

### Common Issues and Solutions

#### Type Mismatch Errors
**Problem**: TypeScript errors about boolean vs string
**Solution**: Ensure frontend TypeScript interfaces match backend C# types exactly

#### Toggle State Issues
**Problem**: Toggle buttons showing incorrect state
**Solution**: Verify property is boolean throughout stack (DB → Entity → DTO → Interface)

#### Build Failures
**Problem**: Docker build fails with TypeScript errors
**Solution**: Run `npm run build` locally first to catch type errors

#### Migration Issues
**Problem**: Database migration failures on deployment
**Solution**: Test migrations locally against SQLite database first

#### Caption Logic Bug (Fixed: Jan 24, 2026)
**Problem**: Episodes without target language subtitles were skipped with message "captions exist for target languages" even though they had no such subtitles
**Root Cause**: MediaSubtitleProcessor.cs line 110 had inverted logic - `if (ignoreCaptions == "true")` should be `if (ignoreCaptions == "false")`
**Explanation**: When `ignoreCaptions=false` (disabled), the system should check for captions and skip if they exist. When `ignoreCaptions=true` (enabled), it should ignore captions and translate anyway. The condition was backwards.
**Fix**: Changed line 110 from `if (ignoreCaptions == "true")` to `if (ignoreCaptions == "false")`
**Impact**: Automation job now properly translates episodes/movies that have no target language subtitles

#### Telemetry Settings Not Persisting (Fixed: Jan 24, 2026)
**Problem**: Telemetry (and auth) settings would reset to default after every Docker deployment
**Root Cause**: StartupService.ApplySettingsFromEnvironment() overwrites database settings with environment variables on every startup, ignoring user preferences set in UI
**Explanation**: Settings like telemetry_enabled and auth_enabled are user preferences that should only be initialized from env vars on first run, not overwritten every time
**Fix**: Added `userPreferenceSettings` HashSet in StartupService.cs to identify settings that should only apply from env vars if still at default value ("false"). If user changed to "true", subsequent deployments won't overwrite.
**Impact**: User preferences now persist across Docker container restarts and redeployments

### Naming Conventions

#### Files
- Vue Components: `PascalCase.vue` (e.g., `MoviePage.vue`)
- TypeScript files: `camelCase.ts` (e.g., `movieService.ts`)
- Store files: `lowercase.ts` (e.g., `movie.ts`, `show.ts`)
- C# files: `PascalCase.cs` (e.g., `MediaController.cs`)

#### Code
- C# Classes/Properties: `PascalCase`
- TypeScript Interfaces: `IPascalCase`
- TypeScript Variables: `camelCase`
- Database Columns: `snake_case`
- API Endpoints: `camelCase` (e.g., `/api/media/includeAll`)

### Important Notes
- Always use boolean types for true/false properties (not strings)
- Include/exclude logic: `true` = excluded, `false` = included
- Toggle ON state = included (excludeFromTranslation = false)
- Sort ascending = included first when sorting by included status
- Global counts must come from database aggregation, not page-level counts
- Cascading updates for TV shows must update show, seasons, and episodes
- Caption handling: `ignoreCaptions=false` checks for captions and skips if found; `ignoreCaptions=true` ignores captions and translates anyway
- User preference settings (telemetry, auth) only apply from env vars if still at default values to prevent overwriting UI changes

### Environment Variables
Key variables (see Settings.MD for complete list):
- `ASPNETCORE_URLS`: Internal port (default: http://+:9876)
- `DB_CONNECTION`: Database type (`sqlite` or `mysql`)
- `SERVICE_TYPE`: Translation service to use
- `MAX_CONCURRENT_JOBS`: Concurrent translation jobs (default: 1)
- `AUTH_ENABLED`: Enable/disable authentication
- `TELEMETRY_ENABLED`: Opt-in telemetry (default: false)

### Translation Keys
UI strings are localized in `Lingarr.Server/Statics/Translations/en.json`
Always add translation keys for new UI text:
```json
{
  "common": {
    "sortByTitle": "Sort by Title",
    "sortByAdded": "Sort by Added",
    "sortByIncluded": "Sort by Included"
  }
}
```

### API Patterns

#### Pagination
```csharp
public class PagedResult<T> {
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int? IncludedCount { get; set; }  // Global count
    public int? ExcludedCount { get; set; }  // Global count
}
```

#### Include/Exclude Requests
```csharp
public class IncludeRequest {
    public required MediaType MediaType { get; set; }
    public required int Id { get; set; }
    public required bool Include { get; set; }
}
```

### Docker Configuration
- Base image: `mcr.microsoft.com/dotnet/aspnet:9.0`
- Build image: `mcr.microsoft.com/dotnet/sdk:9.0`
- Node image: `node:24-slim`
- Multi-stage build: Client → Server → Final
- Supports amd64 and arm64 architectures

### External Integrations
- **Radarr**: Movie library management
- **Sonarr**: TV show library management
- **Translation Services**: 11 supported providers
- **Hangfire Dashboard**: Background job monitoring

### Security Notes
- Never expose API keys in error messages
- Sanitize error messages before sending to frontend
- Use authentication when `AUTH_ENABLED=true`
- Keep sensitive configuration in environment variables

---

*Last Updated: January 24, 2026*
*Version: 1.x*
