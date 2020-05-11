#Requires -Version 7.0
using namespace System.IO

<#
    Local build script to perform build and test validation
#>
param(
    [ValidateSet('Debug', 'Release')]
    [String]$Configuration = 'Debug',
    [String]$ArtifactsPath = (Join-Path $PSScriptRoot '.artifacts')
)

function exec([string]$cmd) {
    $currentPref = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & $cmd @args
    $ErrorActionPreference = $currentPref
    if($LASTEXITCODE -ne 0) {
        Write-Error "[Error code $LASTEXITCODE] Command $cmd $args"
        exit $LASTEXITCODE
    }
}

$ErrorActionPreference = 'Stop'

# Clean
Remove-Item $ArtifactsPath -Recurse -Force -ErrorAction Ignore

# Restore Tools
dotnet tool restore

# Version
$versionDetails = exec dotnet simpleversion
$version = ($versionDetails | ConvertFrom-Json).Formats.Semver2
if ($env:TF_BUILD) { Write-Output "##vso[build.updatebuildnumber]$version" }
 
# Default Args
$dotnetArgs = @('--configuration', $Configuration, "/p:Version=$version")

# Build
exec dotnet build $dotnetArgs

# Test
$testArtifacts = Join-Path $ArtifactsPath 'tests'
exec dotnet test $dotnetArgs --results-directory $testArtifacts --no-build
exec dotnet reportgenerator "-reports:$(Join-Path $testArtifacts '**/*.cobertura.xml')" "-targetDir:$(Join-Path $testArtifacts 'CoverageReport')" "-reporttypes:HtmlInline_AzurePipelines"

# Pack
$distArtifacts = Join-Path $ArtifactsPath 'dist'
exec dotnet pack $dotnetArgs --output $distArtifacts --no-build
