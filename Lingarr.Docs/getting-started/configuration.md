# Configuration

Lingarr is primarily configured through its web interface. However, every setting can also be provided as an environment variable.

## General

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `ASPNETCORE_URLS=http://+:9876` | The internal port that Lingarr will listen on inside the container. |
| `BASE_PATH` | Optional URL prefix to host Lingarr under a sub-path (e.g., behind a reverse proxy). Example: `/lingarr`. Leave unset to serve from the root. |
| `MAX_CONCURRENT_JOBS=1` | Sets the amount of jobs that can run concurrently, defaults to 1. |

## Database

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `DB_CONNECTION=mysql` | Specifies the database connection type. Options are `mysql`, `postgresql` or `sqlite`. |
| `DB_HOST=Lingarr.Mysql` | The hostname for the database (required when using `mysql` or `postgresql`). |
| `DB_PORT=3306` | The port for the database (required when using `mysql` or `postgresql`). |
| `DB_DATABASE=Lingarr` | The name of the database (required when using `mysql` or `postgresql`). |
| `DB_USERNAME=Lingarr` | The username for the database (required when using `mysql` or `postgresql`). |
| `DB_PASSWORD=Secret1234` | The password for the database (required when using `mysql` or `postgresql`). |
| `SQLITE_DB_PATH` | Path to the SQLite database file used when `DB_CONNECTION` is `sqlite`. Defaults to `local.db` within `/app/config/`. |
| `DB_HANGFIRE_SQLITE_PATH=/app/config/Hangfire.db` | Path to the SQLite database file used by the Hangfire job store. Defaults to `/app/config/Hangfire.db`. |

## Docker secrets

Any environment variable can also be supplied by appending `_FILE` to the variable name and pointing it at a file containing the value. When both `<VAR>` and `<VAR>_FILE` are set, `<VAR>_FILE` will be selected primarily.

Example:

```yaml
services:
  lingarr:
    image: lingarr/lingarr:latest
    environment:
      OPENAI_API_KEY_FILE: /run/secrets/openai_api_key
    secrets:
      - openai_api_key

secrets:
  openai_api_key:
    file: ./secrets/openai_api_key.txt
```

# Optional settings

The following settings are all **optional** and can be provided via environment variables.

## Radarr and Sonarr

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `RADARR_URL` | The base URL for the Radarr API. Example: `http://localhost:7878`. |
| `RADARR_API_KEY` | The API key for authenticating with Radarr. Obtain this from your Radarr instance. |
| `SONARR_URL` | The base URL for the Sonarr API. Example: `http://localhost:8989`. |
| `SONARR_API_KEY` | The API key for authenticating with Sonarr. Obtain this from your Sonarr instance. |

## Authentication

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `AUTH_ENABLED` | Enables or disables authentication. |
| `ENCRYPTION_KEYS` | The path for storing encryption keys, stored by default in the config directory. |

## Telemetry

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `TELEMETRY_ENABLED` | Opt-in for anonymized telemetry to help other Lingarr users, defaults to false. |

## Translation services

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `SERVICE_TYPE` | An ordered JSON array of translation services. The first entry is the primary; the rest are fallbacks tried in order when the primary fails. A single value (e.g. `openai`) is still accepted and is normalised to a one-element array on startup. |
| `SOURCE_LANGUAGES` | A minified JSON array of source languages for translation. |
| `TARGET_LANGUAGES` | A minified JSON array of target languages for translation. |

The `SOURCE_LANGUAGES` and `TARGET_LANGUAGES` variables should be provided as a minified JSON array. Each object in the array should contain the `name` and [ISO 639](https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes) or [ISO 639-2](https://en.wikipedia.org/wiki/ISO_639-2) language codes:

```json
[{"name":"English","code":"en"},{"name":"Dutch","code":"nl"}]
```

`SERVICE_TYPE` example, with OpenAI as the primary service and Anthropic and LibreTranslate as fallbacks:

```json
["openai","anthropic","libretranslate"]
```

The supported values are:

- **[AI services](/translation-services/ai-services)**: [`openai`](/translation-services/ai-services#openai), [`anthropic`](/translation-services/ai-services#anthropic), [`gemini`](/translation-services/ai-services#gemini), [`deepseek`](/translation-services/ai-services#deepseek) and [`localai`](/translation-services/ai-services#localai)
- **[Machine translation](/translation-services/machine-translation)**: [`libretranslate`](/translation-services/machine-translation#libretranslate), [`deepl`](/translation-services/machine-translation#deepl), [`google`](/translation-services/machine-translation#google-bing-microsoft-and-yandex), [`bing`](/translation-services/machine-translation#google-bing-microsoft-and-yandex), [`microsoft`](/translation-services/machine-translation#google-bing-microsoft-and-yandex) and [`yandex`](/translation-services/machine-translation#google-bing-microsoft-and-yandex)

Each service has its own configuration variables, such as API keys and model selection, documented on its settings page.
