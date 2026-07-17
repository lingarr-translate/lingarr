# Machine translation

How to set up a machine translation service in Lingarr.

## LibreTranslate

LibreTranslate is a free, self-hosted translation service. Run it alongside Lingarr with Docker Compose:

```yaml
  LibreTranslate:
    container_name: LibreTranslate
    image: libretranslate/libretranslate:latest
    restart: unless-stopped
    environment:
      - LT_LOAD_ONLY=en,nl  # Important, replace with your preferred languages
    ports:
      - 5000:5000
    volumes:
      - /path/to/config:/home/libretranslate/.local/share/argos-translate
    networks:
      - lingarr
    healthcheck:
      test: ["CMD-SHELL", "./venv/bin/python scripts/healthcheck.py"]
```

Or with the Docker CLI. Create the necessary directories and set permissions:

```bash
mkdir -p /apps/libretranslate/{local,db}
chmod -R 777 /apps/libretranslate
```

Run the LibreTranslate Docker container:

```bash
docker run -d \
  --name LibreTranslate \
  -p 5000:5000 \
  -v /path/to/libretranslate/db:/app/db \
  -v /path/to/libretranslate/local:/home/libretranslate/.local \
  libretranslate/libretranslate \
  --disable-web-ui \
  --load-only=en,nl     # Important, replace with your preferred languages
```

LibreTranslate container variables:

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `LT_LOAD_ONLY` | Allows you to add source languages by their [iso code](https://libretranslate.com/languages). |
| `LT_DISABLE_WEB_UI` | Enables or disables a Google translate like web ui. |

Note that LibreTranslate's configuration can change over time. Refer to the [LibreTranslate project](https://github.com/LibreTranslate/LibreTranslate/) for the current options.

Lingarr variables for LibreTranslate:

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `LIBRE_TRANSLATE_URL` | The base URL for the LibreTranslate API. Example: `https://libretranslate.com`. |
| `LIBRE_TRANSLATE_API_KEY` | The API key to use for the LibreTranslate API. This is optional, and only needed if using an instance that requires one. |

## DeepL

| **Environment Variable** | **Description** |
|--------------------------|-----------------|
| `DEEPL_API_KEY` | The API key for authenticating with DeepL. |

Note: DeepL does not support pt-BR.

## Google, Bing, Microsoft and Yandex

These services require no API key or further configuration. Set `SERVICE_TYPE` to `google`, `bing`, `microsoft` or `yandex` and they are ready to use.
