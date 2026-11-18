# iNubVision Academy: ASP.NET Core 8

A DevOps training platform with a course store, Razorpay payments, a learning portal and an admin panel.
**Stack:** .NET 8 · Razor Pages · ASP.NET Core Identity · EF Core · Azure SQL / SQL Server (SQLite for local runs)

## Features

**Website**
- Landing page:
  - announcement bar
  - hero with a CI/CD pipeline dashboard
  - stats
  - scrolling tech strip
  - free webinars
  - course cards
  - "why us"
  - 8-phase curriculum
  - trainer bio
  - success stories and reviews
  - how to enroll
  - FAQ
  - contact form
- Course catalog and course pages with price, MRP and % off, schedule, highlights, and a syllabus with free-preview lessons
- Terms, privacy and refund pages (template text: review with a legal advisor)

**Buying**
- Cart that works for guests; login is asked only at checkout
- Coupons: % or flat ₹ off, expiry date, max uses, on/off switch
- **Razorpay** checkout (UPI, cards, netbanking, wallets) with server-side signature check, plus an optional webhook
- **Demo mode** when Razorpay keys are empty, so you can test the whole flow without real money
- Free courses and 100%-off coupons enroll instantly

**Learning**
- *My learning* with a progress bar for each course
- Lesson player for YouTube (unlisted), Vimeo, Cloudflare Stream or `.mp4` links
- Lesson notes downloadable only by enrolled students
- "Mark complete → next lesson"

**Admin panel**
- Dashboard: revenue, orders, users, enrollments, messages
- Courses, modules and lessons (draft / publish, reorder, upload notes, free preview)
- Coupons, orders and contact messages
- Students: search, and grant or remove access manually for offline payments
- Reviews and success stories: real ones only. These sections stay hidden on the home page until you add some.

## First login
On first start the app creates the admin account from config (`admin@inubvision.com` / `ChangeMe123` unless you set your own). It also adds three starter courses and a `WELCOME10` coupon. **Set your own admin password before going live.**

## Razorpay
1. Create an account at razorpay.com and finish KYC. Start with **Test mode** keys.
2. Put the Key Id and Key Secret into the app settings (see below).
3. Optional: add a webhook at `https://<your-domain>/api/razorpay/webhook` for the events `payment.captured` and `order.paid`, and set the webhook secret.
4. Test with Razorpay's test cards and UPI IDs, then switch to Live keys.

## Run locally
```bash
dotnet run                    # SQLite database in ./App_Data
docker compose up -d --build  # → http://localhost:8081
```

## Configuration
`appsettings.json` (or app settings / environment variables, where `:` becomes `__`):

| Setting | Purpose |
|---|---|
| `Database:Provider` + `ConnectionStrings:Default` | `Sqlite` (default) or `SqlServer` |
| `Admin:Email`, `Admin:Password` | First admin account |
| `Razorpay:KeyId`, `Razorpay:KeySecret`, `Razorpay:WebhookSecret` | Payments (empty = demo mode) |
| `Storage:FilesPath` | Folder for lesson notes |
| `Site:*` | Brand text, stats, webinars, technologies, trainer bio |

## Deploy to Azure Web Apps (recommended)
```bash
az login
export SQL_PASSWORD='Str0ng-Db-Pass!' ADMIN_PASSWORD='Str0ng-Site-Pass'
./deploy/azure-webapp/deploy.sh             # creates Azure SQL + Web App (.NET 8, Linux) and deploys
./deploy/azure-webapp/deploy.sh --app-only  # later manual releases
```
The script sets up:
- **Database:** Azure SQL Database (Basic), reachable from Azure services only
- **Web App:** Linux, .NET 8, Always On, HTTPS only, TLS 1.2+, health check `/health`
- **Settings:** database connection, admin login and Razorpay keys as app settings
- **Notes storage:** `/home/data/files`, which App Service keeps across restarts

Edit the names at the top of the script first (the app and SQL server names must be globally unique).

## Deploy to IIS on a Windows VM (alternative)
```powershell
dotnet publish -c Release -o publish
.\deploy\iis\1-setup-iis.ps1                                # once: IIS + .NET 8 Hosting Bundle
.\deploy\iis\2-deploy-site.ps1 -SourcePath C:\temp\publish  # each release
```
Put secrets in `C:\inetpub\AcademyNet\appsettings.Production.json`. Redeploys keep that file and `App_Data`.

## Deploy to IIS with GitHub Actions

The workflow is `.github/workflows/deploy-iis-vm.yml`:

```
GitHub (ubuntu runner)                     Your Azure Windows VM (self-hosted runner)
──────────────────────                     ──────────────────────────────────────────
Build & publish ─┐
Security scan  ──┴──► artifact ──────────► Backup current site → deploy-site.ps1 → health check
                                                      └── if the health check fails → automatic rollback
```

The runner on the VM makes an **outbound** connection to GitHub. You don't open any new ports, and no VM password is stored in GitHub.

### One-time setup

**1. Prepare IIS on the VM** (RDP, PowerShell as Administrator):
```powershell
.\deploy\iis\1-setup-iis.ps1
```

**2. Create the settings file** `C:\inetpub\AcademyNet\appsettings.Production.json` on the VM:
```json
{
  "Admin": { "Email": "you@inubvision.com", "Password": "Your-Strong-Pass1" },
  "Razorpay": { "KeyId": "", "KeySecret": "", "WebhookSecret": "" }
}
```
To use Azure SQL instead of SQLite, also add `"Database": { "Provider": "SqlServer" }` and `"ConnectionStrings": { "Default": "..." }`.

**3. Install the GitHub runner on the VM:**
1. In GitHub, open the repo → **Settings → Actions → Runners → New self-hosted runner → Windows x64**.
2. On the VM (PowerShell as Administrator), run the **download** commands GitHub shows, then configure the runner like this:
```powershell
mkdir C:\actions-runner; cd C:\actions-runner
# ... the download + extract commands from the GitHub page ...
.\config.cmd --url https://github.com/<owner>/<repo> --token <TOKEN-FROM-GITHUB> `
  --name vm-academy --labels iis-prod --work _work `
  --runasservice --windowslogonaccount "NT AUTHORITY\SYSTEM"
```
3. Check that the runner shows **Idle** under *Settings → Actions → Runners*.

The runner runs as the local SYSTEM account because managing IIS (creating app pools and setting folder permissions) needs admin rights.

**4. Create the environment:** in GitHub, go to **Settings → Environments → New environment** and name it `production-vm`. Add yourself as a *required reviewer* if you want to approve each deployment.

**5. Push to `main`.** The run builds, scans, waits for your approval (if set), deploys, checks `http://localhost/health` on the VM, and rolls back automatically if the new version doesn't come up.

### Good to know
- **Kept across deploys:** `App_Data` (database and lesson notes) and `appsettings.Production.json`. The workflow never touches them.
- **Backups:** the last 5 releases are kept in `C:\inetpub\_backups\AcademyNet`. To roll back by hand, copy one back and run `iisreset`.
- **Which workflow to use:** this repo also has `ci-cd.yml`, which deploys to Azure **Web Apps**. If you only use the VM, delete `ci-cd.yml`; otherwise its deploy job fails on every push because the Azure secrets aren't set.
- **Security:** a self-hosted runner executes the workflow code from your repo, so keep the repo **private**, or allow only trusted collaborators to push to `main`.

## CI/CD with GitHub Actions

The pipeline is in `.github/workflows/ci-cd.yml`:

```
Pull request ──► Build & test ──┐
                Security scan ──┴──► (stops here for PRs)

Push to main ──► Build & test ──┐
                Security scan ──┼──► Docker image → ghcr.io/<owner>/<repo>:latest + :<commit>
                                └──► Deploy to Azure Web Apps ──► Smoke test (health endpoint)
```

| Job | What it does |
|---|---|
| **Build & test** | Compiles, runs tests and uploads the app as a pipeline artifact |
| **Security scan** | Trivy scans dependencies, Dockerfile and config for HIGH/CRITICAL issues and leaked secrets (report-only; set `exit-code: '1'` to block) |
| **Docker image** | Builds the Dockerfile and pushes to GitHub Container Registry, ready for AKS later |
| **Deploy** | Logs in to Azure with **OIDC** (no stored passwords) and deploys the same artifact that was tested |
| **Smoke test** | Calls the health URL until the new version answers "UP"; fails the run if it doesn't |

### One-time setup

**1. Create the Azure resources** (once): run `./deploy/azure-webapp/deploy.sh` (see the deployment section).

**2. Let GitHub deploy to Azure without a password (OIDC).** Run in Azure Cloud Shell, with your own values:
```bash
RG=rg-inubvision-academy-net ; APP=inubvision-academy-net ; REPO=<github-user>/<repo-name>

APP_ID=$(az ad app create --display-name "gh-$APP" --query appId -o tsv)
az ad sp create --id $APP_ID -o none
az role assignment create --assignee $APP_ID --role "Website Contributor" \
  --scope $(az webapp show -g $RG -n $APP --query id -o tsv) -o none
az ad app federated-credential create --id $APP_ID --parameters "{
  \"name\": \"github-production\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$REPO:environment:production\",
  \"audiences\": [\"api://AzureADTokenExchange\"] }"

echo "AZURE_CLIENT_ID=$APP_ID"
echo "AZURE_TENANT_ID=$(az account show --query tenantId -o tsv)"
echo "AZURE_SUBSCRIPTION_ID=$(az account show --query id -o tsv)"
```

**3. Add them to the GitHub repo:** *Settings → Secrets and variables → Actions*.
- **Secrets:** `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`
- **Variables:** `AZURE_WEBAPP_NAME` = `inubvision-academy-net`
- **Environments:** create `production`. Optionally add yourself as a *required reviewer*, so every deployment waits for your approval.

Or with the GitHub CLI:
```bash
gh secret set AZURE_CLIENT_ID --body "<value>"
gh secret set AZURE_TENANT_ID --body "<value>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<value>"
gh variable set AZURE_WEBAPP_NAME --body "inubvision-academy-net"
```

**4. Push to `main`** and watch the run in the **Actions** tab.

> The OIDC "subject" must match exactly: `repo:<owner>/<repo>:environment:production`. If the deploy step says *"No matching federated identity record"*, check the repo name and the environment name.


## Project structure
```
├── .github/workflows/
│   ├── ci-cd.yml                # CI/CD → Azure Web Apps
│   └── deploy-iis-vm.yml        # CI/CD → IIS on an Azure Windows VM (self-hosted runner)
├── deploy/
│   ├── azure-webapp/deploy.sh   # Azure Web Apps + Azure SQL
│   └── iis/                     # IIS on a Windows VM
├── Dockerfile · docker-compose.yml · web.config
├── AcademyNet.csproj
├── Program.cs                   # setup, /health, file downloads, Razorpay webhook
├── Data/                        # EF Core entities, DbContext, seed data
├── Services/                    # cart & coupons, Razorpay, orders, files, helpers
├── Pages/                       # Razor Pages: public, Account, Courses, Cart, Checkout, Learn, Admin
└── wwwroot/                     # css, js
```

> This code wasn't compiled where it was written. Run `dotnet build` once locally and fix anything it reports before your first push.
