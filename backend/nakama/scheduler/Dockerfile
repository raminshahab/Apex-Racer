# Lightweight scheduler for automatic leaderboard resets
FROM alpine:3.18

# Install curl and dcron (Alpine's cron implementation)
RUN apk add --no-cache curl dcron

# Copy the reset script
COPY reset-cron.sh /usr/local/bin/reset-cron.sh
RUN chmod +x /usr/local/bin/reset-cron.sh

# Create crontab file with 5-minute interval
# Format: minute hour day month weekday command
RUN echo "*/5 * * * * /usr/local/bin/reset-cron.sh >> /var/log/cron.log 2>&1" > /etc/crontabs/root

# Create log file
RUN touch /var/log/cron.log

# Start cron in foreground and tail log
CMD crond -f -l 2 & tail -f /var/log/cron.log
