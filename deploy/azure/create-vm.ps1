<#
  Creates the Azure Windows VM for iNubVision Academy and installs IIS + the .NET 8 Hosting Bundle on it.
  Run on YOUR PC in PowerShell (needs the Azure CLI:  winget install Microsoft.AzureCLI ; az login).

    .\deploy\azure\create-vm.ps1                       # uses the defaults below
    .\deploy\azure\create-vm.ps1 -VmName vm-academy2   # override any parameter
#>
param(
    [string]$ResourceGroup = "rg-inubvision-academy-vm",
    [string]$Location      = "centralindia",
    [string]$VmName        = "vm-academy",
    [string]$Size          = "Standard_B2s",          # 2 vCPU / 4 GB — enough to start
    [string]$AdminUser     = "azureadmin",
    [string]$MyIp          = ""                       # your public IP to restrict RDP, e.g. 49.37.10.20 (empty = any)
)
$ErrorActionPreference = "Stop"
$setupScript = Join-Path $PSScriptRoot "..\iis\1-setup-iis.ps1" | Resolve-Path

$cred = Get-Credential -UserName $AdminUser -Message "Choose the VM administrator password (12+ chars, upper/lower/number/symbol)"
$pass = $cred.GetNetworkCredential().Password

Write-Host "▶ Resource group $ResourceGroup ($Location)" -ForegroundColor Cyan
az group create -n $ResourceGroup -l $Location -o none

Write-Host "▶ Windows Server 2022 VM $VmName ($Size) — takes a few minutes" -ForegroundColor Cyan
az vm create -g $ResourceGroup -n $VmName --image Win2022Datacenter --size $Size `
    --admin-username $AdminUser --admin-password $pass `
    --public-ip-sku Standard --public-ip-address-allocation static -o none

Write-Host "▶ Opening ports 80 and 443" -ForegroundColor Cyan
az vm open-port -g $ResourceGroup -n $VmName --port 80  --priority 1010 -o none
az vm open-port -g $ResourceGroup -n $VmName --port 443 --priority 1020 -o none

if ($MyIp) {
    Write-Host "▶ Restricting RDP (3389) to $MyIp" -ForegroundColor Cyan
    $nsg = az network nsg list -g $ResourceGroup --query "[0].name" -o tsv
    $rdp = az network nsg rule list -g $ResourceGroup --nsg-name $nsg --query "[?destinationPortRange=='3389'].name | [0]" -o tsv
    if ($rdp) { az network nsg rule update -g $ResourceGroup --nsg-name $nsg -n $rdp --source-address-prefixes $MyIp -o none }
}

Write-Host "▶ Installing IIS + .NET 8 Hosting Bundle on the VM (no RDP needed)" -ForegroundColor Cyan
az vm run-command invoke -g $ResourceGroup -n $VmName --command-id RunPowerShellScript --scripts "@$setupScript" --query "value[].message" -o tsv

$ip = az vm show -d -g $ResourceGroup -n $VmName --query publicIps -o tsv
Write-Host ""
Write-Host "✅ VM ready: $ip" -ForegroundColor Green
Write-Host "Next:"
Write-Host "  1) .\deploy\azure\configure-vm.ps1 -ResourceGroup $ResourceGroup -VmName $VmName      (admin login + Razorpay keys)"
Write-Host "  2) .\deploy\azure\install-runner.ps1 -ResourceGroup $ResourceGroup -VmName $VmName -Repo <owner>/<repo>"
Write-Host "  3) git push to main  →  GitHub Actions deploys the site  →  http://$ip"
