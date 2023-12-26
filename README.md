# Lingarr

Lingarr is an application that utilizes LibreTranslate to translate subtitle files to a user-specified target language, providing a solution for subtitle localization.

## Usage

### Docker Compose

LibreTranslate doesn't natively support Docker Compose and needs to be built locally. Follow [these](https://docs.portainer.io/user/docker/images/build) docs on how to build an image in Portainer using the below Dockerfile. There are also options to make use of CUDA; more info [here](https://github.com/LibreTranslate/LibreTranslate).

Change `languages` and set `filter` to true to only include preferred languages. If `filter` is set to false, all languages will be installed.

```dockerfile
FROM libretranslate/libretranslate:latest AS models_cache

ARG filter=false
ARG languages="nl,en,tr"

USER libretranslate

WORKDIR /app

RUN if [ "$filter" = "true" ]; then \
        ./venv/bin/python ../app/scripts/install_models.py --load_only_lang_codes "$languages"; \
    else \
        ./venv/bin/python ../app/scripts/install_models.py; \
    fi

RUN ./venv/bin/pip install . && ./venv/bin/pip cache purge

FROM models_cache AS final
ENTRYPOINT [ "./venv/bin/libretranslate", "--host", "0.0.0.0" ]
```

Then, to run Lingarr using Docker Compose, add the following configuration to your `docker-compose.yml` file:

```yaml
networks:
  lingarr:
    external: true

version: '3'
services:
  lingarr:
    image: lingarr/lingarr:latest
    container_name: lingarr
    restart: unless-stopped
    environment:
      - LIBRETRANSLATE_API=http://libretranslate:5000
    ports:
      - "9876:9876"
    volumes:
      - /path/to/movies:/app/media/movies
      - /path/to/tvshows:/app/media/tvshows
      - /path/to/config:/app/config
    networks:
      - lingarr

  libretranslate:
    image: libretranslate:latest
    container_name: libretranslate
    restart: unless-stopped
    ports:
      - "5000:5000"
    environment:
      - LT_DISABLE_WEB_UI=true #optional, enable if you would like to make use of the libretranslate ui 
    networks:
      - lingarr
    healthcheck:
      test: ['CMD-SHELL', './venv/bin/python scripts/healthcheck.py']
```

#### Parameters
|Parameter   |Function   |
|---|---|
|-p 9876   |The port for the Lingarr webinterface   |
|LIBRETRANSLATE_API   |The API address for LibreTranslate   |
-v /config   |Lingarr config
-v /movies   |Location of Movie library on disk
-v /tvshows   |Location of TV Shows library on disk

Adjust the volume paths and other parameters according to your specific setup. 
If desired, add a `languages.json` file with the following content to limit the language selection within the UI.

```json
[
    {
        "name": "Dutch",
        "code": "nl"
    },
    {
        "name": "English",
        "code": "en"
    },
    {
        "name": "Turkish",
        "code": "tr"
    }
]
```

Feel free to contribute to the development of Lingarr or report any issues on the [Lingarr](https://github.com/lingarr-translate/lingarr) GitHub repository.
