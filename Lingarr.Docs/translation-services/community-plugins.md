# Community plugins

Community plugins add translation services that are not built into Lingarr. They are made and maintained by their authors, not by the Lingarr project.

::: warning
Community plugins are not reviewed by the Lingarr project. A plugin runs inside Lingarr with full permissions, including access to your settings and API keys. Only install plugins from authors you trust.
:::

## Installing a plugin

1. Set the `PLUGINS_PATH` environment variable and mount a folder on that path:

```yaml
services:
  lingarr:
    image: lingarr/lingarr:latest
    environment:
      PLUGINS_PATH: /app/plugins
    volumes:
      - ./plugins:/app/plugins
```

2. Download the plugin from its repository and copy its DLL files into the `./plugins` folder. Lingarr does not look in subfolders.
3. Restart Lingarr. The log shows a line like `Loaded plugin /app/plugins/Lingarr.Plugin.Example.dll (1 manifest(s))`.
4. Select the plugin under **Settings > Services** and fill in its settings.

A plugin that fails to load or is built for another plugin API version is skipped, and the log shows why.

## Available plugins

| Plugin | Provider id | Description | Author |
|--------|-------------|-------------|--------|
| [Cloudflare Workers AI](https://github.com/lingarr-translate/lingarr/tree/main/samples/CloudflarePlugin) | `cloudflare` | Translates with Cloudflare Workers AI models. The reference plugin, build it from source. | Lingarr |

## Adding your plugin

Open a pull request that adds a row to the table above. Your plugin must:

- Have a public repository with install instructions and a list of its settings.
- Use a provider id that is not [reserved](/developers/plugins#reserved-identifiers).
- State which Lingarr plugin API version it is built for.

Also add the `lingarr-plugin` topic to your repository, so it shows up in a [GitHub topic search](https://github.com/topics/lingarr-plugin).

See the [plugin API](/developers/plugins) to build a plugin.
