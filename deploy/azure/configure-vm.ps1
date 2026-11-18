<#
  Writes the site's private settings on the VM:  C:\inetpub\AcademyNet\appsettings.Production.json
  (admin login, Razorpay keys, optional Azure SQL). Run on YOUR PC; nothing is stored in GitHub.

    .\deploy\azure\configure-vm.ps1
    .\deploy\azure\configure-vm.ps1 -SqlConnection "Server=tcp:...;Database=academy;User ID=...;Password=...;Encrypt=True;"
#>
param(
    [string]$ResourceGroup = "rg-inubvision-academy-vm",
    [string]$VmName        = "vm-academy",
    [string]$AdminEmail    = "admin@inubvision.com",
    [string]$RazorpayKeyId = "",
    [string]$SqlConnection = ""          # empty = SQLite file in App_Data (fine for one VM)
)
$ErrorActionPreference = "Stop"

$adminPass = (Get-Credential -UserName $AdminEmail -Message "Website admin password (8+ chars, include a number)").GetNetworkCredential().Password
$rzpSecret = ""
$rzpWebhook = ""
if ($RazorpayKeyId) {
    $rzpSecret  = Read-Host "Razorpay Key Secret"
    $rzpWebhook = Read-Host "Razorpay Webhook Secret (optional, press Enter to skip)"
}

$settings = [ordered]@{
    Admin    = [ordered]@{ Email = $AdminEmail; Password = $adminPass }
    Razorpay = [ordered]@{ KeyId = $RazorpayKeyId; KeySecret = $rzpSecret; WebhookSecret = $rzpWebhook }
}
if ($SqlConnection) {
    $settings.Database = @{ Provider = "SqlServer" }
    $settings.ConnectionStrings = @{ Default = $SqlConnection }
}
$json = $settings | ConvertTo-Json -Depth 5
$b64  = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))

# Runs on the VM as SYSTEM: write the file, lock it down to Administrators + the site's app pool
$remote = @"
Import-Module WebAdministration
# create the app pool now (if the site isn't deployed yet) so its identity can be granted read access
if (-not (Test-Path 'IIS:\AppPools\AcademyNet')) { New-WebAppPool -Name AcademyNet | Out-Null; Set-ItemProperty 'IIS:\AppPools\AcademyNet' -Name managedRuntimeVersion -Value '' }
`$dir = 'C:\inetpub\AcademyNet'
New-Item -ItemType Directory -Force -Path `$dir | Out-Null
`$file = Join-Path `$dir 'appsettings.Production.json'
[IO.File]::WriteAllText(`$file, [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('$b64')))
icacls `$file /inheritance:r /grant:r 'Administrators:F' 'SYSTEM:F' 'IIS AppPool\AcademyNet:R' | Out-Null
if ((Get-WebAppPoolState -Name AcademyNet).Value -eq 'Started') { Restart-WebAppPool -Name AcademyNet }
Write-Output "Saved `$file"
"@
$tmp = New-TemporaryFile
Set-Content -Path $tmp -Value $remote -Encoding UTF8
try {
    az vm run-command invoke -g $ResourceGroup -n $VmName --command-id RunPowerShellScript --scripts "@$tmp" --query "value[].message" -o tsv
} finally { Remove-Item $tmp -Force }
Write-Host "✅ Settings saved on the VM. The admin account is created on the site's first start." -ForegroundColor Green
