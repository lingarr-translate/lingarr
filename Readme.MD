# Lingarr: Subtitle Translation Made Easy

Lingarr is an application that leverages translation technologies to automatically translate subtitle files to your desired target language. 
With support for both LibreTranslate, DeepL and AI Lingarr offers a flexible solution for all your subtitle translation needs.

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
- **[Chutes](https://chutes.ai/)**
- **[Google](https://translate.google.com/)** 
- **[Bing](https://www.bing.com/translator)**
- **[Yandex](https://translate.yandex.com/)**
- **[Azure](https://www.microsoft.com/en-us/translator/business/translator-api/)**

Choose the service that best fits your needs.

## Usage
To run Lingarr using Docker Compose, add the following configuration to your `docker-compose.yml` file,

**Note:** _As of this version, Lingarr uses a dynamic mapper, which may encounter issues with deeper media directories.   
If your mapping involves more than a single directory, please consult the [wiki](https://github.com/lingarr-translate/lingarr/wiki), create an [issue](https://github.com/lingarr-translate/lingarr/issues), or contact me on [Discord](https://discord.gg/HkubmH2rcR)_

## Setting up Lingarr
```yaml
services:
  lingarr:
    image: lingarr/lingarr:latest # Use latest-arm64 for the ARM build
    container_name: lingarr
    restart: unless-stopped
    environment:
      - ASPNETCORE_URLS=http://+:9876
    ports:
      - "9876:9876"
    volumes:
      - /path/to/media/movies:/movies
      - /path/to/media/tv:/tv
      - /path/to/config:/app/config
    networks:
      - lingarr

networks:
  lingarr:
    external: true
```

### Setting up Lingarr using Docker CLI
Follow the following steps to set up Lingarr and LibreTranslate via docker CLI

```bash
docker run -d \
  --name lingarr \
  --restart unless-stopped \
  -p 9876:9876 \
  -e ASPNETCORE_URLS=http://+:9876 \
  -v /path/to/media/movies:/movies \
  -v /path/to/media/tv:/tv \
  -v /path/to/config:/app/config \
  --network lingarr \
  lingarr/lingarr:latest
```

### Lingarr environment variables
These variables can be used

| **Environment Variable**                          | **Description**                                                                   |
|---------------------------------------------------|-----------------------------------------------------------------------------------|
| `ASPNETCORE_URLS=http://+:9876`                   | The internal port that Lingarr will listen on inside the container.               |
| `MAX_CONCURRENT_JOBS=1`                           | Sets the amount of jobs that can run concurrently, defaults to 1.                 |
| `DB_CONNECTION=mysql`                             | Specifies the database connection type. Options are `mysql` or `sqlite`.          |
| `DB_HOST=Lingarr.Mysql`                           | The hostname for the MySQL database (required when using `mysql`).                |
| `DB_PORT=3306`                                    | The port for the MySQL database (required when using `mysql`).                    |
| `DB_DATABASE=LingarrMysql`                        | The name of the database (required when using `mysql`).                           |
| `DB_USERNAME=LingarrMysql`                        | The username for the database (required when using `mysql`).                      |
| `DB_PASSWORD=LingarrMysql`                        | The password for the database (required when using `mysql`).                      |
| `DB_HANGFIRE_SQLITE_PATH=/app/config/Hangfire.db` | The path of the sqlite database file for Hangfire, when sqlite connection is used |

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

## API Integration
Lingarr provides a RESTful API that allows you to integrate subtitle translation capabilities into your applications. You can find the complete API documentation, including a Swagger definition of all available endpoints at the 
[Lingarr API Documentation](https://lingarr.com/docs/api/)

### 🤝 Contributing:
We welcome contributions to Lingarr! Whether it's bug reports, 
feature requests, or code contributions, please feel free to help out. Visit the [Lingarr](https://github.com/lingarr-translate/lingarr) GitHub repository to get started.

### 🙏 Credits:
Icons: [Lucide](https://lucide.dev/icons)  
Subtitle Parsing: [AlexPoint](https://github.com/AlexPoint/SubtitlesParser)    
Translation Services: [libretranslate](https://libretranslate.com)  
GTranslate: [GTranslate](https://github.com/d4n3436/GTranslate)

### 🙏 Special thanks:
For supporting open source:  
[selfh.st by Ethan](https://selfh.st/?ref=lingarr)  
[r/selfhosted](https://www.reddit.com/r/selfhosted/)

For helping to squash a big bug:  
[FrankieBBBB](https://github.com/FrankieBBBB)  
