#!/bin/bash

# Directory containing uploaded files
UPLOADS_DIR="e-tour-api/wwwroot/uploads"

# Get list of files in uploads directory
FILES=$(ls "$UPLOADS_DIR")

# Get list of signed file paths from the database using psql (PostgreSQL)
# Adjust connection parameters as needed
SIGNED_FILES=$(psql -U your_db_user -d your_db_name -t -c "SELECT \"SignedFilePath\" FROM \"Documents\" WHERE \"SignedFilePath\" IS NOT NULL;")

# Convert signed files to array
readarray -t SIGNED_FILES_ARRAY <<<"$SIGNED_FILES"

# Loop through files and delete those not in the signed files list
for file in $FILES; do
  if [[ ! " ${SIGNED_FILES_ARRAY[@]} " =~ " ${file} " ]]; then
    echo "Deleting orphaned file: $file"
    rm "$UPLOADS_DIR/$file"
  fi
done

echo "Cleanup complete."
