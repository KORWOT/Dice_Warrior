param([int]$Port=5087,[string]$DatabasePath=(Join-Path $PSScriptRoot 'data/authority.db'))
$ErrorActionPreference='Stop'
if($Port -lt 1024 -or $Port -gt 65535) { throw 'Port must be 1024..65535.' }
$resolvedDatabase=[IO.Path]::GetFullPath($DatabasePath)
Push-Location $PSScriptRoot
try {
 & dotnet run --project (Join-Path $PSScriptRoot 'DiceWarrior.MockServer/DiceWarrior.MockServer.csproj') -- --environment Development --urls ('http://127.0.0.1:'+$Port) --Mock:DatabasePath $resolvedDatabase
 $result=$LASTEXITCODE
} finally { Pop-Location }
exit $result
