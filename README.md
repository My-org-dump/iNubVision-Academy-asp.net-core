# iNubVision Academy: ASP.NET Core 8 on IIS (Azure Windows VM)

A DevOps training platform with a course store, Razorpay payments, a learning portal and an admin panel. It's hosted on **IIS on an Azure Windows VM** and deployed automatically by **GitHub Actions**.

**Stack:** .NET 8 · Razor Pages · ASP.NET Core Identity · EF Core · SQLite on the VM (or Azure SQL) · IIS

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

## How it is hosted

```
 Developer ──git push──► GitHub ──► GitHub Actions
                                     ├─ Build & publish (GitHub's servers)
                                     ├─ Security scan (Trivy)
                                     └─ Deploy job ─────────────┐ runs on the VM itself (self-hosted runner)
                                                                ▼
 Students ──HTTPS──► Azure Windows VM (Windows Server 2022)
                     └─ IIS ─ site "AcademyNet" ─ C:\inetpub\AcademyNet
                                 ├─ App_Data\academy.db        (SQLite database)
                                 ├─ App_Data\files\            (lesson notes)
                                 ├─ App_Data\keys\             (login encryption keys)
                                 └─ appsettings.Production.json (admin login, Razorpay keys; never in Git)
```

## Setup (one time, about 20 minutes)

Run these on **your PC** in PowerShell. You need the Azure CLI (`winget install Microsoft.AzureCLI`, then `az login`) and Git. You don't need to RDP into the VM.

### 1. Put the code on GitHub
Create a **private** repo, then push this folder:
```powershell
git init; git add .; git commit -m "iNubVision Academy"
git branch -M main
git remote add origin https://github.com/<owner>/<repo>.git
git push -u origin main
```
The first run only builds; the deploy job waits until the runner exists (step 4).

### 2. Create the VM (installs IIS and the .NET 8 Hosting Bundle)
```powershell
.\deploy\azure\create-vm.ps1 -MyIp <your-public-ip>
```
`-MyIp` limits Remote Desktop to your own IP, which is recommended. The script prints the VM's public IP at the end.

### 3. Save the site settings on the VM
```powershell
.\deploy\azure\configure-vm.ps1 -AdminEmail you@inubvision.com
# with Razorpay:   -RazorpayKeyId rzp_test_xxx   (it asks for the secret)
# with Azure SQL:  -SqlConnection "Server=tcp:...;Database=academy;User ID=...;Password=...;Encrypt=True;"
```
This writes `C:\inetpub\AcademyNet\appsettings.Production.json` on the VM, readable only by Administrators and the site. **Secrets never go into GitHub.**

### 4. Connect GitHub to the VM
1. In GitHub, open **Settings → Actions → Runners → New self-hosted runner** and copy the token shown after `--token`.
2. On your PC, run:
```powershell
.\deploy\azure\install-runner.ps1 -Repo <owner>/<repo>
```
3. In GitHub, check **Settings → Actions → Runners**: the runner should show **Idle** with the label `iis-prod`.

### 5. Create the environment (optional approval gate)
In GitHub, go to **Settings → Environments → New environment** and name it `production-vm`. Add yourself as a *required reviewer* if you want to approve every release.

### 6. Deploy
Re-run the latest workflow in the **Actions** tab, or push any commit to `main`. Then open `http://<VM-IP>`, and log in at `/Account/Login` with the admin email and password from step 3.

## Every release after that
```powershell
git add .; git commit -m "your change"; git push
```
The pipeline then runs these steps:

| Step | Where | What happens |
|---|---|---|
| Build & publish | GitHub | `dotnet build` and `dotnet publish`; the output is stored as an artifact |
| Security scan | GitHub | Trivy scans dependencies, config and secrets (report-only; set `exit-code: '1'` to block) |
| Approval | GitHub | Waits for you if `production-vm` has required reviewers |
| Backup | VM | Copies the current site to `C:\inetpub\_backups\AcademyNet\<timestamp>` (keeps the last 5) |
| Deploy | VM | `deploy\iis\2-deploy-site.ps1`: stops the app pool, copies the files, sets permissions, starts the site |
| Health check | VM | `http://localhost/health` must answer `{"status":"UP"}` |
| Rollback | VM | Only if the health check fails: restores the backup automatically |

Pull requests only build and scan; they never deploy.

## HTTPS with your domain
1. Create a DNS **A record**, for example `academy.inubvision.com`, pointing to the VM's public IP.
2. RDP to the VM, download **win-acme** from https://www.win-acme.com, and run `wacs.exe`.
3. Choose *N: Create certificate (default settings)*, then pick the **AcademyNet** site. win-acme adds the HTTPS binding and renews the certificate automatically.
4. Set your Razorpay webhook URL to `https://academy.inubvision.com/api/razorpay/webhook`.

## First login and admins
- **First admin:** the admin account is created on the site's first start from the email and password you gave `configure-vm.ps1`.
- **More admins:** to give admin access to another email (for example your personal one), register it on the site, then go to **Admin → Students → Make admin**.

## Backups and operations
- **Database and notes:** back up `C:\inetpub\AcademyNet\App_Data`. Easiest is **Azure Backup** for the whole VM (Portal → VM → Backup).
- **Logs:** to see startup errors, set `stdoutLogEnabled="true"` in `C:\inetpub\AcademyNet\web.config` and read `logs\stdout*.log`. Also check Event Viewer → Windows Logs → Application.
- **Change settings:** rerun `configure-vm.ps1`. The site restarts automatically.
- **Windows updates:** enable *Azure Update Manager* on the VM, or patch it monthly.

| Problem | Fix |
|---|---|
| Deploy job stuck at *"Waiting for a runner"* | The runner is offline: check the **actions.runner.*** service on the VM, or rerun `install-runner.ps1` with a new token |
| HTTP 500.19 | The Hosting Bundle is missing: rerun `deploy\iis\1-setup-iis.ps1` on the VM |
| HTTP 500.30 | The app failed to start: enable stdout logs (above). Often a mistake in `appsettings.Production.json` |
| Health check failed → rolled back | Read the deploy job log and the stdout log; fix and push again |
| Site works on the VM but not from outside | Ports 80/443 in the Azure NSG (open by `create-vm.ps1`) and Windows Firewall |

## Run locally
```powershell
dotnet run          # SQLite database in .\App_Data, admin from appsettings.json
```

## Project structure
```
├── .github/workflows/ci-cd.yml   # build → scan → deploy to IIS on the VM → health check → rollback
├── deploy/
│   ├── azure/
│   │   ├── create-vm.ps1         # VM + ports + IIS + Hosting Bundle (from your PC)
│   │   ├── configure-vm.ps1      # writes appsettings.Production.json on the VM
│   │   └── install-runner.ps1    # GitHub self-hosted runner on the VM
│   └── iis/
│       ├── 1-setup-iis.ps1       # IIS + .NET 8 Hosting Bundle (used by create-vm.ps1)
│       └── 2-deploy-site.ps1     # app pool + site + copy + permissions (used by the pipeline)
├── web.config                    # IIS: ASP.NET Core Module, in-process, 100 MB uploads
├── AcademyNet.csproj · Program.cs · appsettings.json
├── Data/ · Services/ · Pages/ · wwwroot/
```
