# Lingarr plugin API

Lingarr can be extended with your own translation services (like LLM providers or other services) without changing the main Lingarr code.
You create a plugin as a .NET 10 class library (a DLL file) that references the `Lingarr.Contracts` package. 

## What you can extend

| Interface | Used for                                                                                                     |
|-----------|--------------------------------------------------------------------------------------------------------------|
| `ITranslationService` | Any translation service (AI, API-based, or rule-based). This is the main interface for adding a new service. |
| `IPluginManifest` | Describes your provider's settings so they appear nicely in the Lingarr UI.                                                      |

A single plugin can include one or both of these.

## Optional capabilities

A provider class can also implement interfaces from `Lingarr.Contracts.Translation` to opt into extra features. These are purely additive: a plugin that does not implement them keeps working exactly as it does today, no change is required.

### `IProofreadService`

Implement this to let your provider proofread an existing translation, comparing it against its source line and returning a corrected version:

```csharp
public interface IProofreadService
{
    Task<string> ProofreadAsync(
        string sourceText,
        string translatedText,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken);
}
```

Return `translatedText` unchanged when nothing needs correcting. Lingarr only shows the Proofread action, in the UI and through the API, for providers whose class implements this interface. Providers that don't implement it are unaffected and are simply left out of proofreading.

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

`anthropic`, `openai`, `gemini`, `deepseek`, `mistral`, `xai`, `localai`, `deepl`, `libretranslate`, `google`, `bing`, `microsoft`, `yandex`

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
- When the `ai_context_use_translated` setting is on, the `contextLinesBefore` / `contextLinesAfter` arguments of `TranslateAsync` contain JSON objects (`position`, `line`, and `translation` for lines already translated) instead of plain lines; a plugin that does not need the structure can pass them through as text.

## Reference plugin
Check the `samples/CloudflarePlugin/` folder for a complete working example. It demonstrates:

- Plugin manifest
- Provider attribute
- API version
- Settings handling
- HTTP calls
- Error handling

## Build with GitHub Actions

GitHub Actions can build your plugin DLL for you. Users then download it from your releases page and do not need the .NET SDK.

Lingarr already loads `Lingarr.Contracts.dll`, so ship only your plugin DLL. Reference `Lingarr.Contracts.dll` from a path in your `.csproj`, and set `Private` to `false` so the build does not copy it to the output:

```xml
<ItemGroup>
    <Reference Include="Lingarr.Contracts">
        <HintPath>$(LingarrContractsPath)</HintPath>
        <Private>false</Private>
    </Reference>
</ItemGroup>
```

Add this file to your repository as `.github/workflows/build.yml`. Replace `MyPlugin.csproj` and `Lingarr.Plugin.MyPlugin.dll` with your own names, and set `ref` to the Lingarr release you build against:

```yaml
name: Build plugin

on:
  push:
    tags: [ 'v*' ]
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest
    permissions:
      contents: write

    steps:
    - uses: actions/checkout@v6
      with:
        path: plugin

    - name: Check out Lingarr
      uses: actions/checkout@v6
      with:
        repository: lingarr-translate/lingarr
        ref: 1.4.0
        path: lingarr

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'

    - name: Build Lingarr.Contracts
      run: dotnet build lingarr/Lingarr.Contracts/Lingarr.Contracts.csproj --configuration Release --output contracts

    - name: Build plugin
      run: dotnet build plugin/MyPlugin.csproj --configuration Release --output out -p:LingarrContractsPath=${{ github.workspace }}/contracts/Lingarr.Contracts.dll

    - name: Upload artifact
      uses: actions/upload-artifact@v4
      with:
        name: plugin
        path: out/Lingarr.Plugin.MyPlugin.dll

    - name: Create release
      if: startsWith(github.ref, 'refs/tags/')
      env:
        GH_TOKEN: ${{ github.token }}
      run: gh release create "$GITHUB_REF_NAME" out/Lingarr.Plugin.MyPlugin.dll --repo "$GITHUB_REPOSITORY" --generate-notes
```

Push a tag such as `v1.0.0` to build the DLL and attach it to a new release. To build without a release, start the workflow from the **Actions** tab and download the DLL from the run's artifacts.

## Listing your plugin
To list your plugin for users, see [Community plugins](/translation-services/community-plugins#adding-your-plugin).