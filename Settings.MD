## Settings
These settings can be configured as environment variables to persist configurations across reinstallations.

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

### **Translation Service Configuration**

#### **General Translation Settings**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `SERVICE_TYPE`           | The type of translation service to use. Supported values: `libretranslate`, `openai`, `anthropic`, `localai`, `deepl`, `gemini`, `deepseek`, `chutes`, `google`, `bing`, `microsoft`, `yandex`. |
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

#### **Anthropic**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `ANTHROPIC_MODEL`        | The model to use for Anthropic translations. Example: `claude-2`.               |
| `ANTHROPIC_API_KEY`      | The API key for authenticating with Anthropic.                                  |
| `ANTHROPIC_VERSION`      | The version of the Anthropic API to use.                                        |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |

#### **Gemini**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `GEMINI_MODEL`        | The model to use for Anthropic translations. Example: `gemini-2.0-flash`.        |
| `GEMINI_API_KEY`      | The API key for authenticating with Anthropic.                                  |
| `AI_PROMPT`            | The prompt template for AI-based translation services.                          |

#### **DeepSeek**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `DEEPSEEK_MODEL`        | The model to use for Anthropic translations. Example: `deepseek-chat`.               |
| `DEEPSEEK_API_KEY`      | The API key for authenticating with Anthropic.                                  |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |

#### **Chutes**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `CHUTES_MODEL`           | The model to use for Chutes translations. Example: `deepseek-ai/DeepSeek-V3.1-Terminus`. |
| `CHUTES_API_KEY`         | The API key for authenticating with Chutes. Get yours at https://chutes.ai/app/api |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |

#### **LocalAI**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `LOCAL_AI_MODEL`         | The model to use for LocalAI translations.                                      |
| `LOCAL_AI_API_KEY`       | The API key for authenticating with LocalAI.                                    |
| `LOCAL_AI_ENDPOINT`      | The endpoint URL for the LocalAI service. Example: `http://localhost:8080`.     |
| `AI_PROMPT`              | The prompt template for AI-based translation services.                          |

#### **DeepL**

| **Environment Variable** | **Description**                                                                 |
|--------------------------|---------------------------------------------------------------------------------|
| `DEEPL_API_KEY`          | The API key for authenticating with DeepL.                                      |

---

### **Translation Services**

The `SERVICE_TYPE` variable determines which translation service to use. Below are the supported values:

| **Value**       | **Description**                                                                 |
|-----------------|---------------------------------------------------------------------------------|
| `libretranslate`| Use the LibreTranslate service for translations.                                |
| `openai`        | Use OpenAI's models (e.g., GPT-4, GPT-3.5) for translations.                    |
| `anthropic`     | Use Anthropic's models (e.g., Claude-2) for translations.                       |
| `localai`       | Use a locally hosted AI model for translations.                                 |
| `deepl`         | Use the DeepL API for translations.                                             |
| `chutes`        | Use Chutes.AI OpenAI-compatible API for translations.                           |
| `google`        | Use Google Translate for translations.                                          |
| `bing`          | Use Bing Translate for translations.                                            |
| `microsoft`     | Use Microsoft Translator for translations.                                      |
| `yandex`        | Use Yandex Translate for translations.                                          |
