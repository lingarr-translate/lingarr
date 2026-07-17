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

## Environment variables
### OpenAI

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `OPENAI_MODEL` | The model to use for OpenAI translations. Example: `gpt-4`. |
| `OPENAI_API_KEY` | The API key for authenticating with OpenAI. |
| `AI_PROMPT` | The prompt template for AI-based translation services. |

### Anthropic

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `ANTHROPIC_MODEL` | The model to use for Anthropic translations. |
| `ANTHROPIC_API_KEY` | The API key for authenticating with Anthropic. |
| `ANTHROPIC_VERSION` | The version of the Anthropic API to use. |
| `AI_PROMPT` | The prompt template for AI-based translation services. |

### Gemini

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `GEMINI_MODEL` | The model to use for Gemini translations. Example: `gemini-2.0-flash`. |
| `GEMINI_API_KEY` | The API key for authenticating with Gemini. |
| `AI_PROMPT` | The prompt template for AI-based translation services. |

### DeepSeek

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `DEEPSEEK_MODEL` | The model to use for DeepSeek translations. Example: `deepseek-chat`. |
| `DEEPSEEK_API_KEY` | The API key for authenticating with DeepSeek. |
| `AI_PROMPT` | The prompt template for AI-based translation services. |

### LocalAI

LocalAI works with Ollama or any other OpenAI-compatible model or router.

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `LOCAL_AI_MODEL` | The model to use for LocalAI translations. |
| `LOCAL_AI_API_KEY` | The API key for authenticating with LocalAI. This is optional, and only needed if the deployment requires authentication. |
| `LOCAL_AI_ENDPOINT` | The full URL of the completion endpoint. Example: `http://ollama:11434/v1/chat/completions`. |
| `AI_PROMPT` | The prompt template for AI-based translation services. |
