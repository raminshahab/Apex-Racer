# Authentication Guide

## Overview

This Nakama backend uses JWT (JSON Web Token) authentication for secure user registration and login using Nakama's built-in authentication system. The system supports email/password authentication and can be configured for testing modes.

## Authentication Endpoints

### 1. Registration

**Endpoint:** `POST /v2/account/authenticate/email?create=true&username=USERNAME`

**Authentication:** Basic auth with server key (`defaultkey` by default)

**Request Payload:**
```json
{
  "email": "user@example.com",
  "password": "secure_password"
}
```

**Response:**
```json
{
  "created": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 2. Login

**Endpoint:** `POST /v2/account/authenticate/email?create=false`

**Authentication:** Basic auth with server key

**Request Payload:**
```json
{
  "email": "user@example.com",
  "password": "secure_password"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refresh_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

## JWT Configuration

The JWT tokens are configured in `nakama/nakama.yml`:

- **Token Expiry:** 24 hours (86400 seconds)
- **Refresh Token Expiry:** 7 days (604800 seconds)
- **Encryption Keys:** Should be changed in production

## Testing Mode

### Enabling Testing Mode

For development and testing, you can enable anonymous authentication:

1. Create a `.env` file in the project root:
```bash
TESTING_MODE=true
```

2. Restart the Docker containers:
```bash
docker-compose down
docker-compose up --build
```

### Disabling Testing Mode (Production) ToDo: Use a conditional safe guard against prod 

For production environments:

1. Set `TESTING_MODE=false` in your `.env` file or remove the variable entirely
2. Restart the containers

When testing mode is disabled, all API calls must include a valid JWT token in the Authorization header.

## Using JWT Tokens

After registration or login, use the returned token for authenticated requests:

### HTTP API Calls
```
Authorization: Bearer <jwt-token>
```

### WebSocket Connection
Include the token when connecting to Nakama's realtime socket.

## Security Notes

1. **Change encryption keys** in `nakama.yml` before deploying to production
2. **Use HTTPS** in production environments
3. **Never commit** `.env` files with real credentials
4. **Disable testing mode** in production
5. Store tokens securely on the client side

## cURL Examples

### Register a new user:
```bash
curl -X POST "http://localhost:7350/v2/account/authenticate/email?create=true&username=player1" \
  -H "Content-Type: application/json" \
  -u "defaultkey:" \
  -d '{"email":"user@example.com","password":"password123"}'
```

### Login with existing user:
```bash
curl -X POST "http://localhost:7350/v2/account/authenticate/email?create=false" \
  -H "Content-Type: application/json" \
  -u "defaultkey:" \
  -d '{"email":"user@example.com","password":"password123"}'
```

### Get account info (authenticated):
```bash
curl -X GET "http://localhost:7350/v2/account" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## Example Unity Client Code

```csharp
using Nakama;
using System.Threading.Tasks;

public class AuthManager
{
    private IClient client;
    private ISession session;

    public async Task Initialize()
    {
        // Create client instance
        client = new Client("http", "localhost", 7350, "defaultkey");
    }

    // Register a new user
    public async Task<ISession> Register(string email, string password, string username)
    {
        session = await client.AuthenticateEmailAsync(email, password, username, true);
        return session;
    }

    // Login with existing user
    public async Task<ISession> Login(string email, string password)
    {
        session = await client.AuthenticateEmailAsync(email, password, null, false);
        return session;
    }

    // Get user account info
    public async Task<IApiAccount> GetAccount()
    {
        var account = await client.GetAccountAsync(session);
        return account;
    }
}

// Usage example:
var authManager = new AuthManager();
await authManager.Initialize();

// Register
var session = await authManager.Register("user@example.com", "password123", "player1");
Debug.Log($"Registered with token: {session.AuthToken}");

// Or login
session = await authManager.Login("user@example.com", "password123");
Debug.Log($"Logged in as: {session.Username}");
```

## Troubleshooting

### "Authentication failed: invalid credentials"
- Check that the email and password are correct
- Ensure the user is registered

### "Failed to generate token"
- Check Nakama server logs
- Verify encryption keys are properly set in nakama.yml

### Testing mode not working
- Verify TESTING_MODE=true in your .env file
- Check Docker logs: `docker-compose logs nakama`
- Ensure containers were rebuilt after changing environment variables
