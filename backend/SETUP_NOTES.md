# Apex Racer Backend - Setup Notes

## Implementation Summary

JWT authentication has been successfully implemented for the Nakama backend using Nakama's built-in authentication system.

### What's Working

1. **JWT Authentication** - Fully functional with the following endpoints:
   - Registration: `POST /v2/account/authenticate/email?create=true&username=USERNAME`
   - Login: `POST /v2/account/authenticate/email?create=false`
   - Session tokens with 24-hour expiry
   - Refresh tokens with 7-day expiry

2. **Testing Mode** - Environment variable `TESTING_MODE` available for future use

3. **Configuration** - nakama.yml properly configured with:
   - JWT session settings
   - Console and runtime HTTP keys
   - Socket server keys

### Architecture Decision

We're using Nakama's **built-in authentication endpoints** instead of custom RPC handlers. This approach has several advantages:

- ✅ No custom code needed for authentication
- ✅ Battle-tested authentication logic
- ✅ Works on all platforms (including Apple Silicon Macs)
- ✅ Automatic JWT token generation and validation
- ✅ Built-in security features

### Go Plugin Status

The Go runtime plugin is currently disabled due to architecture compatibility issues between ARM64 (Apple Silicon) and AMD64 (Nakama's official Docker images).

**Impact:** None for authentication. Nakama's built-in endpoints handle all authentication needs.

**Future Considerations:** If custom server-side logic is needed (beyond authentication), you have options:

1. **Use Lua** - Cross-platform, no compilation needed
2. **Use JavaScript/TypeScript** - Modern, cross-platform runtime
3. **Build native ARM64** - Requires building custom Nakama Docker images
4. **Use AMD64 with proper emulation** - Ensure consistent architecture across build/runtime

### Testing

Run the provided test script to verify authentication:

```bash
./test-auth.sh
```

Expected output:
- ✓ Registration successful with JWT token
- ✓ Login successful with JWT token
- ✓ Authenticated API calls working

### Files Modified

- `nakama/nakama.yml` - Added JWT and HTTP key configuration
- `nakama/docker-compose.yml` - Added TESTING_MODE environment variable
- `nakama/Dockerfile` - Simplified to run without Go plugin
- `test-auth.sh` - Test script for authentication endpoints
- `AUTH.md` - Complete authentication documentation
- `.env.example` - Environment variable template

### Future Checklist

Future improvements:
- [ ] Use HTTPS/TLS for all connections
- [ ] Use secure database credentials
- [ ] Enable proper monitoring and logging
