# Security Configuration

## Development vs Production

This project includes default credentials for **local development only**. These defaults are intentionally simple to make local testing easy.

### Current Default Credentials (Development)

The following default values are used in `nakama.yml` and `docker-compose.yml`:

- Database Password: `nakama`
- Console HTTP Key: `defaultkey`
- Session Encryption Key: `defaultencryptionkey123456`
- Refresh Encryption Key: `defaultrefreshkey123456`
- Server Key: `defaultkey`
- Runtime HTTP Key: `defaultkey`

## IMPORTANT: Production Deployment

**NEVER use these default credentials in production!**

Before deploying to production:

1. **Create a `.env` file** from `.env.example`:
   ```bash
   cp nakama/.env.example nakama/.env
   ```

2. **Generate secure credentials**:
   ```bash
   # Generate random passwords/keys
   openssl rand -base64 32  # For encryption keys (32+ characters)
   openssl rand -base64 24  # For API keys
   ```

3. **Update nakama.yml** to use environment variables:
   ```yaml
   database:
     password: "${POSTGRES_PASSWORD}"
   console:
     http_key: "${NAKAMA_CONSOLE_HTTP_KEY}"
   session:
     encryption_key: "${NAKAMA_SESSION_ENCRYPTION_KEY}"
     refresh_encryption_key: "${NAKAMA_REFRESH_ENCRYPTION_KEY}"
   socket:
     server_key: "${NAKAMA_SERVER_KEY}"
   runtime:
     http_key: "${NAKAMA_RUNTIME_HTTP_KEY}"
   ```

4. **Update docker-compose.yml**:
   ```yaml
   services:
     postgres:
       environment:
         POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
     nakama:
       environment:
         - NAKAMA_CONSOLE_HTTP_KEY=${NAKAMA_CONSOLE_HTTP_KEY}
         # Add other environment variables
   ```

5. **Add `.env` to `.gitignore`** (already done in this project)

## Security Best Practices

1. **Change ALL default credentials** before production deployment
2. **Use strong, randomly generated keys** (32+ characters)
3. **Never commit `.env` files** to version control
4. **Use secrets management** for production (AWS Secrets Manager, HashiCorp Vault, etc.)
5. **Enable HTTPS/TLS** for production deployments
6. **Restrict database access** to only necessary services
7. **Regular security audits** and dependency updates
8. **Monitor for suspicious activity** using the anti-cheat system

## Client-Side Security

The Unity client also uses `defaultkey` for local development:

```csharp
// client-unity/Assets/Scripts/NakamaConnectionManager.cs
private const string ServerKey = "defaultkey";
```

For production:
1. Update the ServerKey constant to match your production key
2. Consider loading configuration from a secure source
3. Never hardcode production credentials in client code
4. Use obfuscation for release builds

## Additional Resources

- [Nakama Security Best Practices](https://heroiclabs.com/docs/nakama/concepts/security/)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Docker Security](https://docs.docker.com/engine/security/)
