param(
    [Parameter(Mandatory = $true)]
    [string]$HostName,

    [int]$Port = 5432,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [Parameter(Mandatory = $true)]
    [string]$Username,

    [Parameter(Mandatory = $true)]
    [SecureString]$Password
)

$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)

try {
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    $connectionString =
        "Host=$HostName;Port=$Port;Database=$Database;Username=$Username;Password=$plainPassword;SSL Mode=Prefer"

    $env:ConnectionStrings__PostgreSql = $connectionString
    Write-Host "ConnectionStrings__PostgreSql configured for this PowerShell session."
    Write-Host "Run .\scripts\run-etl.ps1 from the same terminal."
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    Remove-Variable plainPassword -ErrorAction SilentlyContinue
}
