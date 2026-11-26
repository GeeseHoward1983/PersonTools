# PE 头描述回归与真实 Roslyn/.NET 代码指标检查；产物仅写入系统临时目录。
# ./scripts/Test-PEHeaderDescriptions.ps1 -Configuration Release
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [ValidateRange(0, 100)]
    [int]$MinimumMaintainability = 85,
    [switch]$SkipMainBuild,
    [switch]$MetricsOnly
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$mainProject = Join-Path $repoRoot 'PersonalTools.csproj'
if (-not $SkipMainBuild) {
    dotnet build $mainProject -c $Configuration -v:q
    if ($LASTEXITCODE -ne 0) { throw '主项目编译失败。' }
}
$properties = (dotnet msbuild $mainProject -getProperty:MSBuildToolsPath,TargetFramework -v:q | ConvertFrom-Json).Properties
if ($LASTEXITCODE -ne 0) { throw '无法读取 SDK 路径。' }
$roslyn = Join-Path $properties.MSBuildToolsPath 'Roslyn/bincore'
$assets = Get-Content -LiteralPath (Join-Path $repoRoot 'obj/project.assets.json') -Raw | ConvertFrom-Json
$package = $assets.libraries.PSObject.Properties | Where-Object { $_.Name -like 'Microsoft.CodeAnalysis.NetAnalyzers/*' } | Select-Object -First 1
if ($null -eq $package) { throw '请先还原主项目的 Microsoft.CodeAnalysis.NetAnalyzers。' }
$analyzer = $null
foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
    $candidate = Join-Path $folder ($package.Value.path + '/analyzers/dotnet/Microsoft.CodeAnalysis.NetAnalyzers.dll')
    if (Test-Path -LiteralPath $candidate) { $analyzer = $candidate; break }
}
if ($null -eq $analyzer) { throw '找不到 .NET 代码指标程序集。' }
$testDir = Join-Path ([IO.Path]::GetTempPath()) ('PersonTools-PEHeader-Regression-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDir | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
$escapedRoslyn = [Security.SecurityElement]::Escape($roslyn)
$escapedAnalyzer = [Security.SecurityElement]::Escape($analyzer)
$descriptionPath = Join-Path $repoRoot 'PEAnalyzer/PEHeaderDescriptions.cs'
$escapedSource = [Security.SecurityElement]::Escape($descriptionPath)
$escapedCatalog = [Security.SecurityElement]::Escape((Join-Path $repoRoot 'PEAnalyzer/PEHeaderDescriptionCatalog.cs'))
$escapedFixture = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'fixtures/PEHeaderDescriptionsRegression.cs.txt'))
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Microsoft.CodeAnalysis"><HintPath>$escapedRoslyn/Microsoft.CodeAnalysis.dll</HintPath></Reference>
    <Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$escapedRoslyn/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference>
    <Reference Include="Microsoft.CodeAnalysis.NetAnalyzers"><HintPath>$escapedAnalyzer</HintPath></Reference>
    <Compile Include="$escapedSource" Link="PEHeaderDescriptions.cs" />
    <Compile Include="$escapedCatalog" Link="PEHeaderDescriptionCatalog.cs" />
    <Compile Include="$escapedFixture" Link="PEHeaderDescriptionsRegression.cs" />
  </ItemGroup>
</Project>
"@
$source = @'
using System.Globalization;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeMetrics;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var source = CSharpSyntaxTree.ParseText(File.ReadAllText(args[0]), path: args[0]);
        var catalogPath = Path.Combine(Path.GetDirectoryName(args[0])!, "PEHeaderDescriptionCatalog.cs");
        var catalog = CSharpSyntaxTree.ParseText(File.ReadAllText(catalogPath), path: catalogPath);
        var usings = CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; global using System.Linq;");
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(p => MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create("PEHeaderMetrics", [source, catalog, usings], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
        var reports = new List<CodeAnalysisMetricData>();
        foreach (string typeName in new[] { "PEHeaderDescriptions", "PEHeaderDescriptionCatalog" })
        {
            var type = compilation.GetTypeByMetadataName("PersonalTools.PEAnalyzer." + typeName)!;
            foreach (var declaration in FlattenTypes(type))
                reports.Add(await CodeAnalysisMetricData.ComputeAsync(declaration, new CodeMetricsAnalysisContext(compilation, default)));
        }
        var members = reports.SelectMany(FlattenMetrics).DistinctBy(m => m.Symbol, SymbolEqualityComparer.Default).ToArray();
        var methods = members.Where(m => m.Symbol is IMethodSymbol).ToArray();
        var fields = members.Where(m => m.Symbol is IFieldSymbol).ToArray();
        var types = members.Where(m => m.Symbol is INamedTypeSymbol).ToArray();
        var publicMethods = methods.Where(m => m.Symbol.DeclaredAccessibility == Accessibility.Public).ToArray();
        var minimum = int.Parse(args[1], CultureInfo.InvariantCulture);
        File.WriteAllText(args[2], JsonSerializer.Serialize(new
        {
            Engine = "Microsoft.CodeAnalysis.NetAnalyzers / CodeAnalysisMetricData",
            MinimumMaintainability = minimum,
            PublicMethodMinimum = publicMethods.Min(m => m.MaintainabilityIndex),
            AllMethodMinimum = methods.Min(m => m.MaintainabilityIndex),
            AllFieldMinimum = fields.Min(m => m.MaintainabilityIndex),
            AllTypeMinimum = types.Min(m => m.MaintainabilityIndex),
            AllMemberMinimum = members.Min(m => m.MaintainabilityIndex),
            ClassMaintainabilityIndex = reports[0].MaintainabilityIndex,
            Types = types.Select(m => new { Name = m.Symbol.ToDisplayString(), m.MaintainabilityIndex }),
            Members = members.Select(m => new { Name = m.Symbol.ToDisplayString(), Kind = m.Symbol.Kind.ToString(), m.MaintainabilityIndex, m.CyclomaticComplexity, m.ExecutableLines })
        }, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var member in members.Where(m => m.Symbol.ContainingType?.TypeKind != TypeKind.Enum))
            Console.WriteLine($"MI={member.MaintainabilityIndex,3} CC={member.CyclomaticComplexity,2} {member.Symbol.ToDisplayString()}");
        if (members.Length == 0 || members.Any(m => m.MaintainabilityIndex < minimum))
            throw new InvalidOperationException($"All members, including fields and types, must be >= {minimum}; see {args[2]}");
        if (args[3] != "True") PEHeaderDescriptionsRegression.Verify();
        Console.WriteLine($"PASS {methods.Length} methods, {fields.Length} fields, {types.Length} types >= {minimum}; class MI={reports[0].MaintainabilityIndex}");
        return 0;
    }

    private static IEnumerable<INamedTypeSymbol> FlattenTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers())
            foreach (var descendant in FlattenTypes(nested))
                yield return descendant;
    }

    private static IEnumerable<CodeAnalysisMetricData> FlattenMetrics(CodeAnalysisMetricData node)
    {
        yield return node;
        foreach (var child in node.Children)
            foreach (var descendant in FlattenMetrics(child))
                yield return descendant;
    }
}
'@
[IO.File]::WriteAllText((Join-Path $testDir 'Regression.csproj'), $project, $utf8)
[IO.File]::WriteAllText((Join-Path $testDir 'Program.cs'), $source, $utf8)
$reportPath = Join-Path $testDir 'PEHeaderDescriptions.metrics.json'
Write-Host "Metrics report: $reportPath"
dotnet run --project (Join-Path $testDir 'Regression.csproj') -c $Configuration -v:q -- $descriptionPath $MinimumMaintainability $reportPath $MetricsOnly.IsPresent
if ($LASTEXITCODE -ne 0) { throw 'PE 头描述回归或可维护性检查失败。' }
