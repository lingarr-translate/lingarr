# Lingarr: Subtitle Translation Made Easy

Lingarr is an application that leverages translation technologies to automatically translate subtitle files to your desired target language. 
With support for both LibreTranslate and DeepL, Lingarr provides a flexible solution for subtitle localization.

### 🌟 Multiple Translation Service Support
Lingarr now offers two services for automated translation:

- **LibreTranslate:** An open-source, self-hosted solution for those who prioritize privacy and customization.
- **DeepL:** A leading AI-powered **freemium** translation service known for its high accuracy and natural-sounding translations.

Choose the engine that best fits your needs, or use both for comparison and optimal results!

## Usage

### Docker Compose

LibreTranslate doesn't natively support Docker Compose and needs to be built locally. Follow [these](https://docs.portainer.io/user/docker/images/build) docs on how to build an image in Portainer using the below Dockerfile. There are also options to make use of CUDA; more info [here](https://github.com/LibreTranslate/LibreTranslate).

Change `languages` and set `filter` to true to only include preferred languages. If `filter` is set to false, all languages will be installed.

```dockerfile
FROM libretranslate/libretranslate:latest AS models_cache

ARG filter=true
# replace with preferred languages
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
      - PUID=0
      - PGID=0
      - MAX_CONCURRENT_JOBS=1 # Optional
      - DB_CONNECTION=mysql # Optional, defaults to `sqlite`
      - DB_HOST=Lingarr.Mysql # Optional, required when using MySQL
      - DB_PORT=3306 # Optional, required when using MySQL
      - DB_DATABASE=LingarrMysql # Optional, required when using MySQL
      - DB_USERNAME=LingarrMysql # Optional, required when using MySQL
      - DB_PASSWORD=Secret1234 # Optional, required when using MySQL
    ports:
      - "9876:8080"
    volumes:
      - /path/to/movies:/app/media/movies
      - /path/to/tv:/app/media/tv
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
      - LT_DISABLE_WEB_UI=true # Optional, enable if you would like to make use of the LibreTranslate web interface 
    networks:
      - lingarr
    healthcheck:
      test: ['CMD-SHELL', './venv/bin/python scripts/healthcheck.py']
```

### Docker CLI

Follow the following steps to set up Lingarr and LibreTranslate via docker CLI

#### Setting up Lingarr
```bash
docker run -d \
  --name lingarr \
  --restart unless-stopped \
  -e PUID=0 \
  -e PGID=0 \
  -e MAX_CONCURRENT_JOBS=1 \      # Optional
  -e DB_CONNECTION=mysql \        # Optional: defaults to 'sqlite' if not specified
  -e DB_HOST=Lingarr.Mysql \      # Optional: required when using MySQL
  -e DB_PORT=3306 \               # Optional: required when using MySQL
  -e DB_DATABASE=LingarrMysql \   # Optional: required when using MySQL
  -e DB_USERNAME=LingarrMysql \   # Optional: required when using MySQL
  -e DB_PASSWORD=Secret1234 \     # Optional: required when using MySQL
  -p 9876:8080 \
  -v /path/to/movies:/app/media/movies \
  -v /path/to/tv:/app/media/tv \
  -v /path/to/config:/app/config \
  --network lingarr \
  lingarr/lingarr:latest
```

#### Setting up LibreTranslate
Create necessary directories and set permissions:
```bash
mkdir -p /apps/libretranslate/{local,db}
chmod -R 777 /apps/libretranslate
```
Run LibreTranslate Docker container:
```bash
docker run -d \
  --name LibreTranslate \
  -p 5000:5000 \
  -v /apps/libretranslate/db:/app/db \
  -v /apps/libretranslate/local:/home/libretranslate/.local \
  libretranslate/libretranslate \
  --disable-web-ui \
  --load-only=pt,en,es     # replace with preferred languages
```

#### Parameters
| Parameter                   | Function                                                                    |
|-----------------------------|-----------------------------------------------------------------------------|
| -p 9876:8080                | Maps port 9876 on the host to port 8080 for the Lingarr web interface       |
| -e PUID=0                   | Sets the user ID for the container process                                  |
| -e PGID=0                   | Sets the group ID for the container process                                 |
| -e MAX_CONCURRENT_JOBS=1    | Sets the amount of jobs that can run concurrently, defaults to 1            |
| -e DB_CONNECTION=mysql      | Specifies the database connection type. Options are `mysql` or `sqlite`     |
| -e DB_HOST=Lingarr.Mysql    | The hostname for the MySQL database (required when using `mysql`)           |
| -e DB_PORT=3306             | The port for the MySQL database (required when using `mysql`)               |
| -e DB_DATABASE=LingarrMysql | The name of the database (required when using `mysql`)                      |
| -e DB_USERNAME=LingarrMysql | The username for the database (required when using `mysql`)                 |
| -e DB_USERNAME=LingarrMysql | The password for the database (required when using `mysql`)                 |
| -v /app/config              | Lingarr configuration directory. The SQLite database is stored here if used |
| -v /app/movies              | Location of Movie library on disk                                           |
| -v /app/tv                  | Location of TV Shows library on disk                                        |

#### 🤝 Contributing:
We welcome contributions to Lingarr! Whether it's bug reports, 
feature requests, or code contributions, please feel free to help out. Visit the [Lingarr](https://github.com/lingarr-translate/lingarr) GitHub repository to get started.

#### 🙏 Credits:
Icons: [Lucide](https://lucide.dev/icons)  
Subtitle Parsing: [AlexPoint](https://github.com/AlexPoint/SubtitlesParser)  
Translation Services: [libretranslate](https://libretranslate.com)
