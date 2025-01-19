# Contributing to Lingarr

Thank you for your interest in contributing to Lingarr!   
This document provides guidelines and instructions for contributing to the project.

## Development Setup

### Prerequisites
- .NET SDK (for build scripts)
- Docker (for running the application)
- PowerShell (for build scripts)

### Getting Started
1. Fork the repository on GitHub
2. Clone your fork locally:
```bash
git clone https://github.com/[USERNAME]/lingarr.git
cd lingarr
```

3. Add the upstream repository as a remote:
```bash
git remote add upstream https://github.com/lingarr-translate/lingarr.git
```

4. Create a new branch for your feature or bugfix following [Conventional Commits](https://www.conventionalcommits.org/):
```bash
git checkout -b feat/your-feature-name
# or
git checkout -b fix/your-bugfix-name
```

## Project Structure

The project is organized into several key components:

- `Lingarr.Server/`: Backend application
- `Lingarr.Core/`: Core domain models and interfaces
- `Lingarr.Client/`: Vue.js frontend application
- `Lingarr.Migrations.SQLite/`: SQLite database migrations
- `Lingarr.Migrations.MySQL/`: MySQL database migrations

## Building and Testing

### Development
Navigate to the root directory and start up the project:
```bash
docker-compose -f .\docker-compose.dev.yml up -d
```
Configure and sync with Sonarr and Radarr to create test data.   
The frontend supports hot reload while the backend needs to be rebuilt each time a change has been made.
### Services:

| Service    | URL                                           |
|------------|-----------------------------------------------|
| Lingarr    | http://localhost:9876                         |
| Swagger    | http://localhost:9877/swagger/index.html      |
| Hangfire   | http://localhost:9877/hangfire                |
| phpmyadmin | http://localhost:9878                         |
| sonarr     | http://localhost:8989                         |
| radarr     | http://localhost:7878                         |


### Docker Build
To build and push the Docker image (if logged into a Docker registry):
```powershell
./build-and-push.ps1 -Tag dev
```

## Creating Pull Requests

1. Select an issue to work on and comment to avoid duplicate effort 
2. Ensure your code follows the project's coding standards 
3. Update documentation as needed 
4. Write clear commit messages following [Conventional Commits](https://www.conventionalcommits.org/) 
5. Push your changes to your fork 
6. Create a Pull Request with a clear title and description 
7. Wait for review and address any feedback

## Development Guidelines
Lingarr welcomes suggestions for improving these standards. Please provide feedback through issues or discussions.

### Backend Guidelines
- Follow C# coding conventions
- Use async/await for asynchronous operations
- Implement proper error handling
- Document public APIs using XML comments
- Use dependency injection where appropriate

### Frontend Guidelines
- Follow Vue.js best practices
- Use TypeScript for type safety
- Implement responsive designs
- Use Tailwind CSS for styling
- Follow the existing component structure

### Code Style
- Use meaningful variable and function names
- Keep functions focused and concise
- Write self-documenting code
- Include comments for complex logic
- Follow the existing project structure

## Database Migrations

### Creating New Migrations
Use the provided PowerShell script to create migrations:
```powershell
./create-migrations.ps1 -MigrationName "YourMigrationName"
```

This will create migrations for both SQLite and MySQL providers.

### Applying Migrations
Migrations are automatically applied when the application starts.

## License

By contributing to Lingarr, you agree that your contributions will be licensed under the project's GNU Affero General Public License v3.0.