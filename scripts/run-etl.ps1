$projectRoot = Split-Path -Parent $PSScriptRoot

if (-not $env:ConnectionStrings__PostgreSql) {
    throw "Configure ConnectionStrings__PostgreSql first with .\scripts\configure-local.ps1"
}

Push-Location $projectRoot

try {
    dotnet restore .\FeedbackAnalyticsEtl.sln
    dotnet build .\FeedbackAnalyticsEtl.sln --configuration Release --no-restore
    dotnet run --project .\src\FeedbackAnalytics.Worker\FeedbackAnalytics.Worker.csproj `
        --configuration Release `
        --no-build
}
finally {
    Pop-Location
}
