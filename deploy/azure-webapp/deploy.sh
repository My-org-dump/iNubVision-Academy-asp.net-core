#!/usr/bin/env bash
# =====================================================================
#  iNubVision Academy (ASP.NET Core 8) → Azure Web Apps (Linux, .NET 8)
#  + Azure SQL Database
#
#  Usage (Bash / Azure Cloud Shell / WSL):
#     az login
#     ./deploy/azure-webapp/deploy.sh            # first time: creates everything + deploys
#     ./deploy/azure-webapp/deploy.sh --app-only # later releases: publish + deploy only
# =====================================================================
set -euo pipefail

# ---------- change these ----------
RG="rg-inubvision-academy-net"
LOCATION="centralindia"
APP="inubvision-academy-net"            # globally unique → https://$APP.azurewebsites.net
PLAN="plan-inubvision-academy-net"
PLAN_SKU="B1"                           # B1 to start; P0v3/P1v3 for production traffic
SQL_SERVER="sql-inubvision-academy"     # globally unique
SQL_ADMIN="academyadmin"
SQL_PASSWORD="${SQL_PASSWORD:-}"        # export SQL_PASSWORD=... (or you'll be asked)
DB="academy"
ADMIN_EMAIL="${ADMIN_EMAIL:-admin@inubvision.com}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-}"
RAZORPAY_KEY_ID="${RAZORPAY_KEY_ID:-}"
RAZORPAY_KEY_SECRET="${RAZORPAY_KEY_SECRET:-}"
RAZORPAY_WEBHOOK_SECRET="${RAZORPAY_WEBHOOK_SECRET:-}"
# -----------------------------------

publish_and_deploy() {
  echo "▶ Publishing"
  rm -rf publish app.zip
  dotnet publish AcademyNet.csproj -c Release -o publish
  (cd publish && zip -qr ../app.zip .)
  echo "▶ Deploying"
  az webapp deploy -g "$RG" -n "$APP" --src-path app.zip --type zip
  echo "✅ Live at https://$APP.azurewebsites.net   (health: /health)"
}

if [[ "${1:-}" == "--app-only" ]]; then publish_and_deploy; exit 0; fi

[[ -z "$SQL_PASSWORD" ]] && read -rsp "Azure SQL admin password (8+ chars, upper/lower/number/symbol): " SQL_PASSWORD && echo
[[ -z "$ADMIN_PASSWORD" ]] && read -rsp "Website admin password: " ADMIN_PASSWORD && echo

echo "▶ Resource group"
az group create -n "$RG" -l "$LOCATION" -o none

echo "▶ Azure SQL server + database (Basic tier)"
az sql server create -g "$RG" -n "$SQL_SERVER" -l "$LOCATION" -u "$SQL_ADMIN" -p "$SQL_PASSWORD" -o none
az sql server firewall-rule create -g "$RG" -s "$SQL_SERVER" -n AllowAzureServices \
  --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 -o none   # Azure services (the Web App) only
az sql db create -g "$RG" -s "$SQL_SERVER" -n "$DB" --service-objective Basic -o none

echo "▶ App Service plan + Web App (Linux, .NET 8)"
az appservice plan create -g "$RG" -n "$PLAN" -l "$LOCATION" --is-linux --sku "$PLAN_SKU" -o none
az webapp create -g "$RG" -p "$PLAN" -n "$APP" --runtime "DOTNETCORE:8.0" -o none

echo "▶ App settings (override appsettings.json; ':' becomes '__')"
CONN="Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
az webapp config appsettings set -g "$RG" -n "$APP" -o none --settings \
  ASPNETCORE_ENVIRONMENT="Production" \
  Database__Provider="SqlServer" \
  Admin__Email="$ADMIN_EMAIL" \
  Admin__Password="$ADMIN_PASSWORD" \
  Razorpay__KeyId="$RAZORPAY_KEY_ID" \
  Razorpay__KeySecret="$RAZORPAY_KEY_SECRET" \
  Razorpay__WebhookSecret="$RAZORPAY_WEBHOOK_SECRET" \
  Storage__FilesPath="/home/data/files"
az webapp config connection-string set -g "$RG" -n "$APP" -o none -t SQLAzure --settings Default="$CONN"

echo "▶ Always On, HTTPS only, TLS 1.2, health check"
az webapp config set -g "$RG" -n "$APP" -o none --always-on true --http20-enabled true --min-tls-version 1.2 --ftps-state Disabled
az webapp config set -g "$RG" -n "$APP" -o none --generic-configurations '{"healthCheckPath": "/health"}'
az webapp update -g "$RG" -n "$APP" --https-only true -o none

publish_and_deploy

cat <<MSG

Next steps
  • Logs:          az webapp log tail -g $RG -n $APP
  • Custom domain: Portal → $APP → Custom domains → Add (+ free App Service managed certificate)
  • Razorpay webhook URL: https://<your-domain>/api/razorpay/webhook
  • Releases:     push to main (GitHub Actions) or ./deploy/azure-webapp/deploy.sh --app-only
MSG
