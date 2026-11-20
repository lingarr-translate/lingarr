# Configuration

Lingarr is primarily configured through its web interface. However, every setting can also be provided as an environment variable.

## General

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `ASPNETCORE_URLS=http://+:9876` | The internal port that Lingarr will listen on inside the container. |
| `BASE_PATH` | Optional URL prefix to host Lingarr under a sub-path (e.g., behind a reverse proxy). Example: `/lingarr`. Leave unset to serve from the root. |
| `MAX_CONCURRENT_JOBS=1` | Sets the amount of jobs that can run concurrently, defaults to 1. |
| `JOB_TIMEOUT_MINUTES=30` | Minutes of silence before Hangfire retries a job. `sqlite` only, defaults to `30`. |
| `FFMPEG_PATH=/app/config/ffmpeg` | Folder for `ffmpeg` and `ffprobe`, used by [subtitle extraction](#subtitle-extraction). Defaults to `/app/config/ffmpeg`. |
| `TZ=Europe/Amsterdam` | The time zone Lingarr runs in, defaults to UTC. It applies to schedules, log timestamps and every date shown in the web interface, so set it to your own zone. A schedule of `0 3 * * *` runs at 3am in this zone. |

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

## Subtitle extraction

With **Extract embedded subtitles** enabled under Settings, Subtitle, Lingarr downloads ffmpeg into `FFMPEG_PATH`, about 220 MB on disk.
Automation and webhooks then extract the embedded text subtitles of a video next to it before translating. Streams without a language and image subtitles such as PGS are skipped.

Without internet access, copy `ffmpeg` and `ffprobe` into `FFMPEG_PATH` yourself.

## Telemetry

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `TELEMETRY_ENABLED` | Opt-in for anonymized telemetry to help other Lingarr users, defaults to false. |

When enabled, Lingarr submits a report at most once every 7 days, Each report also carries an installation identifier.
This is a random value generated once on first submission and stored locally as a setting and doesn't persist on reinstallations.

## Translation services

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `SERVICE_TYPE` | An ordered JSON array of providers that defines the translation services. The first entry is the primary; the rest are fallbacks tried in order when the primary fails. A single value (e.g. `openai`) is also accepted. See [SERVICE_TYPE and service instances](#service-type-and-service-instances). |
| `SOURCE_LANGUAGES` | A minified JSON array of source languages for translation. |
| `TARGET_LANGUAGES` | A minified JSON array of target languages for translation. |
| `USE_BATCH_TRANSLATION` | Set to `true` to translate subtitles in batches with AI services, defaults to false. |

The `SOURCE_LANGUAGES` and `TARGET_LANGUAGES` variables should be provided as a minified JSON array. Each object in the array should contain the `name` and [ISO 639](https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes) or [ISO 639-2](https://en.wikipedia.org/wiki/ISO_639-2) language codes:

```json
[{"name":"English","code":"en"},{"name":"Dutch","code":"nl"}]
```

The order of the array is the priority order, see [Source and target languages](#source-and-target-languages).

`SERVICE_TYPE` example, with OpenAI as the primary service and Anthropic and LibreTranslate as fallbacks:

```json
["openai","anthropic","libretranslate"]
```

The supported values are:

- **[AI services](/translation-services/ai-services)**: [`openai`](/translation-services/ai-services#openai), [`anthropic`](/translation-services/ai-services#anthropic), [`gemini`](/translation-services/ai-services#gemini), [`deepseek`](/translation-services/ai-services#deepseek), [`mistral`](/translation-services/ai-services#mistral), [`xai`](/translation-services/ai-services#xai) and [`localai`](/translation-services/ai-services#localai)
- **[Machine translation](/translation-services/machine-translation)**: [`libretranslate`](/translation-services/machine-translation#libretranslate), [`deepl`](/translation-services/machine-translation#deepl), [`google`](/translation-services/machine-translation#google-bing-microsoft-and-yandex), [`bing`](/translation-services/machine-translation#google-bing-microsoft-and-yandex), [`microsoft`](/translation-services/machine-translation#google-bing-microsoft-and-yandex) and [`yandex`](/translation-services/machine-translation#google-bing-microsoft-and-yandex)

Each service has its own configuration variables, such as API keys and model selection, documented on its settings page.

### SERVICE_TYPE and service instances

Each configured translation service is a row in the database with its own name and settings, and the list of services in position order is the cascade. A provider can appear more than once, see [AI services](/translation-services/ai-services#more-than-one-service-of-a-provider).

When `SERVICE_TYPE` is set, Lingarr rebuilds the cascade from it at every start:

- The first time a provider appears in the list maps to the oldest service of that provider, the second time to the second oldest, and so on.
- A service that does not exist yet is created with the default settings of its provider.
- A service that is not listed is deleted together with its settings. Lingarr logs a warning for each one.
- The order of the list becomes the order of the cascade.
- If the value is not valid JSON, is an empty array, or contains no known provider, Lingarr keeps the existing services and logs a warning.

Without `SERVICE_TYPE`, the services you configured in the web interface are kept.

#### Provider variables

These variables write to a service, not to a global setting:

| **Provider** | **Environment Variables** |
|--------------|---------------------------|
| LibreTranslate | `LIBRE_TRANSLATE_URL`, `LIBRE_TRANSLATE_API_KEY` |
| OpenAI | `OPENAI_MODEL`, `OPENAI_API_KEY` |
| Anthropic | `ANTHROPIC_MODEL`, `ANTHROPIC_API_KEY`, `ANTHROPIC_VERSION` |
| LocalAI | `LOCAL_AI_MODEL`, `LOCAL_AI_API_KEY`, `LOCAL_AI_ENDPOINT`, `LOCAL_AI_STRUCTURED_OUTPUT` |
| Gemini | `GEMINI_MODEL`, `GEMINI_API_KEY` |
| DeepSeek | `DEEPSEEK_MODEL`, `DEEPSEEK_API_KEY` |
| Mistral | `MISTRAL_MODEL`, `MISTRAL_API_KEY` |
| xAI | `XAI_MODEL`, `XAI_API_KEY` |
| DeepL | `DEEPL_API_KEY` |

A variable without a number targets the oldest service of its provider. Add `_N`, with N from 2, to target the N-th oldest service of that provider. `OPENAI_API_KEY_2` is the API key of the second OpenAI service. The `_FILE` form works for both, as `OPENAI_API_KEY_FILE` and `OPENAI_API_KEY_2_FILE`. API keys are stored encrypted.

When the service does not exist, Lingarr creates it at the end of the cascade, so it becomes a fallback. Lingarr only creates the next number: `OPENAI_API_KEY_3` with only one OpenAI service is skipped with a warning. If `SERVICE_TYPE` is set, Lingarr does not create it and logs a warning instead. Put the provider in `SERVICE_TYPE` as many times as you need services.

Example, two OpenAI services with different models and Anthropic as a fallback:

```yaml
environment:
  SERVICE_TYPE: '["openai","openai","anthropic"]'
  OPENAI_MODEL: gpt-4
  OPENAI_API_KEY_FILE: /run/secrets/openai_api_key
  OPENAI_MODEL_2: gpt-4-turbo
  OPENAI_API_KEY_2_FILE: /run/secrets/openai_api_key_2
  ANTHROPIC_API_KEY_FILE: /run/secrets/anthropic_api_key
```

Like every setting variable, these are applied at every start and win over the web interface. Google, Bing, Microsoft, Yandex and third party plugins have no provider variables.

Batch translation is used only when at least one service in the cascade supports it (OpenAI, Anthropic, LocalAI, Gemini, Mistral or xAI).

#### Upgrading

The `service_type` setting no longer exists. When you upgrade, each provider in your current list becomes a service with its existing settings, in the same order. Settings of providers that were not in the list are removed. Set the matching environment variables to create them again.

## Source and target languages

Both language lists are ordered. In the web interface, drag a language to change its position.
With `SOURCE_LANGUAGES` and `TARGET_LANGUAGES` as environment variable, the order of the JSON array is the order.

### Source priority

Lingarr uses the first source language that has a subtitle.

### Target languages

By default, every missing target language is translated.

With **Only translate to the first target language** enabled, an existing subtitle in any target language counts as translated. 
If none exists, Lingarr translates to the first target language.

For example, with `es-mx` then `es`:

- `Movie.es.srt` or `Movie.es-mx.srt` exists, nothing is translated.
- Neither exists, `Movie.es-mx.srt` is created.
