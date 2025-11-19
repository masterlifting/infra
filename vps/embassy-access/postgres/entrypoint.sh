#!/bin/bash
set -e

echo "Starting custom entrypoint..."

# Start cron
echo "Starting cron daemon..."
service cron start || cron

echo "Handing over to original entrypoint..."
exec /usr/local/bin/docker-entrypoint.sh "$@"