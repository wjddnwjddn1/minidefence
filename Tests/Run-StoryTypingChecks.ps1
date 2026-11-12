$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $PSScriptRoot 'bin\StoryTypingChecks.exe'
& $compiler /nologo /utf8output /codepage:65001 "/out:$output" (Join-Path $root 'Assets\Scripts\Presentation\StoryTypewriter.cs') (Join-Path $PSScriptRoot 'StoryTypingChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Typing compilation failed'}
& $output
if($LASTEXITCODE -ne 0){throw 'Typing checks failed'}
