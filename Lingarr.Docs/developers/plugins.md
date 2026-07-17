# Lingarr plugin API

Lingarr can be extended with your own translation services (like LLM providers or other services) without changing the main Lingarr code.
You create a plugin as a .NET 10 class library (a DLL file) that references the `Lingarr.Contracts` package. 

## What you can extend

| Interface | Used for                                                                                                     |
|-----------|--------------------------------------------------------------------------------------------------------------|
| `ITranslationService` | Any translation service (AI, API-based, or rule-based). This is the main interface for adding a new service. |
| `IPluginManifest` | Describes your provider's settings so they appear nicely in the Lingarr UI.                                                      |

A single plugin can include one or both of these.

## Required setup

Every provider class must be marked with:

```csharp
[PluginProvider("your-identifier")]
```
The identifier is used internally and is what users select in the Services settings page.
Every plugin must also declare the plugin API version:
```csharp
[assembly: LingarrPluginApiVersion(1, 0)]
```
This version is based on `Lingarr.Contracts`, not the main Lingarr version.

## How plugins are loaded

Lingarr scans a folder specified by the `PLUGINS_PATH` environment variable at startup.

- If the env var `PLUGINS_PATH` is not set, plugins are disabled.
- If the folder doesn’t exist, Lingarr logs a warning and continues.
- Failed or incompatible plugins are skipped (they shouldn't crash the application).

## Reserved identifiers

Do not use these plugin identifiers (they are used by built-in providers):

`anthropic`, `openai`, `gemini`, `deepseek`, `localai`, `deepl`, `libretranslate`, `google`, `bing`, `microsoft`, `yandex`

## Settings

- Use snake_case keys with a namespace (example: `myprovider_api_key`).
- Settings defined in your manifest are automatically added when the plugin loads.
- PluginSettingType.Secret fields are encrypted.
- To read settings, inject `Lingarr.Contracts.Settings.ISettingsAccess` in your constructor.
- Plugins can only read settings, they cannot write them.

## HTTP and retry
Get Lingarr's shared HTTP settings from:
```csharp
var httpSettings = await settingsAccess.GetHttpSettingsAsync();
```
This includes timeout, retry count, delay, etc. Use these values so your plugin behaves consistently with built-in providers.
See the sample plugin for a full example of HTTP handling and retries.

## Important Rules

- Only depend on `Lingarr.Contracts`, this is the only stable API.
- Plugins share the same process. Use dependency versions compatible with Lingarr to avoid conflicts.
- Plugins run with full permissions (no sandbox). Only add DLLs you trust.

## Reference plugin
Check the `samples/CloudflarePlugin/` folder for a complete working example. It demonstrates:

- Plugin manifest
- Provider attribute
- API version
- Settings handling
- HTTP calls
- Error handling