# 实测 WordFieldUpdater 的 .NET 可维护性指标，并运行不依赖 Office 的回归测试。
# 示例：./scripts/Test-WordFieldUpdater.ps1 -Configuration Release
# 加 -WithWord 可在本机 Word 上验证并发替换、长路径、FILENAME 域与只读文件。
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [ValidateRange(0, 100)][int]$MinimumMaintainability = 85,
    [string]$SourcePath,
    [switch]$SkipRegression,
    [switch]$WithWord,
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
if ($WithWord -and $SkipRegression) { throw '-WithWord 和 -SkipRegression 不能同时使用。' }
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repoRoot 'PersonalTools.csproj'
if (!$SourcePath) { $SourcePath = Join-Path $repoRoot 'MarkdownToWord/WordFieldUpdater.cs' }
$SourcePath = (Resolve-Path -LiteralPath $SourcePath).Path
$testDir = Join-Path ([IO.Path]::GetTempPath()) ('PersonTools-WordFields-' + [Guid]::NewGuid().ToString('N'))
$utf8 = [Text.UTF8Encoding]::new($false)
New-Item -ItemType Directory -Path $testDir | Out-Null
if (!$SkipBuild) {
    dotnet build $projectPath -c $Configuration -v:q
    if ($LASTEXITCODE -ne 0) { throw '主项目编译失败。' }
}
$inputs = dotnet msbuild $projectPath -t:Compile "-p:Configuration=$Configuration" '-getItem:Compile,ReferencePath' '-getProperty:DefineConstants,MSBuildToolsPath,TargetFramework' -v:q
if ($LASTEXITCODE -ne 0) { throw '无法读取编译输入。' }
$inputPath = Join-Path $testDir 'inputs.json'
[IO.File]::WriteAllText($inputPath, ($inputs -join [Environment]::NewLine), $utf8)
$properties = ($inputs -join [Environment]::NewLine | ConvertFrom-Json).Properties
$roslyn = Join-Path $properties.MSBuildToolsPath 'Roslyn/bincore'
$assets = Get-Content -LiteralPath (Join-Path $repoRoot 'obj/project.assets.json') -Raw | ConvertFrom-Json
$package = $assets.libraries.PSObject.Properties | Where-Object { $_.Name -like 'Microsoft.CodeAnalysis.NetAnalyzers/*' } | Select-Object -First 1
if ($null -eq $package) { throw '未还原代码指标依赖。' }
$analyzer = $null
foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
    $candidate = Join-Path $folder ($package.Value.path + '/analyzers/dotnet/Microsoft.CodeAnalysis.NetAnalyzers.dll')
    if (Test-Path -LiteralPath $candidate) { $analyzer = $candidate; break }
}
if ($null -eq $analyzer) { throw '找不到代码指标程序集。' }
$escapedRoslyn = [Security.SecurityElement]::Escape($roslyn)
$escapedAnalyzer = [Security.SecurityElement]::Escape($analyzer)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>$($properties.TargetFramework)</TargetFramework>
    <UseWPF>true</UseWPF><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Microsoft.CodeAnalysis"><HintPath>$escapedRoslyn/Microsoft.CodeAnalysis.dll</HintPath></Reference>
    <Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$escapedRoslyn/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference>
    <Reference Include="Microsoft.CodeAnalysis.NetAnalyzers"><HintPath>$escapedAnalyzer</HintPath></Reference>
  </ItemGroup>
</Project>
"@
[IO.File]::WriteAllText((Join-Path $testDir 'Metrics.csproj'), $project, $utf8)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'WordFieldUpdater.Metrics.cs.txt') -Destination (Join-Path $testDir 'Program.cs')
$reportPath = Join-Path $testDir 'metrics.json'
$runArgs = @($inputPath, $SourcePath, "$MinimumMaintainability", $reportPath)
if (!$SkipRegression) { $runArgs += (Join-Path $PSScriptRoot 'WordFieldUpdater.Regression.cs.txt') }
if ($WithWord) { $runArgs += (Join-Path $PSScriptRoot 'WordFieldUpdater.Office.cs.txt') }
dotnet run --project (Join-Path $testDir 'Metrics.csproj') -c $Configuration -- @runArgs
if ($LASTEXITCODE -ne 0) { throw "指标或回归检查失败；报告目录：$testDir" }
Write-Host "报告：$reportPath"
