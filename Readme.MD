# Lingarr: Subtitle Translation Made Easy

[![Version](https://img.shields.io/github/v/release/lingarr-translate/lingarr)](https://github.com/lingarr-translate/lingarr/releases)
[![Docker Pulls](https://img.shields.io/docker/pulls/lingarr/lingarr.svg)](https://hub.docker.com/r/lingarr/lingarr)
[![License](https://img.shields.io/badge/license-AGPL--3.0-green.svg)](LICENSE)
[![Discord](https://img.shields.io/discord/1293119073739210885?label=discord&logo=discord)](https://discord.gg/HkubmH2rcR)

Lingarr is an application that leverages translation technologies to automatically translate subtitle files to your desired target language. 
With support for multiple translation services including LibreTranslate, DeepL, and various AI providers, Lingarr offers a flexible solution for all your subtitle translation needs.

https://github.com/user-attachments/assets/be753d77-ca53-4867-adea-363145613d2b

### 🌟 Multiple Translation Service Support
Lingarr now offers multiple services for automated translation:

- **[LibreTranslate](https://libretranslate.com)**
- **Local AI (with Ollama, or any other OpenAPI compatible model/router)**
- **[DeepL](https://www.deepl.com/)**  (without pt-BR)
- **[Anthropic](https://www.anthropic.com/)**
- **[OpenAI](https://openai.com/)**
- **[DeepSeek](https://deepseek.com)**
- **[Gemini](https://gemini.google.com/)**
- **[Google](https://translate.google.com/)** 
- **[Bing](https://www.bing.com/translator)**
- **[Yandex](https://translate.yandex.com/)**
- **[Azure](https://www.microsoft.com/en-us/translator/business/translator-api/)**

Choose the service that best fits your needs.

## Usage

### Docker Image Tags
Lingarr provides multi-architecture Docker images that automatically select the correct platform:

| Tag | Description | Architectures |
|-----|-------------|---------------|
| `latest` | Latest stable release | `amd64` `arm64` |
| `1.2.3` | Specific version | `amd64` `arm64` |
| `main` | ⚠️ Development build from main branch | `amd64` `arm64` |

**Note:** As of 1.0.3 all images support both AMD64 (Intel/AMD) and ARM64 (Raspberry Pi, Apple Silicon) architectures. Docker will automatically pull the correct architecture for your system.

### Available Registries

Lingarr Docker images are available from multiple registries:

| Registry | Image |
|----------|-------|
| GitHub Container Registry | `ghcr.io/lingarr-translate/lingarr:latest` |
| Docker Hub | `docker.io/lingarr/lingarr:latest` |

## Setting up Lingarr

By default, Lingarr uses `MySQL` however, `PostgreSQL` and `SQLite` are also supported. Note that Lingarr starts quickly, if migrations fail, using a `healthcheck` combined with 
`depends_on` is recommended.

```yaml
services:
  lingarr:
    image: ghcr.io/lingarr-translate/lingarr:latest # or lingarr/lingarr:latest
    container_name: lingarr
    restart: unless-stopped
    environment:
      - ASPNETCORE_URLS=http://+:9876
      - DB_CONNECTION=mysql
      - DB_HOST=lingarr-db
      - DB_PORT=3306
      - DB_DATABASE=lingarr
      - DB_USERNAME=lingarr
      - DB_PASSWORD=your-password # provide a password
    ports:
      - "9876:9876"
    volumes:
      - /path/to/media/movies:/movies
      - /path/to/media/tv:/tv
      - /path/to/config:/app/config
    networks:
      - lingarr
    depends_on:
      lingarr-db:
        condition: service_healthy

  lingarr-db:
    image: mariadb:latest
    container_name: lingarr-db
    restart: unless-stopped
    environment:
      - MYSQL_DATABASE=lingarr
      - MYSQL_USER=lingarr
      - MYSQL_PASSWORD=your-password # provide a password
      - MYSQL_ROOT_PASSWORD=your-password # provide a root password
    volumes:
      - /path/to/db:/var/lib/mysql # define a volume to persist the database
    networks:
      - lingarr
    healthcheck:
      test: "mariadb $$MYSQL_DATABASE -u$$MYSQL_USER -p$$MYSQL_PASSWORD -e 'SELECT 1;'"
      interval: 10s
      timeout: 5s
      retries: 5

networks:
  lingarr:
    external: true
```

### Setting up Lingarr using Docker CLI
Follow the following steps to set up Lingarr via Docker CLI

```bash
# Create the network
docker network create lingarr

# Start MariaDB
docker run -d \
  --name lingarr-db \
  --restart unless-stopped \
  -e MYSQL_DATABASE=lingarr \
  -e MYSQL_USER=lingarr \
  -e MYSQL_PASSWORD=your-password \
  -e MYSQL_ROOT_PASSWORD=your-root-password \
  -v /path/to/db:/var/lib/mysql \
  --network lingarr \
  mariadb:latest

# Start Lingarr
docker run -d \
  --name lingarr \
  --restart unless-stopped \
  -p 9876:9876 \
  -e ASPNETCORE_URLS=http://+:9876 \
  -e DB_CONNECTION=mysql \
  -e DB_HOST=lingarr-db \
  -e DB_PORT=3306 \
  -e DB_DATABASE=lingarr \
  -e DB_USERNAME=lingarr \
  -e DB_PASSWORD=your-password \ # provide a password
  -v /path/to/media/movies:/movies \
  -v /path/to/media/tv:/tv \
  -v /path/to/config:/app/config \
  --network lingarr \
  ghcr.io/lingarr-translate/lingarr:latest # or use: lingarr/lingarr:latest
```

### Lingarr environment variables
These variables can be used

| **Environment Variable**                          | **Description**                                                                         |
|---------------------------------------------------|-----------------------------------------------------------------------------------------|
| `ASPNETCORE_URLS=http://+:9876`                   | The internal port that Lingarr will listen on inside the container.                     |
| `MAX_CONCURRENT_JOBS=1`                           | Sets the amount of jobs that can run concurrently, defaults to 1.                       |
| `DB_CONNECTION=mysql`                             | Specifies the database connection type. Options are `mysql`, `postgresql` or `sqlite`.  |
| `DB_HOST=Lingarr.Mysql`                           | The hostname for the MySQL database (required when using `mysql` or `postgresql`).      |
| `DB_PORT=3306`                                    | The port for the MySQL database (required when using `mysql` or `postgresql`).          |
| `DB_DATABASE=Lingarr`                             | The name of the database (required when using `mysql` or `postgresql`).                 |
| `DB_USERNAME=Lingarr`                             | The username for the database (required when using `mysql` or `postgresql`).            |
| `DB_PASSWORD=Secret1234`                          | The password for the database (required when using `mysql` or `postgresql`).            |
| `DB_HANGFIRE_SQLITE_PATH=/app/config/Hangfire.db` | The path of the sqlite database file for Hangfire, when sqlite connection is used       |

Additional [settings](Settings.MD) can be found here that can be set as environment variables to persist settings for each reinstall

## Setting up LibreTranslate
This step is optional if you are using a translation service other than LibreTranslate.

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

### Setting up LibreTranslate using Docker CLI

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
  -v /path/to/libretranslate/db:/app/db \[Settings.MD](Settings.MD)
  -v /path/to/libretranslate/local:/home/libretranslate/.local \
  libretranslate/libretranslate \
  --disable-web-ui \
  --load-only=en,nl     # Important, replace with your preferred languages
```

### LibreTranslate environment variables
| Parameter                      | Function                                                                        |
|--------------------------------|---------------------------------------------------------------------------------|
| `LT_LOAD_ONLY`                  | Allows you to add source languages by their [iso code](https://libretranslate.com/languages)                    |
| `LT_DISABLE_WEB_UI`              | Enables or disables a Google translate like web ui                            |

### Running Lingarr as non-root
When running Docker containers, you can optionally specify which user should run the container process using the `user` flag. This is useful for:
- **Security**: Running containers as non-root users
- **Permissions**: Ensuring files created by the container have the correct ownership on your host system

**Docker Compose syntax:**
```yaml
user: "1000:1000"  # UID:GID
```

**Docker CLI syntax:**
```bash
--user 1000:1000  # UID:GID
```

## API Integration
Lingarr provides a RESTful API that allows you to integrate subtitle translation capabilities into your applications. You can find the complete API documentation, including a Swagger definition of all available endpoints at the 
[Lingarr API Documentation](https://lingarr.com/docs/api/)

### 🤝 Contributing:
We welcome contributions to Lingarr! Whether it's bug reports,
feature requests, or code contributions, please feel free to help out.

Please read our [Contributing Guidelines](CONTRIBUTING.md) for development setup, coding standards, and requirements for submit pull requests.

### 🙏 Credits:
Icons: [Lucide](https://lucide.dev/icons)  
Subtitle Parsing: [AlexPoint](https://github.com/AlexPoint/SubtitlesParser)    
Translation Services: [libretranslate](https://libretranslate.com)  
GTranslate: [GTranslate](https://github.com/d4n3436/GTranslate)

### 🙏 Special thanks:
For supporting open source:  
[selfh.st by Ethan](https://selfh.st/?ref=lingarr)  
[r/selfhosted](https://www.reddit.com/r/selfhosted/)  
[FrankieBBBB](https://github.com/FrankieBBBB)  
