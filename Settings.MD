## Settings
These settings can be configured as environment variables to persist configurations across reinstallations.

### **Docker secrets**

Any of the environment variables documented below can also be supplied by appending `_FILE` to the variable name and pointing it at a file containing the value.  
When both `<VAR>` and `<VAR>_FILE` are set, `<VAR>_FILE` will be selected primarily.

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

---

### **General Configuration**

| **Environment Variable**   | **Description**                                                                                                                               |
|----------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| `BASE_PATH`                | Optional URL prefix to host Lingarr under a sub-path (e.g., behind a reverse proxy). Example: `/lingarr`. Leave unset to serve from the root. |
| `MAX_CONCURRENT_JOBS`      | Maximum number of Hangfire worker threads processing jobs in parallel. Defaults to `1` when unset.                                            |
| `SQLITE_DB_PATH`           | Path to the SQLite database file used when `DB_CONNECTION` is `sqlite`. Defaults to `local.db` within `/app/config/`.                         |
| `DB_HANGFIRE_SQLITE_PATH`  | Path to the SQLite database file used by the Hangfire job store. Defaults to `/app/config/Hangfire.db`.                                       |

---

### **Radarr Configuration**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `RADARR_URL`             | The base URL for the Radarr API. Example: `http://localhost:7878`.              |
| `RADARR_API_KEY`         | The API key for authenticating with Radarr. Obtain this from your Radarr instance. |

---

### **Sonarr Configuration**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `SONARR_URL`             | The base URL for the Sonarr API. Example: `http://localhost:8989`.              |
| `SONARR_API_KEY`         | The API key for authenticating with Sonarr. Obtain this from your Sonarr instance. |

---

### **Authentication Configuration**

| **Environment Variable** | **Description**                                                                  |
|--------------------------|----------------------------------------------------------------------------------|
| `AUTH_ENABLED`           | Enables or disables authentication.                                              |
| `ENCRYPTION_KEYS`        | The path for storing encryption keys, stored by default in the config directory. |

---

### **Telemetry Configuration**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `TELEMETRY_ENABLED`      | Opt-in for anonymized telemetry to help other Lingarr users, defaults to false. |

---

### **Translation Service Configuration**

#### **General Translation Settings**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `SERVICE_TYPE`           | An ordered JSON array of translation services. The first entry is the primary; the rest are fallbacks tried in order when the primary fails. A single value (e.g. `openai`) is still accepted and is normalised to a one-element array on startup. |
| `SOURCE_LANGUAGES`       | A minified JSON array of source languages for translation. |
| `TARGET_LANGUAGES`       | A minified JSON array of target languages for translation. |

The `SOURCE_LANGUAGES` and `TARGET_LANGUAGES` variables should be provided as a minified JSON array. Each object in the array should contain the `name` and `code` of the language.

#### Example:
```json
[{"name":"English","code":"en"},{"name":"Dutch","code":"nl"}]
```

### Service type

#### **LibreTranslate**

| **Environment Variable**     | **Description**                                                                 |
|------------------------------|---------------------------------------------------------------------------------|
| `LIBRE_TRANSLATE_URL`        | The base URL for the LibreTranslate API. Example: `https://libretranslate.com`. |
| `LIBRE_TRANSLATE_API_KEY`    | The API key to use for the LibreTranslate API. This is optional, and only needed if using an instance that requires one. |

#### **OpenAI**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `OPENAI_MODEL`           | The model to use for OpenAI translations. Example: `gpt-4`, `gpt-3.5-turbo`.    |
| `OPENAI_API_KEY`         | The API key for authenticating with OpenAI.                                     |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |
| `AI_USER_PROMPT`         | The user message template. Supports `{lineToTranslate}`, `{contextBefore}` and `{contextAfter}`. |

#### **Anthropic**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `ANTHROPIC_MODEL`        | The model to use for Anthropic translations. Example: `claude-opus-5`.          |
| `ANTHROPIC_API_KEY`      | The API key for authenticating with Anthropic.                                  |
| `ANTHROPIC_VERSION`      | The version of the Anthropic API to use.                                        |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |
| `AI_USER_PROMPT`         | The user message template. Supports `{lineToTranslate}`, `{contextBefore}` and `{contextAfter}`. |

#### **Gemini**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `GEMINI_MODEL`           | The model to use for Gemini translations. Example: `gemini-2.0-flash`.          |
| `GEMINI_API_KEY`         | The API key for authenticating with Gemini.                                     |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |
| `AI_USER_PROMPT`         | The user message template. Supports `{lineToTranslate}`, `{contextBefore}` and `{contextAfter}`. |

#### **DeepSeek**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `DEEPSEEK_MODEL`         | The model to use for DeepSeek translations. Example: `deepseek-chat`.           |
| `DEEPSEEK_API_KEY`       | The API key for authenticating with DeepSeek.                                   |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |
| `AI_USER_PROMPT`         | The user message template. Supports `{lineToTranslate}`, `{contextBefore}` and `{contextAfter}`. |

#### **LocalAI**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `LOCAL_AI_MODEL`         | The model to use for LocalAI translations.                                      |
| `LOCAL_AI_API_KEY`       | The API key for authenticating with LocalAI.                                    |
| `LOCAL_AI_ENDPOINT`      | The full URL of the completion endpoint. Example: `http://ollama:11434/v1/chat/completions`. |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |
| `AI_USER_PROMPT`         | The user message template. Supports `{lineToTranslate}`, `{contextBefore}` and `{contextAfter}`. |

#### **Proofreading**

Proofreading is only available for the AI services above (OpenAI, Anthropic, Gemini, DeepSeek, LocalAI), and only for a `service_type` entry that supports it. It uses two more settings, shared across all AI services the same way `AI_PROMPT` and `AI_USER_PROMPT` are. These are configured through **Settings > Services > Request Settings** in the web interface, or through their environment variables:

| **Setting** | **Environment Variable** | **Default** |
|-------------|--------------------------|-------------|
| `proofread_prompt` | `PROOFREAD_PROMPT` | `You are proofreading a subtitle translated from {sourceLanguage} to {targetLanguage}. Compare the translation against the source and correct mistranslations, wrong names, grammar and punctuation, preserving the tone and meaning without censoring the content. Keep the length close to the original so it still fits on screen. If the translation is already correct, repeat it unchanged. Provide only the corrected translation as output, with no additional comments.` |
| `proofread_user_prompt` | `PROOFREAD_USER_PROMPT` | `[SOURCE] {sourceLine}\n[TRANSLATION] {translatedLine}` |

See the AI services documentation for the full placeholder list.

#### **DeepL**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `DEEPL_API_KEY`          | The API key for authenticating with DeepL.                                      |

---

### **Translation Services**

The `SERVICE_TYPE` variable determines which translation services to use. Provide a JSON array, 
the first entry is the primary service and any remaining entries are fallbacks, tried in order when the primary fails. 
A single value is also accepted and is rewritten to an array on startup.

#### Example:
```json
["openai","anthropic","libretranslate"]
```

The supported values are:

| **Value**       | **Description**                                                                 |
|-----------------|---------------------------------------------------------------------------------|
| `libretranslate`| Use the LibreTranslate service for translations.                                |
| `openai`        | Use OpenAI's models (e.g., GPT-4, GPT-3.5) for translations.                    |
| `anthropic`     | Use Anthropic's models (e.g., Claude-2) for translations.                       |
| `gemini`        | Use Google's Gemini models for translations.                                    |
| `deepseek`      | Use DeepSeek's models for translations.                                         |
| `localai`       | Use a locally hosted or OpenAI-compatible AI model for translations.            |
| `deepl`         | Use the DeepL API for translations.                                             |
| `google`        | Use Google Translate for translations.                                          |
| `bing`          | Use Bing Translate for translations.                                            |
| `microsoft`     | Use Microsoft Translator for translations.                                      |
| `yandex`        | Use Yandex Translate for translations.                                          |
