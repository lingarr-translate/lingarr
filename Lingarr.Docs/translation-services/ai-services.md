# AI services

How to set up an AI translation service in Lingarr.

All AI services follow the same pattern: select the service, provide an API key, and choose a model.

## How to set up

1. In the Lingarr web interface, go to **Settings > Services**.
2. Select your AI service in the primary service dropdown, or click **Add fallback service** to use it as a fallback.
3. Click the gear icon on the service row to configure its credentials.
4. Enter the API key and choose a model. Self-hosted services such as [LocalAI](#localai) also ask for an endpoint.

    When a service asks for an endpoint, enter the full URL that Lingarr sends requests to, including the path. A base URL such as `http://localhost:8080` will not work.
    
    The path also determines which protocol is used:
    
    - An endpoint ending in `completions` uses the OpenAI-compatible chat protocol. 
   Example: `http://ollama:11434/v1/chat/completions`.
    - Any other endpoint uses the generate protocol.   
   Example: `http://ollama:11434/api/generate`.

5. Optionally configure a prompt, a default one is automatically set if left empty. Customization is available by clicking the `Open Request Settings` button below the configuration.
6. Set your source and target languages at the bottom of the page.

Every setting can also be provided as an environment variable, listed per service below. See [Configuration](/getting-started/configuration) for more details.

## Prompts

Each request is built from two templates, and both apply to every AI service.

`AI_PROMPT` is the system prompt. It carries the standing instruction on how to translate, and is sent as the system message. Lingarr seeds a default on first run. Clearing it sends an empty system message, leaving the service without translation instructions, so keep a value set.

`AI_USER_PROMPT` is the user message. It carries the line being translated, and is sent as the user message. The default is `{lineToTranslate}`, the subtitle line on its own. When it is left empty, the line is sent unchanged.

Both templates accept the same placeholders:

| **Placeholder** | **Value** |
|-----------------|-----------|
| `{lineToTranslate}` | The subtitle line being translated. |
| `{contextBefore}` | The lines preceding it, as many as the context setting allows. Empty when that setting is `0`. |
| `{contextAfter}` | The lines following it, as many as the context setting allows. Empty when that setting is `0`. |
| `{sourceLanguage}` | The language being translated from. |
| `{targetLanguage}` | The language being translated to. |
| `{model}` | The configured model. |

Use the user prompt to frame a single line, for example to surround it with its context so the service can see where the line sits.

Batch translation does not use the user prompt. The batch is sent as the user message instead, so only `AI_PROMPT` applies when batching is enabled.

## Proofreading

Proofreading re-examines a completed translation. For each subtitle line it sends the source line and the existing translation together to the AI service and asks for a corrected translation, without translating from scratch.

Only services that implement proofreading offer it: OpenAI, Anthropic, Gemini, DeepSeek, Mistral, xAI and LocalAI. LibreTranslate, DeepL, Google, Bing, Microsoft and Yandex do not support proofreading.

You can run it two ways:

- **Whole request.** On the translations list, a `Completed` request with a translated subtitle shows a Proofread action beside Retry, Resume and Remove. This queues a job that proofreads every line and rewrites the translated subtitle file in place once it finishes. Lingarr keeps no copy of the pre-proofread text, in the file or in the database, so there is nothing to revert to afterwards.
- **Single line.** On the translation detail page, each line has its own Proofread button. It fetches a suggested correction for that line, shows it inline, and you apply or dismiss it.

A whole-request proofread calls the AI service once per subtitle line, the same as a non-batch translation, batching does not apply. Proofreading a long subtitle track costs roughly as much as translating it again.

The subtitle file is rewritten only after every line has been checked, so cancelling a proofread, or restarting Lingarr while one is running, leaves the existing translation and its file exactly as they were. The request returns to `Completed` and can be proofread again.

Proofreading uses two more templates, `proofread_prompt` (the system prompt) and `proofread_user_prompt` (the user message), configured on the same Request Settings panel as `AI_PROMPT` and `AI_USER_PROMPT`, or through the `PROOFREAD_PROMPT` and `PROOFREAD_USER_PROMPT` environment variables.

Both accept the placeholders already listed above, plus two more:

| **Placeholder** | **Value** |
|-----------------|-----------|
| `{sourceLine}` | The original subtitle line. |
| `{translatedLine}` | The existing translation being checked. |

`{lineToTranslate}`, `{contextBefore}` and `{contextAfter}` are sent as empty strings during a proofread call, so a request template that already contains them still renders correctly.

## Environment variables
### OpenAI

| **Environment Variable** | **Description**                                             |
|--------------------------|-------------------------------------------------------------|
| `OPENAI_MODEL` | The model to use for OpenAI translations. Example: `gpt-4`. |
| `OPENAI_API_KEY` | The API key for authenticating with OpenAI.                 |
| `AI_PROMPT` | The system prompt template.                                 |
| `AI_USER_PROMPT` | The user message template.                                  |

### Anthropic

| **Environment Variable** | **Description**                                |
|--------------------------|------------------------------------------------|
| `ANTHROPIC_MODEL` | The model to use for Anthropic translations.   |
| `ANTHROPIC_API_KEY` | The API key for authenticating with Anthropic. |
| `ANTHROPIC_VERSION` | The version of the Anthropic API to use.       |
| `AI_PROMPT` | The system prompt template.                    |
| `AI_USER_PROMPT` | The user message template.                     |

### Gemini

| **Environment Variable** | **Description**                                                        |
|--------------------------|------------------------------------------------------------------------|
| `GEMINI_MODEL` | The model to use for Gemini translations. Example: `gemini-2.0-flash`. |
| `GEMINI_API_KEY` | The API key for authenticating with Gemini.                            |
| `AI_PROMPT` | The system prompt template.                                            |
| `AI_USER_PROMPT` | The user message template.                                             |

### DeepSeek

| **Environment Variable** | **Description**                                                       |
|--------------------------|-----------------------------------------------------------------------|
| `DEEPSEEK_MODEL` | The model to use for DeepSeek translations. Example: `deepseek-chat`. |
| `DEEPSEEK_API_KEY` | The API key for authenticating with DeepSeek.                         |
| `AI_PROMPT` | The system prompt template.                                           |
| `AI_USER_PROMPT` | The user message template.                                            |

### Mistral

| **Environment Variable** | **Description**                                                             |
|--------------------------|-----------------------------------------------------------------------------|
| `MISTRAL_MODEL` | The model to use for Mistral translations. Example: `mistral-large-latest`. |
| `MISTRAL_API_KEY` | The API key for authenticating with Mistral.                                |
| `AI_PROMPT` | The system prompt template.                                                 |
| `AI_USER_PROMPT` | The user message template.                                                  |

### xAI

| **Environment Variable** | **Description**                                          |
|--------------------------|----------------------------------------------------------|
| `XAI_MODEL` | The model to use for xAI translations. Example: `grok-4.5`. |
| `XAI_API_KEY` | The API key for authenticating with xAI.                 |
| `AI_PROMPT` | The system prompt template.                              |
| `AI_USER_PROMPT` | The user message template.                               |

### LocalAI

LocalAI works with Ollama or any other OpenAI-compatible model or router.

| **Environment Variable** | **Description**                                                                                                           |
|--------------------------|---------------------------------------------------------------------------------------------------------------------------|
| `LOCAL_AI_MODEL` | The model to use for LocalAI translations.                                                                                |
| `LOCAL_AI_API_KEY` | The API key for authenticating with LocalAI. This is optional, and only needed if the deployment requires authentication. |
| `LOCAL_AI_ENDPOINT` | The full URL of the completion endpoint. Example: `http://ollama:11434/v1/chat/completions`.                              |
| `AI_PROMPT` | The system prompt template.                                                                                               |
| `AI_USER_PROMPT` | The user message template.                                                                                                |
