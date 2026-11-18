# Run ONCE on the Azure Windows VM, in PowerShell as Administrator.
# Installs IIS and the ASP.NET Core 8 Hosting Bundle, then restarts IIS.

$ErrorActionPreference = "Stop"

Write-Host "Installing IIS..." -ForegroundColor Cyan
Install-WindowsFeature -Name Web-Server, Web-WebSockets, Web-Mgmt-Console, Web-Mgmt-Service -IncludeManagementTools

Write-Host "Downloading ASP.NET Core 8 Hosting Bundle..." -ForegroundColor Cyan
$installer = "$env:TEMP\dotnet-hosting-8.exe"
# Official Microsoft permalink for the latest .NET 8 Hosting Bundle
Invoke-WebRequest -Uri "https://dotnet.microsoft.com/permalink/dotnetcore-8-windows-runtime-bundle-installer" -OutFile $installer

Write-Host "Installing Hosting Bundle..." -ForegroundColor Cyan
Start-Process -FilePath $installer -ArgumentList "/install", "/quiet", "/norestart" -Wait

Write-Host "Restarting IIS..." -ForegroundColor Cyan
net stop was /y
net start w3svc

Write-Host "Opening Windows Firewall for HTTP/HTTPS..." -ForegroundColor Cyan
New-NetFirewallRule -DisplayName "HTTP 80"   -Direction Inbound -Protocol TCP -LocalPort 80  -Action Allow -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName "HTTPS 443" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow -ErrorAction SilentlyContinue

Write-Host "Done. IIS + ASP.NET Core Hosting Bundle are ready." -ForegroundColor Green
