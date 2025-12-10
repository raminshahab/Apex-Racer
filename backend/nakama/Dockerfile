# Simple Lua-based Nakama server
FROM --platform=linux/amd64 heroiclabs/nakama:3.3.0

WORKDIR /nakama

# Copy configuration
COPY nakama.yml ./nakama.yml

# Copy Lua modules
COPY modules/ /nakama/data/modules/

# Expose Nakama ports
# 7349: gRPC API
# 7350: HTTP API
# 7351: Console
EXPOSE 7349 7350 7351

# Run migrations and start Nakama
ENTRYPOINT ["/bin/sh", "-c"]
CMD ["/nakama/nakama migrate up --database.address nakama:nakama@postgres:5432/nakama && exec /nakama/nakama --config /nakama/nakama.yml --database.address nakama:nakama@postgres:5432/nakama"]
