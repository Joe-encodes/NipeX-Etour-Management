# Resetdatabase.ps1

psql -U youruser -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'nipex_etour_db' AND pid <> pg_backend_pid();"
psql -U youruser -d postgres -c "DROP DATABASE IF EXISTS nipex_etour_db;"
psql -U youruser -d postgres -c "CREATE DATABASE nipex_etour_db;"
dotnet ef database update --context AppDbContext