# Lingarr - GitHub Copilot Instructions

## Project Overview

**Lingarr** is a full-stack application that automates subtitle translation for media files. It integrates with Radarr and Sonarr to detect new media, automatically translates subtitle files using various translation services, and provides a web-based dashboard for management and monitoring.

**Current Version**: 1.0.3  
**Branch**: main  
**Repository**: https://github.com/lingarr-translate/lingarr

## Architecture

### Technology Stack

#### Backend (.NET 9.0)
- **Framework**: ASP.NET Core Web API
- **Database**: SQLite (default) or MySQL/MariaDB
- **Job Scheduling**: Hangfire for background job processing
- **Real-time Communication**: SignalR for live updates
- **ORM**: Entity Framework Core
- **Language**: C# 12

#### Frontend (Vue.js 3)
- **Framework**: Vue 3 with Composition API
- **Language**: TypeScript
- **Build Tool**: Vite 6
- **Styling**: Tailwind CSS 4
- **State Management**: Pinia
- **Routing**: Vue Router
- **HTTP Client**: Axios
- **Real-time**: SignalR client

### Project Structure

```
Lingarr/
├── Lingarr.Server/           # Backend API application
│   ├── Controllers/          # REST API endpoints
│   ├── Services/             # Business logic layer
│   ├── Jobs/                 # Hangfire background jobs
│   ├── Hubs/                 # SignalR hubs for real-time updates
│   ├── Providers/            # Translation service providers
│   ├── Extensions/           # Service/application extensions
│   ├── Models/               # DTOs and request/response models
│   └── Filters/              # Action filters and middleware
│
├── Lingarr.Core/             # Core domain layer
│   ├── Entities/             # Database entities (Movie, Show, Episode, etc.)
│   ├── Interfaces/           # Service interfaces
│   ├── Configuration/        # EF Core configurations
│   ├── Data/                 # DbContext
│   ├── Enum/                 # Enumerations
│   ├── Models/               # Domain models
│   └── Logging/              # Logging utilities
│
├── Lingarr.Client/           # Frontend Vue.js application
│   ├── src/
│   │   ├── components/       # Vue components
│   │   │   ├── common/       # Reusable UI components
│   │   │   ├── features/     # Feature-specific components
│   │   │   ├── layout/       # Layout components
│   │   │   └── icons/        # Icon components
│   │   ├── pages/            # Page-level components
│   │   ├── router/           # Vue Router configuration
│   │   ├── store/            # Pinia stores
│   │   ├── services/         # API service layer
│   │   ├── composables/      # Vue composables
│   │   ├── utils/            # Utility functions
│   │   └── plugins/          # Vue plugins (i18n)
│   └── public/               # Static assets
│
├── Lingarr.Migrations.SQLite/   # SQLite EF Core migrations
├── Lingarr.Migrations.MySQL/    # MySQL EF Core migrations
├── Lingarr.Server.Tests/        # Unit and integration tests
│   ├── Jobs/                 # Job tests
│   └── Services/             # Service tests (including translation)
└── deployment/                   # Ansible deployment scripts
```

## Core Features

### Translation Services Integration
Lingarr supports multiple translation providers:
- **LibreTranslate** (self-hosted, open source)
- **DeepL** (commercial API)
- **OpenAI** (GPT models)
- **Anthropic** (Claude models)
- **Google Translate** (free API)
- **Bing Translator**
- **Yandex Translate**
- **Azure Translator**
- **Gemini** (Google AI)
- **DeepSeek**
- **Local AI** (Ollama or OpenAPI-compatible)

### Key Functionality
1. **Media Integration**: Syncs with Radarr (movies) and Sonarr (TV shows)
2. **Automatic Translation**: Background jobs monitor for new media and translate subtitles
3. **Path Mapping**: Dynamic path mapper for Docker volume mounts
4. **Batch Processing**: Efficient batch translation for multiple subtitle lines
5. **Schedule Management**: Cron-based scheduling for translation jobs
6. **Statistics Dashboard**: Real-time metrics and translation history
7. **Multi-language Support**: UI localization system

## Development Guidelines

### Backend Development

#### Code Style
- Follow C# coding conventions and naming standards
- Use `async/await` for all I/O operations
- Implement proper error handling with try-catch blocks
- Use dependency injection for all services
- Document public APIs with XML comments
- Use Entity Framework Core for all database operations

#### Key Patterns
- **Repository Pattern**: Data access through EF Core DbContext
- **Service Layer**: Business logic in service classes
- **Background Jobs**: Use Hangfire for long-running tasks
- **Real-time Updates**: SignalR hubs for progress notifications
- **Provider Pattern**: Abstract translation services behind interfaces

#### Important Classes
- `TranslationRequestService`: Orchestrates translation workflows
- `SubtitleTranslationService`: Core translation logic
- `GoogleGeminiService`: Gemini API integration with JSON truncation handling
- `DeepLService`: DeepL API with enhanced retry logic
- `RadarrService`/`SonarrService`: Media server integration
- `PathMappingService`: Handles Docker path mappings
- `SettingService`: Configuration management
- `TranslationException`: Custom exception with proper inner exception chaining

### Frontend Development

#### Code Style
- Use TypeScript for all new code
- Follow Vue 3 Composition API patterns
- Use `<script setup>` syntax
- Implement proper type definitions
- Use Tailwind CSS classes for styling
- Keep components focused and composable

#### Key Patterns
- **Composables**: Extract reusable logic (e.g., `useModelOptions`, `useLocalStorage`)
- **Services**: API calls abstracted in service layer
- **Pinia Stores**: State management for app-wide data
- **Components**: Small, reusable components with clear props/emits

#### Component Organization
- `common/`: Generic UI components (buttons, inputs, cards)
- `features/`: Domain-specific components (settings, media, dashboard)
- `layout/`: Page structure components (navigation, page layout)
- `icons/`: SVG icon components

#### Important Files
- `services/index.ts`: API service definitions
- `store/instance.ts`: Global app state
- `router/index.ts`: Route definitions
- `plugins/i18n.ts`: Internationalization system

### Database

#### Entities
- **Movie**: Radarr movie entries
- **Show**: Sonarr TV show entries
- **Season**: TV show seasons
- **Episode**: Individual episodes
- **Setting**: Key-value configuration store
- **PathMapping**: Docker volume path mappings
- **DailyStatistics**: Aggregated translation metrics

#### Migrations
Use PowerShell script to create migrations for both providers:
```powershell
./create-migrations.ps1 -MigrationName "YourMigrationName"
```

Migrations are applied automatically on application startup.

### Environment Variables

#### Core Configuration
- `ASPNETCORE_URLS`: Internal listening URL (default: `http://+:9876`)
- `MAX_CONCURRENT_JOBS`: Concurrent translation jobs (default: 1)
- `DB_CONNECTION`: Database type (`sqlite` or `mysql`)

#### Database (MySQL)
- `DB_HOST`, `DB_PORT`, `DB_DATABASE`, `DB_USERNAME`, `DB_PASSWORD`

#### Integration
- `RADARR_URL`, `RADARR_API_KEY`
- `SONARR_URL`, `SONARR_API_KEY`

#### Translation Services
Each service has specific environment variables (see Settings.MD for full list):
- LibreTranslate: `LIBRE_TRANSLATE_URL`, `LIBRE_TRANSLATE_API_KEY`
- OpenAI: `OPENAI_API_KEY`, `OPENAI_MODEL`
- DeepL: `DEEPL_API_KEY`
- Etc.

## Development Workflow

### Local Development Setup

1. **Prerequisites**
   - Docker and Docker Compose
   - .NET 9.0 SDK
   - Node.js 24+
   - PowerShell (for build scripts)

2. **Start Development Environment**
   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```

3. **Access Services**
   - Lingarr UI: http://localhost:9876
   - Swagger API: http://localhost:9877/swagger/index.html
   - Hangfire Dashboard: http://localhost:9877/hangfire
   - phpMyAdmin: http://localhost:9878
   - Sonarr: http://localhost:8989
   - Radarr: http://localhost:7878

4. **Hot Reload**
   - Frontend: Supports Vite hot module replacement
   - Backend: Requires rebuild after changes

### Building and Deployment

#### Docker Build
```powershell
# Development build
./build-and-push.ps1 -Tag dev

# Release build
./build-and-push.ps1 -Tag 1.0.3
```

The script builds for both `linux/amd64` and `linux/arm64` platforms.

#### Testing
```bash
# Run all tests
dotnet test Lingarr.Server.Tests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~GoogleGeminiServiceTests"

# Run integration tests (requires API keys)
export LINGARR_TEST_GEMINI_KEY="your-api-key"
dotnet test --filter "FullyQualifiedName~Integration"
```

**Note**: Integration tests auto-skip in CI/CD when API keys are not set.

### Git Workflow

- **Branching**: Use conventional branch names (`feat/`, `fix/`, `chore/`)
- **Commits**: Follow [Conventional Commits](https://www.conventionalcommits.org/)
- **Pull Requests**: Reference related issues, provide clear descriptions

## Common Tasks

### Adding a New Translation Service

1. Create provider class implementing `ITranslationProvider` in `Lingarr.Server/Providers/`
2. Add service configuration to `SettingKeys.cs`
3. Update `TranslationProviderFactory` to instantiate new provider
4. Add frontend configuration component in `Lingarr.Client/src/components/features/settings/services/`
5. Update `Settings.MD` with environment variables

### Adding a New API Endpoint

1. Create controller in `Lingarr.Server/Controllers/`
2. Implement service logic in `Lingarr.Server/Services/`
3. Add corresponding service interface to `Lingarr.Core/Interfaces/`
4. Register service in `ServiceCollectionExtensions.cs`
5. Add frontend service method in `Lingarr.Client/src/services/`
6. Update TypeScript types in `Lingarr.Client/src/ts/`

### Adding a New Entity

1. Create entity class in `Lingarr.Core/Entities/`
2. Add EF Core configuration in `Lingarr.Core/Configuration/`
3. Add DbSet to `LingarrDbContext.cs`
4. Create migration: `./create-migrations.ps1 -MigrationName "AddNewEntity"`
5. Add corresponding TypeScript interface in frontend

## Important Notes

### Path Mapping
- The dynamic path mapper translates paths between Radarr/Sonarr and Lingarr containers
- Issues may occur with deeply nested directory structures
- Consult wiki or create issue if mapping problems arise

### Performance Considerations
- Use batch translation for large subtitle files when possible
- Configure `MAX_CONCURRENT_JOBS` based on available resources
- Translation API rate limits vary by provider

### Known Issues
- DeepL does not support Brazilian Portuguese (pt-BR)
- Dynamic path mapper may have issues with complex directory hierarchies

### Recent Fixes (v1.0.3)
- **Google Gemini JSON Truncation (Issue #204)**: Fixed batch translation failures
  - Added `maxOutputTokens: 8192` configuration
  - Implemented `TryRepairJson` method for graceful degradation
  - Transforms 100% crashes into 76-92% success rate
- **TranslationException**: Now properly chains inner exceptions for debugging
- **Sonarr/DeepL**: Enhanced resilience and retry logic

## Resources

- **Documentation**: README.md, CONTRIBUTING.md, Settings.MD
- **API Docs**: https://lingarr.com/docs/api/
- **GitHub**: https://github.com/lingarr-translate/lingarr
- **Discord**: https://discord.gg/HkubmH2rcR
- **Wiki**: https://github.com/lingarr-translate/lingarr/wiki

## License

GNU Affero General Public License v3.0

---

## Quick Reference

### Key Files to Know
- `Lingarr.Core/LingarrVersion.cs`: Version number (currently 1.0.3)
- `Lingarr.Server/Program.cs`: Application entry point
- `Lingarr.Core/Data/LingarrDbContext.cs`: Database context
- `Lingarr.Client/src/main.ts`: Frontend entry point
- `docker-compose.dev.yml`: Development environment configuration

### Common Commands
```bash
# Create migrations
./create-migrations.ps1 -MigrationName "MigrationName"

# Build Docker images
./build-and-push.ps1 -Tag dev

# Start dev environment
docker-compose -f docker-compose.dev.yml up -d

# Frontend dev server (in Lingarr.Client/)
npm run dev

# Build frontend
npm run build
```

### Debugging
- Backend: Launch "Lingarr.Server" in IDE with debugging
- Frontend: Use browser dev tools, Vue DevTools extension
- Database: Access via phpMyAdmin at http://localhost:9878
- Jobs: Monitor Hangfire dashboard at http://localhost:9877/hangfire

---

**Last Updated**: November 30, 2025  
**For**: GitHub Copilot and AI-assisted development

## Recent Development Activity

### Latest Changes (November 2025)
1. **Issue #204 - Gemini JSON Truncation**: Implemented comprehensive fix
   - Prevention: Increased token limits
   - Resilience: JSON repair logic
   - Testing: Unit tests + real API integration tests
2. **Exception Handling**: Fixed TranslationException inner exception propagation
3. **Documentation**: Added CI/CD workflows, security policy, code of conduct
4. **Testing Infrastructure**: Comprehensive test suite with mocking and real API tests
