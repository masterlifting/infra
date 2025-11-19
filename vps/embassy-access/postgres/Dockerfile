FROM postgres:18

# Install cron
RUN apt-get update && apt-get install -y cron && rm -rf /var/lib/apt/lists/*

# Copy backup script
COPY backup.sh /usr/local/bin/backup.sh
RUN chmod +x /usr/local/bin/backup.sh

# Copy custom entrypoint
COPY entrypoint.sh /usr/local/bin/custom-entrypoint.sh
RUN chmod +x /usr/local/bin/custom-entrypoint.sh

# Set up cron job to run backup script daily at 2 AM
RUN echo "0 2 * * * /usr/local/bin/backup.sh" > /etc/cron.d/postgres-backup
RUN chmod 0644 /etc/cron.d/postgres-backup
RUN crontab /etc/cron.d/postgres-backup

# Use custom entrypoint
ENTRYPOINT ["/usr/local/bin/custom-entrypoint.sh"]
CMD ["postgres"]