<#
  Installs a GitHub Actions self-hosted runner on the VM (label: iis-prod) so the pipeline can deploy to IIS.
  Run on YOUR PC. Needs a short-lived registration token:
    GitHub → repo → Settings → Actions → Runners → New self-hosted runner → copy the token after "--token"
    (or:  gh api -X POST repos/<owner>/<repo>/actions/runners/registration-token --jq .token )

    .\deploy\azure\install-runner.ps1 -Repo swamygopu/inubvision-academy-dotnet
#>
param(
    [Parameter(Mandatory)] [string]$Repo,            # owner/repo
    [string]$ResourceGroup = "rg-inubvision-academy-vm",
    [string]$VmName        = "vm-academy",
    [string]$Token         = ""
)
$ErrorActionPreference = "Stop"
if (-not $Token) { $Token = Read-Host "GitHub runner registration token" }

# Runs on the VM as SYSTEM
$remote = @"
`$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
`$dir = 'C:\actions-runner'
if (Test-Path "`$dir\.runner") { Write-Output 'Runner already configured - skipping.'; exit 0 }
New-Item -ItemType Directory -Force -Path `$dir | Out-Null
`$rel = Invoke-RestMethod -Uri 'https://api.github.com/repos/actions/runner/releases/latest' -Headers @{ 'User-Agent' = 'academy-setup' }
`$asset = `$rel.assets | Where-Object { `$_.name -like 'actions-runner-win-x64-*.zip' } | Select-Object -First 1
Invoke-WebRequest -Uri `$asset.browser_download_url -OutFile "`$dir\runner.zip" -UseBasicParsing
Expand-Archive -Path "`$dir\runner.zip" -DestinationPath `$dir -Force
Remove-Item "`$dir\runner.zip"
Set-Location `$dir
.\config.cmd --unattended --url 'https://github.com/$Repo' --token '$Token' --name '$VmName' --labels 'iis-prod' --work '_work' --runasservice --windowslogonaccount 'NT AUTHORITY\SYSTEM' --replace
Write-Output "Runner `$(`$rel.tag_name) installed and running as a Windows service."
"@
$tmp = New-TemporaryFile
Set-Content -Path $tmp -Value $remote -Encoding UTF8
try {
    az vm run-command invoke -g $ResourceGroup -n $VmName --command-id RunPowerShellScript --scripts "@$tmp" --query "value[].message" -o tsv
} finally { Remove-Item $tmp -Force }
Write-Host "✅ Check GitHub → Settings → Actions → Runners: '$VmName' should be Idle (label iis-prod)." -ForegroundColor Green
