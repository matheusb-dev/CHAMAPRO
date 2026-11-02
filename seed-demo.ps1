param(
    [switch]$Apply,
    [switch]$ConfigurePassword,
    [ValidatePattern('^[a-z0-9]{1,16}$')][string]$Batch = 'v1'
)
$ErrorActionPreference = 'Stop'
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousPassword = $env:CHAMAPRO_DEMO_PASSWORD
$seedArtifacts = Join-Path $env:TEMP 'chamapro-demo-build'
Push-Location $PSScriptRoot
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    & dotnet build Tests/Chamapro.DemoSeed/Chamapro.DemoSeed.csproj --artifacts-path $seedArtifacts -p:NuGetAudit=false --ignore-failed-sources
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível compilar o seed.' }
    $seedArguments = @('--batch', $Batch)
    if ($Apply) { $seedArguments += '--apply' }
    if ($ConfigurePassword) {
        $secure = Read-Host 'Senha local das contas fictícias deste lote (12 a 128 caracteres)' -AsSecureString
        $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
        try { $env:CHAMAPRO_DEMO_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
        finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer); $secure.Dispose() }
        $seedArguments += '--set-password'
    }
    & dotnet (Join-Path $seedArtifacts 'bin/Chamapro.DemoSeed/debug/Chamapro.DemoSeed.dll') @seedArguments
    if ($LASTEXITCODE -ne 0) { throw 'O seed foi bloqueado ou encontrou um erro. Nenhum esquema foi alterado.' }
}
finally {
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:CHAMAPRO_DEMO_PASSWORD = $previousPassword
    Pop-Location
}
