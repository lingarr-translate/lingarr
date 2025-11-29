# Lingarr Documentation Index

Welcome to the Lingarr documentation! This guide will help you find the information you need.

## 📚 Table of Contents

### Getting Started
- [README.md](../Readme.MD) - Project overview, installation, and quick start
- [Settings.MD](../Settings.MD) - Complete environment variables and configuration reference
- [CHANGELOG.md](../CHANGELOG.md) - Version history and release notes

### Contributing
- [CONTRIBUTING.md](../CONTRIBUTING.md) - How to contribute to the project
- [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md) - Community guidelines
- [SECURITY.md](../SECURITY.md) - Security policy and vulnerability reporting
- [.github/PULL_REQUEST_TEMPLATE.md](PULL_REQUEST_TEMPLATE.md) - Pull request template

### Issue Templates
- [Bug Report](ISSUE_TEMPLATE/bug_report.yml) - Report bugs and issues
- [Feature Request](ISSUE_TEMPLATE/feature_request.yml) - Suggest new features
- [Issue Configuration](ISSUE_TEMPLATE/config.yml) - Links to discussions

### Development
- [copilot-instructions.md](copilot-instructions.md) - Comprehensive guide for AI-assisted development
- [CI Workflow](workflows/ci.yml) - Continuous Integration configuration
- [Docker Build Workflow](workflows/docker-build.yml) - Docker image build automation

### External Resources
- [API Documentation](https://lingarr.com/docs/api/) - Complete API reference
- [GitHub Wiki](https://github.com/lingarr-translate/lingarr/wiki) - Additional guides and tutorials
- [Discord Community](https://discord.gg/HkubmH2rcR) - Get help and discuss features
- [Docker Hub](https://hub.docker.com/r/lingarr/lingarr) - Official Docker images

## 🚀 Quick Links

### For Users
1. **Installation**: See [README.md](../Readme.MD#setting-up-lingarr)
2. **Configuration**: See [Settings.MD](../Settings.MD)
3. **Troubleshooting**: Visit [GitHub Issues](https://github.com/lingarr-translate/lingarr/issues) or [Discord](https://discord.gg/HkubmH2rcR)

### For Developers
1. **Development Setup**: See [CONTRIBUTING.md](../CONTRIBUTING.md#development-setup)
2. **Project Structure**: See [copilot-instructions.md](copilot-instructions.md#project-structure)
3. **Creating PRs**: See [CONTRIBUTING.md](../CONTRIBUTING.md#creating-pull-requests)
4. **Coding Guidelines**: See [CONTRIBUTING.md](../CONTRIBUTING.md#development-guidelines)

### For Contributors
1. **First Contribution**: Start with [CONTRIBUTING.md](../CONTRIBUTING.md)
2. **Code of Conduct**: Read [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md)
3. **Finding Issues**: Look for [good first issue](https://github.com/lingarr-translate/lingarr/labels/good%20first%20issue) label
4. **Development Environment**: Follow [docker-compose.dev.yml](../docker-compose.dev.yml) setup

## 📖 Documentation by Topic

### Architecture
- **Backend**: ASP.NET Core 9.0, Entity Framework Core, Hangfire, SignalR
- **Frontend**: Vue.js 3, TypeScript, Tailwind CSS 4, Vite 6
- **Database**: SQLite (default) or MySQL/MariaDB
- **Deployment**: Docker, Docker Compose

See [copilot-instructions.md](copilot-instructions.md#architecture) for details.

### Translation Services
Supported translation providers:
- LibreTranslate (self-hosted)
- DeepL
- OpenAI (GPT models)
- Anthropic (Claude)
- Google Translate
- Bing Translator
- Yandex Translate
- Azure Translator
- Gemini (Google AI)
- DeepSeek
- Local AI (Ollama/OpenAPI-compatible)

See [README.md](../Readme.MD#multiple-translation-service-support) and [Settings.MD](../Settings.MD) for configuration.

### Integration
- **Radarr**: Movie library management and automation
- **Sonarr**: TV show library management and automation

See [Settings.MD](../Settings.MD#radarr-configuration) for setup instructions.

### Development Workflow
1. Fork and clone the repository
2. Create a feature branch
3. Make changes following coding guidelines
4. Write/update tests
5. Submit a pull request
6. Await review and address feedback

See [CONTRIBUTING.md](../CONTRIBUTING.md) for detailed workflow.

## 🔧 Configuration Examples

### Docker Compose
```yaml
services:
  lingarr:
    image: lingarr/lingarr:latest
    container_name: lingarr
    environment:
      - ASPNETCORE_URLS=http://+:9876
      - DB_CONNECTION=sqlite
      - RADARR_URL=http://radarr:7878
      - RADARR_API_KEY=your_api_key_here
    ports:
      - "9876:9876"
    volumes:
      - /path/to/movies:/movies
      - /path/to/tv:/tv
      - /path/to/config:/app/config
```

See [README.md](../Readme.MD#setting-up-lingarr) for complete examples.

### Environment Variables
See [Settings.MD](../Settings.MD) for comprehensive list of all configuration options.

## 🤝 Community

- **GitHub Discussions**: [Ask questions and share ideas](https://github.com/lingarr-translate/lingarr/discussions)
- **Discord**: [Real-time chat and support](https://discord.gg/HkubmH2rcR)
- **Issues**: [Report bugs and request features](https://github.com/lingarr-translate/lingarr/issues)

## 📝 License

Lingarr is licensed under the GNU Affero General Public License v3.0. See [LICENSE](../LICENSE) for details.

---

**Need help?** Check our [Discord server](https://discord.gg/HkubmH2rcR) or [open an issue](https://github.com/lingarr-translate/lingarr/issues/new/choose)!

Last Updated: November 29, 2025
