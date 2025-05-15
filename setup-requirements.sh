#!/usr/bin/env bash
set -euo pipefail

# ─── SYSTEM CHECK ─────────────────────────────────────────────
OS="$(uname -s)"
IS_WINDOWS=false
if [[ "$OS" == MINGW* || "$OS" == CYGWIN* || "$OS" == MSYS* ]]; then
  IS_WINDOWS=true
fi

echo "🔍 Starting setup on $OS…"

# ─── NODE.JS CHECK ────────────────────────────────────────────
echo "🔍 Checking Node.js version…"
if ! node --version | grep -qE '^v1[6-9]|^v[2-9]'; then
  echo "⚠️ Node.js 16+ required. Install from https://nodejs.org/"
  exit 1
fi

echo "✅ Node.js: $(node --version)"

# ─── 1. ENSURE .NET SDKS ─────────────────────────────────────
echo "🔍 Checking .NET SDKs…"

# .NET 8 SDK
if ! dotnet --list-sdks | grep -qE '^8\.'; then
  echo "⚠️ .NET 8 SDK not found."
  if [ "$IS_WINDOWS" = true ]; then
    echo "→ On Windows, download .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
  else
    echo "→ Installing .NET 8 SDK locally…"
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir "$HOME/.dotnet8" --sdk
    export DOTNET_ROOT="$HOME/.dotnet8"
    export PATH="$DOTNET_ROOT:$PATH"
    echo "👉 Remember to add to your shell profile if persistent:"
    echo "   export DOTNET_ROOT=\"$HOME/.dotnet8\""
    echo "   export PATH=\"$DOTNET_ROOT:$PATH\""
  fi
fi

echo "✅ .NET SDKs:"
dotnet --list-sdks | grep -E '^(8\.|9\.)'

# ─── 2. BACKEND SETUP ─────────────────────────────────────────
echo "
# ─── Backend Setup ───────────────────────────────────────────"
pushd e-tour-api > /dev/null

echo "🔧 Restoring backend dependencies…"
dotnet restore

if [[ "${ASPNETCORE_ENVIRONMENT:-Production}" != "Production" ]]; then
  echo "🧪 Dev mode: configuring EF CLI & secrets…"
  if [ -f .config/dotnet-tools.json ]; then
    dotnet tool restore
  else
    dotnet new tool-manifest --quiet
    dotnet tool install dotnet-ef --local
  fi
  echo -n "🔧 dotnet-ef: "
  dotnet tool run dotnet-ef --version
  if ! dotnet user-secrets list &>/dev/null; then
    echo "🔐 Initializing user-secrets…"
    dotnet user-secrets init
  fi
fi

popd > /dev/null

# ─── 3. FRONTEND SETUP ────────────────────────────────────────
echo "
# ─── Frontend Setup ─────────────────────────────────────────"
pushd e-tour-frontend > /dev/null

if [ ! -f .env ]; then
  echo "⚠️  Missing .env — please create from .env.example"
fi

echo "👉 Installing front-end dependencies…"
npm ci
popd > /dev/null

# ─── 4. POSTGRES CLIENT ───────────────────────────────────────
echo "
# ─── PostgreSQL Client Check ─────────────────────────────────"
if ! command -v psql &>/dev/null; then
  echo "⚠️ psql not found. Install PostgreSQL client."
else
  echo "✅ psql: $(psql --version)"
fi

# ─── 5. DONE ─────────────────────────────────────────────────
echo "
🎉 Setup script finished.\nNext steps:"
echo "1. Copy config: cp e-tour-api/appsettings.json.example e-tour-api/appsettings.json"
echo "2. Edit appsettings.json: fill Jwt.Key, DB creds, AllowedHosts…"
echo "3. Run migrations: cd e-tour-api && dotnet tool run dotnet-ef database update --context AppDbContext"
echo "4. Start backend: dotnet run --project e-tour-api"
echo "5. Start frontend: cd e-tour-frontend && npm start"