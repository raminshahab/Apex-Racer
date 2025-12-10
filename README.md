# Apex Racer

A multiplayer racing game with real-time leaderboards, built with Unity and Nakama.

## Project Structure

```
Apex-Racer/
├── backend/              # Nakama game server
│   ├── nakama/          # Nakama configuration and modules
│   │   ├── modules/     # Lua runtime modules
│   │   └── migrations/  # Database schema
│   └── tests/           # Backend test scripts
└── client-unity/        # Unity game client
    ├── Assets/          # Unity project assets
    └── ProjectSettings/ # Unity configuration
```

## Features

### Backend (Nakama)
- **Cycle-Based Leaderboards** - 5-minute racing cycles with automatic resets
- **Authentication System** - JWT-based auth with email/password and device ID
- **Reward Distribution** - Automatic rewards for top players each cycle
- **Anti-Cheat System** - Time-bound validation with admin review tools
- **Idempotent Operations** - Safe retry logic for all submissions
- **Data Persistence** - PostgreSQL storage via Nakama Storage API

### Client (Unity)
- **Nakama Integration** - Full C# SDK integration
- **Retry Logic** - Exponential backoff for network operations
- **Authentication UI** - Email/password and device authentication
- **Leaderboard Display** - Real-time leaderboard updates
- **Reward History** - View past rewards and rankings

## Quick Start

### Prerequisites
- Docker and Docker Compose
- Unity 2021.3+ (for client development)

### Running the Backend

1. Navigate to the backend directory:
   ```bash
   cd backend/nakama
   ```

2. Start the services:
   ```bash
   docker-compose up --build
   ```

3. Access the Nakama Console:
   - URL: http://localhost:7351
   - Default credentials: admin/password

4. API Endpoints:
   - HTTP API: http://localhost:7350
   - WebSocket: ws://localhost:7351

### Running the Unity Client

1. Open the project in Unity:
   ```bash
   cd client-unity
   # Open in Unity Editor
   ```

2. Open the main scene in `Assets/Scenes/`

3. Press Play to run in the editor

## Documentation

### Backend Documentation
- [README.md](backend/README.md) - Comprehensive backend documentation
- [SECURITY.md](backend/SECURITY.md) - Security configuration and best practices
- [AUTH.md](backend/AUTH.md) - Authentication system details
- [ANTI_CHEAT.md](backend/ANTI_CHEAT.md) - Anti-cheat implementation
- [AUTOMATIC_RESET.md](backend/AUTOMATIC_RESET.md) - Leaderboard reset system
- [SCALABILITY.md](backend/SCALABILITY.md) - Scaling strategies
- [DATABASE.md](backend/DATABASE.md) - Database schema
- [STORAGE_QUERIES.md](backend/STORAGE_QUERIES.md) - Query examples

### Client Documentation
- [QUICKSTART_UI.md](client-unity/QUICKSTART_UI.md) - UI quick start guide
- [UI_SETUP_GUIDE.md](client-unity/UI_SETUP_GUIDE.md) - Detailed UI setup

## Testing

Run the backend tests:

```bash
cd backend/tests

# Basic leaderboard test
./test-leaderboard-simple.sh

# Automatic reset test
./test-automatic-reset.sh

# Anti-cheat test
./test-anti-cheat.sh

# Authentication test
./test-auth.sh
```

## Security Notice

This project uses **default credentials for local development**. These are intentionally simple to make local testing easy.

**IMPORTANT:** Never use these default credentials in production!

See [SECURITY.md](backend/SECURITY.md) for:
- Production security configuration
- Generating secure credentials
- Environment variable setup
- Best practices

## Architecture

### Technology Stack
- **Backend**: Nakama (Lua runtime) + PostgreSQL
- **Client**: Unity (C#) + Nakama SDK
- **Infrastructure**: Docker + Docker Compose
- **Scheduler**: Custom cron service for automatic resets

### Key Design Decisions
1. **Lua Runtime** - Simpler than Go plugins, no compilation needed
2. **External Scheduler** - More reliable than internal timers
3. **Client-Side Idempotency Keys** - Prevents duplicate submissions
4. **Cycle-Based Natural Keys** - Automatic idempotency without coordination
5. **Non-Blocking Anti-Cheat** - Flags suspicious activity without blocking users

## Performance

- Submission latency: ~100ms p99
- Leaderboard query: ~50ms p99
- Reset duration: ~5s (1K users)
- Supports: 1K-10K active users (current setup)

For scaling beyond 10K users, see [SCALABILITY.md](backend/SCALABILITY.md).

## API Examples

### Submit Race Time
```bash
curl -X POST "http://localhost:7350/v2/rpc/submit_race_time?http_key=defaultkey" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '"{\"race_time\":45.123,\"idempotency_key\":\"unique-uuid-here\"}"'
```

### Get Reward History
```bash
curl -X POST "http://localhost:7350/v2/rpc/race_reward_history?http_key=defaultkey" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '""'
```

### View Leaderboard
```bash
curl -X GET "http://localhost:7350/v2/leaderboard/race_times?http_key=defaultkey"
```

## Development

### Backend Development
- Lua modules are in `backend/nakama/modules/`
- Changes are hot-reloaded on file save
- Check logs: `docker logs nakama-go-server`

### Client Development
- Scripts are in `client-unity/Assets/Scripts/`
- Main connection manager: `NakamaConnectionManager.cs`
- Authentication: `NakamaEmailAuth.cs`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

[Add your license here]

## Support

For issues or questions:
- Check the documentation in the `backend/` folder
- Review the test scripts for usage examples
- Check Nakama logs for debugging

## Roadmap

- [ ] Mobile build support
- [ ] Additional race tracks
- [ ] Ghost replays
- [ ] Friend challenges
- [ ] Advanced anti-cheat metrics
- [ ] Global leaderboards (cross-region)
