param(
    [ValidateRange(1024, 65535)]
    [int]$Port = 5088
)

$ErrorActionPreference = 'Stop'

# No SDK .NET 10, --environment recebe NOME=VALOR, não o nome do ambiente.
# Development carrega os User Secrets associados ao UserSecretsId do projeto.
& dotnet run --project (Join-Path $PSScriptRoot 'Chamapro.Api.csproj') `
    --no-launch-profile --environment ASPNETCORE_ENVIRONMENT=Development `
    -- --urls "http://localhost:$Port"
exit $LASTEXITCODE
