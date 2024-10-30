# Lingarr: Subtitle Translation Made Easy

Lingarr is an application that leverages translation technologies to automatically translate subtitle files to your desired target language. 
With support for both LibreTranslate and DeepL, Lingarr provides a flexible solution for subtitle translation.

https://github.com/user-attachments/assets/be753d77-ca53-4867-adea-363145613d2b

### 🌟 Multiple Translation Service Support
Lingarr now offers multiple services for automated translation:

- **[LibreTranslate](https://libretranslate.com)** 
- **[DeepL](https://www.deepl.com/)**
- **[Anthropic](https://www.anthropic.com/)**
- **[OpenAI](https://openai.com/)**
- **[Local AI](https://localai.io/)** 
- **[Google](https://translate.google.com/)** 
- **[Bing](https://www.bing.com/translator)**
- **[Yandex](https://translate.yandex.com/)**
- **[Azure](https://www.microsoft.com/en-us/translator/business/translator-api/)**

Choose the engine that best fits your needs.

## Usage

### Docker Compose
To run Lingarr using Docker Compose, add the following configuration to your `docker-compose.yml` file,

**Note:** _As of this version, Lingarr uses a dynamic mapper, which may encounter issues with deeper media directories.   
If your mapping involves more than a single directory, please consult the [wiki](https://github.com/lingarr-translate/lingarr/wiki), create an [issue](https://github.com/lingarr-translate/lingarr/issues), or contact me on [Discord](https://discord.gg/MR8epCKt)_

#### Setting up Lingarr
```yaml
services:
  lingarr:
    image: lingarr/lingarr:latest
    container_name: lingarr
    restart: unless-stopped
    ports:
      - "9876:8080"
    volumes:
      - /path/to/media/movies:/app/media/movies
      - /path/to/media/tv:/app/media/tv
      - /path/to/config:/app/config
    networks:
      - lingarr

networks:
  lingarr:
    external: true
```

#### Setting up LibreTranslate
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

### Docker CLI

Follow the following steps to set up Lingarr and LibreTranslate via docker CLI

#### Setting up Lingarr
```bash
docker run -d \
  --name lingarr \
  --restart unless-stopped \
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
  -v /path/to/libretranslate/db:/app/db \
  -v /path/to/libretranslate/local:/home/libretranslate/.local \
  libretranslate/libretranslate \
  --disable-web-ui \
  --load-only=en,nl     # Important, replace with your preferred languages
```

#### Lingarr parameters
| Parameter                   | Function                                                                                      |
|-----------------------------|-----------------------------------------------------------------------------------------------|
| -p 9876:8080                | Maps port 9876 on the host to port 8080 for the Lingarr web interface                         |
| -e PUID=1000                | Sets the user ID for the container process                                                    |
| -e PGID=1000                | Sets the group ID for the container process                                                   |
| -e MAX_CONCURRENT_JOBS=1    | Sets the amount of jobs that can run concurrently, defaults to 1                              |
| -e DB_CONNECTION=mysql      | Specifies the database connection type. Options are `mysql` or `sqlite`                       |
| -e DB_HOST=Lingarr.Mysql    | The hostname for the MySQL database (required when using `mysql`)                             |
| -e DB_PORT=3306             | The port for the MySQL database (required when using `mysql`)                                 |
| -e DB_DATABASE=LingarrMysql | The name of the database (required when using `mysql`)                                        |
| -e DB_USERNAME=LingarrMysql | The username for the database (required when using `mysql`)                                   |
| -e DB_USERNAME=LingarrMysql | The password for the database (required when using `mysql`)                                   |
| -v /app/config              | Lingarr configuration directory. The SQLite database is stored here if used                   |
| -v /app/movies              | Location of Movie library on disk                                                             |
| -v /app/tv                  | Location of TV Shows library on disk                                                          |

#### LibreTranslate parameters
| Parameter                   | Function                                                                                      |
|-----------------------------|-----------------------------------------------------------------------------------------------|
| LT_LOAD_ONLY                | Allows you to add source languages by their [iso code](https://libretranslate.com/languages)  |
| LT_DISABLE_WEB_UI           | Enables or disables a Google translate like web ui                                            |

#### API Integration
Lingarr provides a RESTful API that allows you to integrate subtitle translation capabilities into your applications. You can find the complete API documentation, including a Swagger definition of all available endpoints at the 
[Lingarr API Documentation](https://lingarr.com/docs/api/)

#### 🤝 Contributing:
We welcome contributions to Lingarr! Whether it's bug reports, 
feature requests, or code contributions, please feel free to help out. Visit the [Lingarr](https://github.com/lingarr-translate/lingarr) GitHub repository to get started.

#### 🙏 Credits:
Icons: [Lucide](https://lucide.dev/icons)  
Subtitle Parsing: [AlexPoint](https://github.com/AlexPoint/SubtitlesParser)    
Translation Services: [libretranslate](https://libretranslate.com)  
GTranslate: [GTranslate](https://github.com/d4n3436/GTranslate)  
For helping to squash a big bug:  
[HomerSimpson](https://github.com/HomerSimpson)  
