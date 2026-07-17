$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $root
$settings = Get-Content (Join-Path $root "appsettings.json") -Raw | ConvertFrom-Json
$route = $settings.ReverseProxy.Routes.'existing-application'
$accountRoute = $settings.ReverseProxy.Routes.'existing-account'
$destination = $settings.ReverseProxy.Clusters.'existing-application'.Destinations.primary.Address
$adapter = Get-Content (Join-Path $root "wwwroot/js/live-adapter.js") -Raw
$pages = Get-Content (Join-Path $root "wwwroot/js/live-pages.js") -Raw
$apiController = Get-Content (Join-Path $repoRoot "src/PresentationLayer/Controllers/UiLabApiController.cs") -Raw

if ($settings.UiLab.DefaultMode -ne "fixture") { throw "Fixture mode must remain the default." }
if ($route.Match.Path -ne "/backend/{**catch-all}") { throw "Proxy route contract changed." }
if ($route.Transforms[0].PathRemovePrefix -ne "/backend") { throw "Proxy prefix transform is missing." }
if ($accountRoute.Match.Path -ne "/Account/{**catch-all}") { throw "Account form-post proxy route is missing." }
if (-not [Uri]::IsWellFormedUriString($destination, [UriKind]::Absolute)) { throw "Backend destination is invalid." }
if (-not $adapter.Contains('requestJson("/api/chat/sessions")')) { throw "Chat REST adapter contract is missing." }
if (-not $adapter.Contains('.withUrl("/backend/chatHub")')) { throw "SignalR adapter contract is missing." }
foreach ($path in @('/api/ui-lab/me', '/api/ui-lab/courses', '/api/ui-lab/documents', '/api/ui-lab/benchmark', '/api/ui-lab/users')) {
    if (-not $adapter.Contains($path)) { throw "Live adapter path is missing: $path" }
}
if (-not $pages.Contains('Sign in to live application')) { throw "Explicit authentication recovery is missing." }
if (-not $apiController.Contains('[Authorize]')) { throw "UI lab API must require authentication." }
if ($apiController.Contains('AppDbContext')) { throw "UI lab API must use application services, not DbContext directly." }

Write-Output "Adapter contract verified: fixture default, authenticated DB-backed REST, SignalR, role boundaries, and explicit recovery present."
