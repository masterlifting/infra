#!/bin/bash

BACKUP_DIR="/backups"
LAST_BACKUP_FILE="$BACKUP_DIR/last_backup.txt"
INTERVAL_DAYS=${BACKUP_INTERVAL_DAYS:-7}

# Ensure backup directory exists
mkdir -p "$BACKUP_DIR"

# Get current date in seconds since epoch
CURRENT_DATE=$(date +%s)

# Check if last backup file exists
if [ -f "$LAST_BACKUP_FILE" ]; then
    LAST_BACKUP_DATE=$(cat "$LAST_BACKUP_FILE")
    DAYS_SINCE_LAST=$(( (CURRENT_DATE - LAST_BACKUP_DATE) / 86400 ))
    if [ "$DAYS_SINCE_LAST" -lt "$INTERVAL_DAYS" ]; then
        echo "Not time for backup yet. Days since last: $DAYS_SINCE_LAST, Interval: $INTERVAL_DAYS"
        exit 0
    fi
fi

# Perform backup
BACKUP_FILE="$BACKUP_DIR/backup_$(date +%Y%m%d_%H%M%S).sql"
pg_dump -h localhost -U "$POSTGRES_USER" -d "$POSTGRES_DB" > "$BACKUP_FILE"

# Update last backup date
echo "$CURRENT_DATE" > "$LAST_BACKUP_FILE"

echo "Backup completed: $BACKUP_FILE"