[CmdletBinding()]
param(
    [ValidateSet('Google', 'Facebook')]
    [string[]] $Provider = @('Google', 'Facebook')
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../src/CineBook.Api/CineBook.Api.csproj'
$keys = @{
    Google = @('ClientId', 'ClientSecret')
    Facebook = @('AppId', 'AppSecret')
}
$values = @{}

try {
    foreach ($name in ($Provider | Select-Object -Unique)) {
        foreach ($key in $keys[$name]) {
            $inputValue = Read-Host "$name $key" -AsSecureString
            try {
                $value = [System.Net.NetworkCredential]::new('', $inputValue).Password.Trim()
                if ([string]::IsNullOrWhiteSpace($value)) {
                    throw "$name $key is required."
                }
                $values["Authentication:${name}:$key"] = $value
            } finally {
                $value = $null
                $inputValue.Dispose()
            }
        }
    }

    $values | ConvertTo-Json -Compress | dotnet user-secrets set --project $project | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not save OAuth settings in .NET User Secrets.'
    }

    Write-Host 'Saved OAuth settings for CineBook.Api. Restart the API to test sign-in.'
} finally {
    $values.Clear()
}
