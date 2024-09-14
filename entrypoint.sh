#!/bin/bash

# Default values if PUID and PGID are not set
PUID=${PUID:-1654}
PGID=${PGID:-1654}

# Create a new group with the specified PGID
if [ ! "$(getent group app)" ]; then
    echo "Creating group with GID $PGID"
    groupmod -g "$PGID" app
fi

# Modify the user with the specified PUID
if [ ! "$(id -u app)" -eq "$PUID" ]; then
    echo "Changing UID to $PUID"
    usermod -u "$PUID" app
fi

# Ensure correct ownership of relevant directories
chown -R app:app /app

# Switch to the non-root user and start the application
exec gosu app "$@"
