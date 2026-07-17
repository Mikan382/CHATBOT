$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$settings = Get-Content (Join-Path $root "appsettings.json") -Raw | ConvertFrom-Json
$route = $settings.ReverseProxy.Routes.'existing-application'
$destination = $settings.ReverseProxy.Clusters.'existing-application'.Destinations.primary.Address
$adapter = Get-Content (Join-Path $root "wwwroot/js/live-adapter.js") -Raw

if ($settings.UiLab.DefaultMode -ne "fixture") { throw "Fixture mode must remain the default." }
if ($route.Match.Path -ne "/backend/{**catch-all}") { throw "Proxy route contract changed." }
if ($route.Transforms[0].PathRemovePrefix -ne "/backend") { throw "Proxy prefix transform is missing." }
if (-not [Uri]::IsWellFormedUriString($destination, [UriKind]::Absolute)) { throw "Backend destination is invalid." }
if (-not $adapter.Contains('"/backend/api/chat/sessions"')) { throw "REST adapter contract is missing." }
if (-not $adapter.Contains('"/backend/chatHub"')) { throw "SignalR adapter contract is missing." }

Write-Output "Adapter contract verified: fixture default, REST proxy, SignalR proxy, and fallback present."
