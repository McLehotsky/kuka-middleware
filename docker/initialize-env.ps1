param([string]$Path = (Join-Path $PSScriptRoot '.env'))

$ErrorActionPreference = 'Stop'

function New-HexSecret([int]$Bytes) {
    $buffer = New-Object byte[] $Bytes
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generator.GetBytes($buffer) } finally { $generator.Dispose() }
    return -join ($buffer | ForEach-Object { $_.ToString('x2') })
}

if (Test-Path -LiteralPath $Path) {
    $content = [IO.File]::ReadAllText($Path)
} else {
    $content = [IO.File]::ReadAllText((Join-Path $PSScriptRoot '.env.example'))
    $content = $content -replace '(?m)^POSTGRES_PASSWORD=change-me\r?$', ('POSTGRES_PASSWORD=' + (New-HexSecret 24))
}

foreach ($name in @('WEB_UI_API_KEY', 'EXTERNAL_SYSTEM_API_KEY')) {
    $matchesForKey = [regex]::Matches($content, ('(?m)^' + $name + '=([^\r\n]*)'))
    if ($matchesForKey.Count -gt 1) { throw "Duplicate setting: $name" }
    if ($matchesForKey.Count -eq 0) {
        $content = $content.TrimEnd("`r", "`n") + "`n$name=" + (New-HexSecret 32) + "`n"
    } elseif ($matchesForKey[0].Groups[1].Value -in @('', 'change-me')) {
        $content = [regex]::Replace($content, ('(?m)^' + $name + '=[^\r\n]*'), ($name + '=' + (New-HexSecret 32)))
    } elseif ($matchesForKey[0].Groups[1].Value -cnotmatch '^[0-9a-f]{64}$') {
        throw "$name must contain exactly 64 lowercase hexadecimal characters."
    }
}

$webKey = [regex]::Match($content, '(?m)^WEB_UI_API_KEY=([^\r\n]*)').Groups[1].Value
$externalKey = [regex]::Match($content, '(?m)^EXTERNAL_SYSTEM_API_KEY=([^\r\n]*)').Groups[1].Value
if ($webKey -eq $externalKey) { throw 'API clients must have different keys.' }

[IO.File]::WriteAllText($Path, ($content -replace "`r`n", "`n"), (New-Object Text.UTF8Encoding $false))
Write-Output 'Local environment ready; existing database password and configured API keys preserved.'
