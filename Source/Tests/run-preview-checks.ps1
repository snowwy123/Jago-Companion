param([Parameter(Mandatory=$true)][string]$Output)
$ErrorActionPreference='Stop'
$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
New-Item -ItemType Directory -Force -Path $Output | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
$files=@(Get-ChildItem -LiteralPath (Join-Path $repo 'Source') -Filter '*.cs' | ForEach-Object FullName)
foreach($name in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll')){Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $Output -Force}
& $compiler /nologo /target:winexe /platform:x64 /main:QuickPreviewTests ('/out:'+(Join-Path $Output 'QuickPreviewTests.exe')) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.Web.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Xaml.dll ('/reference:'+(Join-Path $wpf 'PresentationFramework.dll')) ('/reference:'+(Join-Path $wpf 'PresentationCore.dll')) ('/reference:'+(Join-Path $wpf 'WindowsBase.dll')) ('/reference:'+(Join-Path $wpf 'WindowsFormsIntegration.dll')) ('/reference:'+(Join-Path $repo 'Microsoft.Web.WebView2.Core.dll')) ('/reference:'+(Join-Path $repo 'Microsoft.Web.WebView2.WinForms.dll')) $files (Join-Path $repo 'VideoPreview.cs') (Join-Path $repo 'PreviewSeeking.cs') (Join-Path $repo 'QuickFilePreview.cs') (Join-Path $repo 'QuickDocumentPreview.cs') (Join-Path $PSScriptRoot 'QuickPreviewTests.cs')
if($LASTEXITCODE -ne 0){throw 'Preview test build failed'}
$process=Start-Process -FilePath (Join-Path $Output 'QuickPreviewTests.exe') -ArgumentList ('"'+$Output+'"') -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(60000)){$process.Kill();throw 'Preview checks timed out'}
$result=Get-Content -LiteralPath (Join-Path $Output 'result.json') -Raw | ConvertFrom-Json
$result | ConvertTo-Json -Depth 5
if(!$result.ok){throw 'Preview checks failed'}

& $compiler /nologo /target:exe ('/out:'+(Join-Path $Output 'PreviewSeekingTests.exe')) (Join-Path $repo 'PreviewSeeking.cs') (Join-Path $PSScriptRoot 'PreviewSeekingTests.cs')
if($LASTEXITCODE -ne 0){throw 'Seek test build failed'}
& (Join-Path $Output 'PreviewSeekingTests.exe')
if($LASTEXITCODE -ne 0){throw 'Seek state tests failed'}
if(Test-Path -LiteralPath (Join-Path $Output 'seek-fixture.mp4')){
& $compiler /nologo /target:winexe /platform:x64 /main:PreviewPlaybackTests ('/out:'+(Join-Path $Output 'PreviewPlaybackTests.exe')) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.Web.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Xaml.dll ('/reference:'+(Join-Path $wpf 'PresentationFramework.dll')) ('/reference:'+(Join-Path $wpf 'PresentationCore.dll')) ('/reference:'+(Join-Path $wpf 'WindowsBase.dll')) ('/reference:'+(Join-Path $wpf 'WindowsFormsIntegration.dll')) ('/reference:'+(Join-Path $repo 'Microsoft.Web.WebView2.Core.dll')) ('/reference:'+(Join-Path $repo 'Microsoft.Web.WebView2.WinForms.dll')) $files (Join-Path $repo 'VideoPreview.cs') (Join-Path $repo 'PreviewSeeking.cs') (Join-Path $repo 'QuickFilePreview.cs') (Join-Path $repo 'QuickDocumentPreview.cs') (Join-Path $PSScriptRoot 'PreviewPlaybackTests.cs')
if($LASTEXITCODE -ne 0){throw 'Playback test build failed'}
$playback=Join-Path $Output 'Playback'
New-Item -ItemType Directory -Path $playback -Force | Out-Null
$process=Start-Process -FilePath (Join-Path $Output 'PreviewPlaybackTests.exe') -ArgumentList ('"'+(Join-Path $Output 'seek-fixture.mp4')+'" "'+$playback+'"') -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(45000)){$process.Kill();throw 'Playback check timed out'}
$seek=Get-Content -LiteralPath (Join-Path $playback 'seek-result.json') -Raw | ConvertFrom-Json
$seek | ConvertTo-Json
if(!$seek.ok){throw 'Native backward seeking failed'}
}

if(Test-Path -LiteralPath (Join-Path $Output 'seek-fixture.mp4')){
& $compiler /nologo /target:winexe /platform:x64 /main:PreviewLoopTests ('/out:'+(Join-Path $Output 'PreviewLoopTests.exe')) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll /reference:System.Web.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll /reference:System.Xaml.dll ('/reference:'+(Join-Path $wpf 'PresentationFramework.dll')) ('/reference:'+(Join-Path $wpf 'PresentationCore.dll')) ('/reference:'+(Join-Path $wpf 'WindowsBase.dll')) ('/reference:'+(Join-Path $wpf 'WindowsFormsIntegration.dll')) ('/reference:'+(Join-Path $repo 'Microsoft.Web.WebView2.Core.dll')) ('/reference:'+(Join-Path $repo 'Microsoft.Web.WebView2.WinForms.dll')) $files (Join-Path $repo 'VideoPreview.cs') (Join-Path $repo 'PreviewSeeking.cs') (Join-Path $repo 'QuickFilePreview.cs') (Join-Path $repo 'QuickDocumentPreview.cs') (Join-Path $PSScriptRoot 'PreviewLoopTests.cs')
if($LASTEXITCODE -ne 0){throw 'Loop test build failed'}
$loopOutput=Join-Path $Output 'Loop'
New-Item -ItemType Directory -Path $loopOutput -Force | Out-Null
$process=Start-Process -FilePath (Join-Path $Output 'PreviewLoopTests.exe') -ArgumentList ('"'+(Join-Path $Output 'seek-fixture.mp4')+'" "'+$loopOutput+'"') -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(45000)){$process.Kill();throw 'Loop checks timed out'}
$loops=Get-Content -LiteralPath (Join-Path $loopOutput 'loop-result.json') -Raw | ConvertFrom-Json
$loops | ConvertTo-Json
if(!$loops.ok){throw 'Loop/replay checks failed'}
}
