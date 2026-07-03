# Lingarr Cloudflare Workers AI plugin

This sample plugin lets Lingarr translate subtitles using **[Cloudflare Workers AI](https://developers.cloudflare.com/workers-ai/)**.
It uses translation models such as `@cf/meta/m2m100-1.2b` and serves as an **example** for anyone who wants to create their own translation service plugin.

## Main parts of the plugin

- `CloudflareTranslator`
The actual translator. It implements `ITranslationService`, calls Cloudflare’s AI endpoint, and handles errors similarly like Lingarr.
- `CloudflarePluginManifest` 
Tells Lingarr what settings to show in the UI (Account ID, API Token, Model).
- `AssemblyInfo.cs`
Contains the required version declaration so Lingarr can load the plugin.

## Prerequisites

- A free Cloudflare account
- An API token with **Workers AI: Read** permission, 
  (Create it at: *My Profile > API Tokens > Create Token > Workers AI (Read)*)
- Your Cloudflare Account ID (visible in the dashboard URL: `dash.cloudflare.com/{account_id}/...`)

> **Note**: The free tier gives you 10,000 so called "neurons" per day, enough for light subtitle translation. Check the [pricing page](https://developers.cloudflare.com/workers-ai/platform/pricing/) for current limits.

## How to Build

```bash
dotnet build samples/CloudflarePlugin/CloudflarePlugin.csproj -c Release
```
This creates `Lingarr.Plugin.Cloudflare.dll`. You must also copy `Lingarr.Contracts.dll`, both files are needed.

## Deploy

Lingarr looks for plugins in the folder defined by the `PLUGINS_PATH` environment variable

Docker-compose example:

```yaml
services:
  lingarr:
    image: lingarr/lingarr:latest
    environment:
      PLUGINS_PATH: /app/plugins
    volumes:
      - ./plugins:/app/plugins
```
1. Copy both DLL files into your `./plugins` folder.
2. Restart Lingarr.
3. Look for a log message like:
`Loaded plugin /app/plugins/Lingarr.Plugin.Cloudflare.dll (1 manifest(s))`

Once loaded, the plugin should appear in `Settings > Plugins`.

## Configuration

Go to the service settings, select `Cloudflare Workers AI` and fill in:

| Field | Stored as         | Notes |
|-------|-------------------|-------|
| Account ID | Plain text        | Found in your dashboard URL. |
| API token | Encrypted setting | Needs only "Workers AI: Read" permission |
| Translation model | Plain text        | Defaults to `@cf/meta/m2m100-1.2b` |

## Supported languages

Cloudflare doesn't expose a "supported languages" endpoint for the translation models so in this case the plugin doesn't provide supported languages.
The plugin accepts standard language codes (`en`, `nl`, `ja`, etc.). If a language pair is not supported, you will see a `TranslationException` during translation.

## Creating your own Plugin

1. Create a .NET 10 class library.
2. Reference `Lingarr.Contracts`
3. Implement `ITranslationService` on a class marked with `[PluginProvider("your_identifier")]`.
4. Create one `IPluginManifest` 
5. Add `[assembly: LingarrPluginApiVersion(1, 0)]` to the assembly.
6. Build and place the DLLs in the plugins folder.

## Reserved provider identifiers

Do not use these plugin identifiers (they are used by built-in providers):

`anthropic`, `openai`, `gemini`, `deepseek`, `localai`, `deepl`, `libretranslate`, `google`, `bing`, `microsoft`, `yandex`.

## Security

Plugins run with full permissions inside Lingarr. There is no sandbox. Only use DLLs you trust.