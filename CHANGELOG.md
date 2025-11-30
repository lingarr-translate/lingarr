# Changelog

All notable changes to Lingarr will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2025-11-30

### Fixed
- **Google Gemini**: Fixed JSON truncation handling in batch translation (Issue #204)
  - Increased `maxOutputTokens` to 8192 to reduce truncation likelihood
  - Added `TryRepairJson` method to salvage partial responses
  - Transforms catastrophic failures into graceful degradation (76-92% success rate)
- **Exception Handling**: Fixed `TranslationException` to properly chain inner exceptions for better debugging
- Improved Sonarr integration resilience with 404 fallback handling
- Enhanced DeepL translation service error handling and retry logic
- Fixed path mapping issues with deeply nested directories

### Changed
- Updated to `gemini-2.5-flash` model (Gemini 1.x models deprecated)
- Updated frontend dependencies (Vue 3.5.12, Vite 6.4.1, Tailwind CSS 4.0)
- Improved error logging and debugging capabilities
- Enhanced translation retry logic with exponential backoff

### Added
- Comprehensive test suite for Google Gemini service
  - Unit tests for JSON truncation scenarios
  - Integration tests with real API (auto-skip in CI/CD)
- CI/CD workflows for automated testing and Docker builds
- Complete documentation suite (SECURITY.md, CODE_OF_CONDUCT.md, GitHub templates)
- Dependabot configuration for automated dependency updates

## [1.0.2] - 2025-10-10

### Added
- Multi-language UI support with i18n
- Enhanced statistics dashboard
- Daily translation statistics tracking

### Fixed
- Subtitle formatting issues
- Translation batch processing improvements

## [1.0.1] - 2025-07-25

### Added
- Support for additional translation services (Gemini, DeepSeek)
- Improved schedule management with cron expressions
- Path mapping configuration UI

### Fixed
- Docker volume mounting issues
- Subtitle overlap detection and correction

## [1.0.0] - 2025-07-06

### Added
- Initial stable release
- Support for multiple translation services:
  - LibreTranslate
  - DeepL
  - OpenAI
  - Anthropic
  - Google Translate
  - Bing Translator
  - Yandex Translate
  - Azure Translator
  - Local AI (Ollama)
- Radarr and Sonarr integration
- Automatic subtitle translation
- Background job scheduling with Hangfire
- Real-time progress updates via SignalR
- Web-based dashboard
- SQLite and MySQL database support
- Docker deployment

### Known Issues
- DeepL does not support Brazilian Portuguese (pt-BR)
- Path mapper may have issues with complex directory structures

---

## Development Notes

### Version Numbering
Lingarr follows semantic versioning (MAJOR.MINOR.PATCH):
- MAJOR: Breaking changes
- MINOR: New features, backward compatible
- PATCH: Bug fixes, backward compatible

### Upgrade Notes

#### From 0.9.x to 1.0.x
- Database migrations will run automatically
- Review and update environment variables if needed
- Check path mappings for custom configurations

[1.0.3]: https://github.com/lingarr-translate/lingarr/releases/tag/1.0.3
[1.0.2]: https://github.com/lingarr-translate/lingarr/releases/tag/1.0.2
[1.0.1]: https://github.com/lingarr-translate/lingarr/releases/tag/1.0.1
[1.0.0]: https://github.com/lingarr-translate/lingarr/releases/tag/1.0.0
