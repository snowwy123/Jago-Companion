param(
 [Parameter(Mandatory=$true)][string]$Output,
 [Parameter(Mandatory=$true)][string]$WorkscapeRepository,
 [Parameter(Mandatory=$true)][string]$InstallerCompiler,
 [string]$LibraryRepository
)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $Output){throw 'Choose a new output folder; preview packages are never overwritten.'}
foreach($repo in @($PSScriptRoot,$WorkscapeRepository,$LibraryRepository) | Where-Object {$_}){
 $changes=& git -C $repo status --porcelain
 if($LASTEXITCODE -ne 0 -or $changes){throw "Commit source changes before packaging: $repo"}
}
& (Join-Path $PSScriptRoot 'Build.ps1')
$portable=Join-Path $Output 'Portable'
New-Item -ItemType Directory -Path $portable -Force | Out-Null
foreach($name in @('Jago-Companion.exe','Jago-Capture.exe','Jago-Recorder.exe','Jago-Preview.exe','Jago-Update.exe','ScreenRecorderLib.dll','Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll','README.md','LICENSE.txt')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $portable}
foreach($folder in @('Documentation','Brand','Notices')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $folder) -Destination $portable -Recurse}
Compress-Archive -LiteralPath $portable -DestinationPath (Join-Path $Output 'Jago-Companion-v0.7.3-Preview.zip')
$bundle=Join-Path $Output 'Workscape-with-Companion'
New-Item -ItemType Directory -Path $bundle | Out-Null
Copy-Item -LiteralPath $portable -Destination (Join-Path $bundle 'Companion') -Recurse
Copy-Item -LiteralPath (Join-Path $WorkscapeRepository 'Jago-Workscape.exe') -Destination $bundle
Copy-Item -LiteralPath (Join-Path $WorkscapeRepository 'Documentation') -Destination $bundle -Recurse
Copy-Item -LiteralPath (Join-Path $WorkscapeRepository 'LICENSE.txt') -Destination (Join-Path $bundle 'Workscape-LICENSE.txt')
@'
# Workscape + Companion preview

Open Jago-Workscape.exe, then use Companion > Open as a tab, Show narrow side panel, or Float over your work. A narrow panel needs another studio tab open first. Drag the divider to adjust its width.

This small preview includes Companion and Workscape. Use Manage studios to select your existing extracted studios if they are not already discovered. It is not a full suite download.

Open Companion/Jago-Capture.exe for one-click region capture, or Companion/Jago-Companion.exe for independent notes. Closing a Companion tab returns it to the tray; Quit from the tray exits fully.
'@ | Set-Content -LiteralPath (Join-Path $bundle 'README.md') -Encoding UTF8

if($LibraryRepository){
 $library=Join-Path $bundle 'Library-Production'
 New-Item -ItemType Directory -Path $library | Out-Null
 foreach($name in @('Jago-Library.exe','Jago-Library.html','README.md','LICENSE.txt')){Copy-Item -LiteralPath (Join-Path $LibraryRepository $name) -Destination $library}
 foreach($name in @('Documentation','Notices')){Copy-Item -LiteralPath (Join-Path $LibraryRepository $name) -Destination $library -Recurse}
 Add-Content -LiteralPath (Join-Path $bundle 'README.md') -Value "`nLibrary 0.10.1 preview adds Captures navigation and a shared capture folder. Open Library-Production/Jago-Library.exe or choose it from Workscape. This lightweight bundle includes Library's core browser/PDF tools; the larger optional Office, OCR and document conversion engines remain in your existing full Library installation and can be connected separately. Companion's installer updates Companion only."
 & git -C $LibraryRepository archive --format=zip ('--output='+(Join-Path $Output 'Jago-Library-Capture-Inbox-Source.zip')) HEAD
 if($LASTEXITCODE -ne 0){throw 'Library source archive failed'}
 Compress-Archive -LiteralPath $library -DestinationPath (Join-Path $Output 'Jago-Library-Capture-Inbox-Preview.zip')
}
Compress-Archive -LiteralPath $bundle -DestinationPath (Join-Path $Output 'Jago-Workscape-Companion-Preview.zip')
& git -C $PSScriptRoot archive --format=zip ('--output='+(Join-Path $Output 'Jago-Companion-Source.zip')) HEAD
if($LASTEXITCODE -ne 0){throw 'Source archive failed'}
& git -C $WorkscapeRepository archive --format=zip ('--output='+(Join-Path $Output 'Jago-Workscape-Preview-Source.zip')) HEAD
if($LASTEXITCODE -ne 0){throw 'Workscape source archive failed'}
& $InstallerCompiler /Q ('/DPayload='+$portable) ('/DOutput='+$Output) (Join-Path $PSScriptRoot 'Installer\Companion.iss')
if($LASTEXITCODE -ne 0){throw 'Installer build failed'}
$manifest=[ordered]@{channel='development-preview';version='0.7.3';companionCommit=(& git -C $PSScriptRoot rev-parse HEAD);workscapeCommit=(& git -C $WorkscapeRepository rev-parse HEAD);libraryCommit=$(if($LibraryRepository){& git -C $LibraryRepository rev-parse HEAD}else{$null});files=@{}}
Get-ChildItem -LiteralPath $Output -File | Where-Object Extension -In '.exe','.zip' | ForEach-Object {$manifest.files[$_.Name]=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $Output 'Build-manifest.json') -Encoding UTF8
Write-Output "Preview ready: $Output"
