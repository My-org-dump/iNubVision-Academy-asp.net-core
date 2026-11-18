# Run on the Azure Windows VM, in PowerShell as Administrator.
# Creates an app pool + IIS site and copies the published files into place.
# Usage:  .\2-deploy-site.ps1 -SourcePath C:\temp\publish
# Your secrets live in C:\inetpub\AcademyNet\appsettings.Production.json on the server.
# That file is not part of the published output, so redeploying never overwrites it.

param(
    [string]$SourcePath = "C:\temp\publish",
    [string]$SiteName   = "AcademyNet",
    [string]$SitePath   = "C:\inetpub\AcademyNet",
    [int]$Port          = 80
)

$ErrorActionPreference = "Stop"
Import-Module WebAdministration

# Stop the default site so it doesn't hold port 80
if ((Get-Website -Name "Default Web Site" -ErrorAction SilentlyContinue) -and $Port -eq 80) {
    Stop-Website -Name "Default Web Site"
}

# App pool: ASP.NET Core does not use the .NET CLR, so set "No Managed Code"
if (-not (Test-Path "IIS:\AppPools\$SiteName")) {
    New-WebAppPool -Name $SiteName | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$SiteName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$SiteName" -Name startMode -Value "AlwaysRunning"
# Never idle-stop: the background job that saves live-class recordings must keep running
Set-ItemProperty "IIS:\AppPools\$SiteName" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)

# Stop the pool so files aren't locked during copy
if ((Get-WebAppPoolState -Name $SiteName).Value -ne "Stopped") {
    Stop-WebAppPool -Name $SiteName
    Start-Sleep -Seconds 3
}

# Copy files
New-Item -ItemType Directory -Force -Path $SitePath | Out-Null
New-Item -ItemType Directory -Force -Path "$SitePath\logs" | Out-Null
New-Item -ItemType Directory -Force -Path "$SitePath\App_Data" | Out-Null   # database + uploaded PDFs (kept between deploys)
Copy-Item -Path "$SourcePath\*" -Destination $SitePath -Recurse -Force

# Give the app pool identity read access (and write to logs)
icacls $SitePath /grant "IIS AppPool\${SiteName}:(OI)(CI)RX" | Out-Null
icacls "$SitePath\logs" /grant "IIS AppPool\${SiteName}:(OI)(CI)M" | Out-Null
icacls "$SitePath\App_Data" /grant "IIS AppPool\${SiteName}:(OI)(CI)M" | Out-Null

# Create site if missing
if (-not (Get-Website -Name $SiteName -ErrorAction SilentlyContinue)) {
    New-Website -Name $SiteName -PhysicalPath $SitePath -ApplicationPool $SiteName -Port $Port | Out-Null
}

Start-WebAppPool -Name $SiteName
Start-Website -Name $SiteName

Write-Host "Deployed! Browse to http://localhost:$Port or http://<VM-public-IP>:$Port" -ForegroundColor Green
