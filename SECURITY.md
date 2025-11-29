# Security Policy

## Supported Versions

We release patches for security vulnerabilities in the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

If you discover a security vulnerability within Lingarr, please send an email to the maintainers via GitHub or Discord. All security vulnerabilities will be promptly addressed.

**Please do not report security vulnerabilities through public GitHub issues.**

### What to Include

When reporting a vulnerability, please include:

1. Type of vulnerability (e.g., XSS, SQL injection, authentication bypass)
2. Full paths of source file(s) related to the vulnerability
3. Location of the affected source code (tag/branch/commit or direct URL)
4. Step-by-step instructions to reproduce the issue
5. Proof-of-concept or exploit code (if possible)
6. Impact of the issue, including how an attacker might exploit it

### Response Timeline

- **Initial Response**: Within 48 hours
- **Status Update**: Within 7 days
- **Fix Release**: Depends on severity and complexity

## Security Best Practices

When deploying Lingarr:

1. **Use Strong API Keys**: Always use strong, randomly generated API keys for all services
2. **Network Isolation**: Run Lingarr in an isolated Docker network
3. **Environment Variables**: Never commit sensitive environment variables to version control
4. **Regular Updates**: Keep Lingarr and all dependencies up to date
5. **Database Security**: If using MySQL, ensure proper authentication and network restrictions
6. **Reverse Proxy**: Consider placing Lingarr behind a reverse proxy with HTTPS
7. **File Permissions**: Ensure proper file permissions for mounted volumes

## Known Security Considerations

### API Keys
All translation service API keys are stored in the database. Ensure your database is properly secured and not publicly accessible.

### Docker Volumes
Lingarr requires access to media directories. Ensure these volumes are mounted with appropriate permissions.

### Database Connections
When using MySQL, ensure the database connection is secured and uses proper authentication.

## Disclosure Policy

When we receive a security bug report, we will:

1. Confirm the problem and determine affected versions
2. Audit code to find any similar problems
3. Prepare fixes for all supported releases
4. Release patches as soon as possible

## Security Updates

Security updates will be announced via:
- GitHub Security Advisories
- GitHub Releases
- Discord Server

## Contact

- **GitHub Issues**: [Create an issue](https://github.com/lingarr-translate/lingarr/issues) (for non-security bugs)
- **Discord**: [Join our Discord](https://discord.gg/HkubmH2rcR)
- **GitHub Discussions**: [Security discussions](https://github.com/lingarr-translate/lingarr/discussions/categories/security)

Thank you for helping keep Lingarr and its users safe!
