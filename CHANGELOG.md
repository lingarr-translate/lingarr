# Changelog

All notable changes to Lingarr will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2024-11-29

### Fixed
- Improved Sonarr integration resilience
- Enhanced DeepL translation service error handling
- Fixed path mapping issues with deeply nested directories

### Changed
- Updated frontend dependencies (Vue 3.5.12, Vite 6.4.1, Tailwind CSS 4.0)
- Improved error logging and debugging capabilities
- Enhanced translation retry logic with exponential backoff

## [1.0.2] - 2024-11

### Added
- Multi-language UI support with i18n
- Enhanced statistics dashboard
- Daily translation statistics tracking

### Fixed
- Subtitle formatting issues
- Translation batch processing improvements

## [1.0.1] - 2024-10

### Added
- Support for additional translation services (Gemini, DeepSeek)
- Improved schedule management with cron expressions
- Path mapping configuration UI

### Fixed
- Docker volume mounting issues
- Subtitle overlap detection and correction

## [1.0.0] - 2024-10

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
