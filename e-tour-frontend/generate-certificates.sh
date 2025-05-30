#!/bin/bash

# Create certificates directory if it doesn't exist
mkdir -p certificates

# Generate private key and certificate
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout certificates/localhost.key \
  -out certificates/localhost.crt \
  -subj "/C=US/ST=State/L=City/O=Organization/CN=localhost" \
  -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"

# Create .env.local with certificate paths
echo "HTTPS=true" > .env.local
echo "SSL_CRT_FILE=certificates/localhost.crt" >> .env.local
echo "SSL_KEY_FILE=certificates/localhost.key" >> .env.local

echo "Certificates generated successfully!"
echo "You may need to trust the certificate in your system's keychain." 