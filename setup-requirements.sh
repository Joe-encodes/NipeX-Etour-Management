 #!/usr/bin/env bash
set -euo pipefail

# Detect OS
OS="$(uname -s)"
IS_WINDOWS=false
if [[ "$OS" == MINGW* || "$OS" == CYGWIN* || "$OS" == MSYS* ]]; then
  IS_WINDOWS=true
fi

echo "🔍 Starting setup on $OS…"

# ——— 2. Ensure .NET 8 SDK exists —————————————————————
if ! dotnet --list-sdks | grep -qE '^8\.'; then
  echo "⚠️  .NET 8 SDK not found—installing…"
  if [ "$IS_WINDOWS" = true ]; then
    echo "→ On Windows, please download & install .NET 8 SDK from:"
    echo "    https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
  else
    # *nix: use install script
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir "$HOME/.dotnet8" --sdk
    export DOTNET_ROOT="$HOME/.dotnet8"
    export PATH="$DOTNET_ROOT:$PATH"
  fi
fi
echo "✅ .NET 8 SDK: $(dotnet --list-sdks | grep '^8\.')"

# ——— 3. Backend: EF tools & packages —————————————————
pushd e-tour-api > /dev/null

# Local tools manifest
if [ -f .config/dotnet-tools.json ]; then
  echo "🔄 Restoring local .NET tools…"
  dotnet tool restore
else
  echo "📦 Creating tool manifest & installing dotnet-ef…"
  dotnet new tool-manifest --quiet
  dotnet tool install dotnet-ef --version 9.* --local
fi

# Confirm EF CLI
echo -n "🔧 dotnet-ef version: "
dotnet tool run dotnet-ef --version

# Idempotent NuGet adds
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.* || true
dotnet add package dotenv.net --version 3.* || true

# User‑secrets init
if ! dotnet user-secrets list &>/dev/null; then
  echo "🔐 Initializing user-secrets…"
  dotnet user-secrets init
else
  echo "🔐 User‑secrets already initialized."
fi

popd > /dev/null

# ——— 4. Frontend deps —————————————————————————
pushd e-tour-frontend > /dev/null
echo "👉 Installing frontend dependencies…"
npm ci
popd > /dev/null

# ——— 5. PostgreSQL client check ————————————————————
echo "🔍 Verifying psql client…"
if ! command -v psql &>/dev/null; then
  echo "⚠️  psql not found. Please install PostgreSQL client."
else
  echo "✅ psql: $(psql --version)"
fi

# ——— Final instructions —————————————————————————
cat <<EOF

🎉 Setup complete!

Next steps to get the app running:

1. **Copy & configure your settings**  
   \`\`\`bash
   cp e-tour-api/appsettings.json.example e-tour-api/appsettings.json
   # Edit e-tour-api/appsettings.json: fill in Jwt:Key, DB password, AllowedHosts…
   \`\`\`

2. **Run migrations**  
   \`\`\`bash
   cd e-tour-api
   dotnet tool run dotnet-ef database update --context AppDbContext
   \`\`\`

3. **Start the backend**  
   ```bash
   dotnet run --project e-tour-api
Start the frontend (in a new terminal)


cd e-tour-frontend
npm start
Browse to `http://localhost:3000\` for the React UI
and `http://localhost:5000/swagger\` for the API docs.

If you’re on Windows PowerShell exclusively, here’s the equivalent snippet:


# 1. Ensure .NET 8 SDK
if (-not (dotnet --list-sdks | Select-String '^8\.')) {
  Write-Error '.NET 8 SDK missing—download from https://dotnet.microsoft.com/download/dotnet/8.0'
  exit 1
}

# 2. Backend setup
Push-Location e-tour-api
dotnet tool restore
dotnet tool run dotnet-ef --version
dotnet add package Microsoft.EntityFrameworkCore.Design -v 9.*
dotnet add package dotenv.net -v 3.*
dotnet user-secrets init
Pop-Location

# 3. Frontend
Push-Location e-tour-frontend
npm ci
Pop-Location

Write-Host 'Setup done. Copy appsettings.json.example, run migrations, and start your app!'
