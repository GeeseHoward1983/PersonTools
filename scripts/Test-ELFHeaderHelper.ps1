# 编译、回归测试并检查 ELFHeaderHelper 的方法和类整体是否达到指定可维护性指数。
# 临时项目和报告写入系统临时目录，不参与主项目的默认 Compile 匹配。
# 运行：./scripts/Test-ELFHeaderHelper.ps1 -Configuration Release
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [ValidateRange(0, 100)]
    [int]$MinimumMaintainability = 80,
    [ValidateRange(0, 100)]
    [int]$MinimumClassMaintainability = 80,
    [switch]$IncludeUIHelper
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repoRoot 'PersonalTools.csproj'
$testDir = Join-Path ([IO.Path]::GetTempPath()) ('PersonTools-ELFHeader-Regression-' + [Guid]::NewGuid().ToString('N'))
$utf8 = [Text.UTF8Encoding]::new($false)
New-Item -ItemType Directory -Path $testDir | Out-Null

dotnet build $projectPath -c $Configuration -v:q
if ($LASTEXITCODE -ne 0) { throw '主项目编译失败。' }

# Use real references and defines; analyze ELF and its dependencies without WPF generators.
$inputs = dotnet msbuild $projectPath -t:Compile "-p:Configuration=$Configuration" '-getItem:Compile,ReferencePath' '-getProperty:DefineConstants,MSBuildToolsPath,TargetFramework' -v:q
if ($LASTEXITCODE -ne 0) { throw '无法读取编译输入。' }
$inputPath = Join-Path $testDir 'compilation.json'
[IO.File]::WriteAllText($inputPath, ($inputs -join [Environment]::NewLine), $utf8)
$properties = ($inputs -join [Environment]::NewLine | ConvertFrom-Json).Properties
$roslyn = Join-Path $properties.MSBuildToolsPath 'Roslyn/bincore'

# The Metrics.exe MSBuild loader cannot load the current SDK; use the same API from NetAnalyzers.
$assets = Get-Content -LiteralPath (Join-Path $repoRoot 'obj/project.assets.json') -Raw | ConvertFrom-Json
$analyzerPackage = $assets.libraries.PSObject.Properties | Where-Object { $_.Name -like 'Microsoft.CodeAnalysis.NetAnalyzers/*' } | Select-Object -First 1
if ($null -eq $analyzerPackage) { throw '项目未还原 Microsoft.CodeAnalysis.NetAnalyzers。' }
$analyzer = $null
foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
    $candidate = Join-Path $folder ($analyzerPackage.Value.path + '/analyzers/dotnet/Microsoft.CodeAnalysis.NetAnalyzers.dll')
    if (Test-Path -LiteralPath $candidate) { $analyzer = $candidate; break }
}
if ($null -eq $analyzer) { throw '找不到代码指标程序集。' }
$escapedRoslyn = [Security.SecurityElement]::Escape($roslyn)
$escapedAnalyzer = [Security.SecurityElement]::Escape($analyzer)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$($properties.TargetFramework)</TargetFramework>
    <UseWPF>true</UseWPF>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="Microsoft.CodeAnalysis"><HintPath>$escapedRoslyn/Microsoft.CodeAnalysis.dll</HintPath></Reference>
    <Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$escapedRoslyn/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference>
    <Reference Include="Microsoft.CodeAnalysis.NetAnalyzers"><HintPath>$escapedAnalyzer</HintPath></Reference>
  </ItemGroup>
</Project>
"@
$source = @'
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeMetrics;

internal static class Program
{
    private static readonly byte[] padding = [0, 1, 15, 16, 128, 254, 255];

    private static async Task<int> Main(string[] args)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(args[0]));
        var items = json.RootElement.GetProperty("Items");
        var constants = json.RootElement.GetProperty("Properties").GetProperty("DefineConstants").GetString()!.Split(';');
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: constants);
        var paths = items.GetProperty("Compile").EnumerateArray().Select(i => i.GetProperty("FullPath").GetString()!).Distinct()
            .Where(p => p.Contains("\\ELFAnalyzer\\", StringComparison.OrdinalIgnoreCase)
                || p.Contains("\\Enums\\", StringComparison.OrdinalIgnoreCase)
                || p.Contains("\\Utils\\", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith("GlobalUsings.g.cs", StringComparison.Ordinal));
        var trees = paths.Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), parseOptions, p)).ToList();
        if (args.Length > 4) trees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(args[5]), parseOptions, args[5]));
        var references = items.GetProperty("ReferencePath").EnumerateArray().Select(i => i.GetProperty("FullPath").GetString()!).Distinct()
            .Select(p => MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create("PersonalTools.ELFHeaderRegression", trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream);
        if (!emitted.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));

        var type = compilation.GetTypeByMetadataName("PersonalTools.ELFAnalyzer.UIHelper.ELFHeaderHelper")!;
        int minimum = int.Parse(args[1], CultureInfo.InvariantCulture);
        int classMinimum = int.Parse(args[3], CultureInfo.InvariantCulture);
        var types = args.Length > 4 ? type.ContainingNamespace.GetTypeMembers().SelectMany(FlattenTypes).ToArray() : new[] { type };
        var reports = new List<CodeAnalysisMetricData>();
        foreach (var item in types.OrderBy(t => t.Name))
        {
            var report = await CodeAnalysisMetricData.ComputeAsync(item, new CodeMetricsAnalysisContext(compilation, CancellationToken.None));
            reports.Add(report);
            Console.WriteLine($"{item.Name}: MI={report.MaintainabilityIndex}");
            if (report.MaintainabilityIndex < classMinimum)
                throw new InvalidOperationException($"{item.ToDisplayString()} has overall MI {report.MaintainabilityIndex} below {classMinimum}.");
        }
        var methods = reports.SelectMany(FlattenMetrics).Where(m => m.Symbol is IMethodSymbol).ToArray();
        if (methods.Length == 0) throw new InvalidOperationException("No methods were analyzed.");
        foreach (var method in methods)
        {
            Console.WriteLine($"{method.Symbol.ContainingType.Name}.{method.Symbol.Name}: MI={method.MaintainabilityIndex}, CC={method.CyclomaticComplexity}, ExecutableLines={method.ExecutableLines}");
            if (method.MaintainabilityIndex < minimum)
                throw new InvalidOperationException($"{method.Symbol.ToDisplayString()} has MI below {minimum}.");
        }
        File.WriteAllText(args[2], JsonSerializer.Serialize(new
        {
            MinimumMaintainability = minimum,
            MinimumClassMaintainability = classMinimum,
            Classes = reports.Select(r => new { Name = r.Symbol.Name, r.MaintainabilityIndex }),
            Methods = methods.Select(m => new { Name = m.Symbol.ToDisplayString(), m.MaintainabilityIndex, m.CyclomaticComplexity, m.ExecutableLines })
        }, new JsonSerializerOptions { WriteIndented = true }));

        var assembly = Assembly.Load(stream.ToArray());
        var parserType = assembly.GetType("PersonalTools.ELFAnalyzer.Core.ELFParser")!;
        var helper = assembly.GetType("PersonalTools.ELFAnalyzer.UIHelper.ELFHeaderHelper")!;
        var format = helper.GetMethod("GetFormattedELFHeaderInfo", BindingFlags.Static | BindingFlags.NonPublic)!;
        var originalCulture = CultureInfo.CurrentCulture;
        int cases = 0;
        try
        {
            // Exercise LSB, MSB, NONE and every invalid EI_DATA value for both bitnesses.
            foreach (string culture in new[] { "", "en-US", "fr-FR", "ar-SA" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (bool is64Bit in new[] { false, true })
                {
                    for (int data = 0; data <= byte.MaxValue; data++)
                    {
                        object parser = Activator.CreateInstance(parserType, [Fixture(is64Bit, (byte)data)])!;
                        string actual = (string)format.Invoke(null, [parser])!;
                        string expected = Expected(is64Bit, (byte)data);
                        if (actual != expected)
                            throw new InvalidOperationException($"Output differs for ELF{(is64Bit ? 64 : 32)}, EI_DATA={data}, culture={culture}.\nExpected:\n{expected}\nActual:\n{actual}");
                        cases++;
                    }
                }
                Console.WriteLine($"PASS ELF32/ELF64, all EI_DATA bytes, exact text/spacing/newlines, culture={culture}");
            }
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
        Console.WriteLine($"PASS {cases} output regression cases; all {methods.Length} methods have MI >= {minimum}.");
        if (args.Length > 4)
            assembly.GetType("UIHelperRegression")!.GetMethod("Verify", BindingFlags.Static | BindingFlags.Public)!.Invoke(null, [args[4]]);
        return 0;
    }

    private static IEnumerable<INamedTypeSymbol> FlattenTypes(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers())
            foreach (var item in FlattenTypes(nested)) yield return item;
    }

    private static IEnumerable<CodeAnalysisMetricData> FlattenMetrics(CodeAnalysisMetricData metric)
    {
        yield return metric;
        foreach (var child in metric.Children)
            foreach (var item in FlattenMetrics(child)) yield return item;
    }

    private static byte[] Fixture(bool is64Bit, byte data)
    {
        using var stream = new MemoryStream();
        stream.Write([0x7F, 0x45, 0x4C, 0x46, (byte)(is64Bit ? 2 : 1), data, 1, 0, 255]);
        stream.Write(padding);
        void Write(ulong value, int size)
        {
            Span<byte> bytes = stackalloc byte[8];
            if (data == 1) BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
            else BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
            stream.Write(data == 1 ? bytes[..size] : bytes[(8 - size)..]);
        }
        int width = is64Bit ? 8 : 4;
        Write(2, 2); // ET_EXEC
        Write(is64Bit ? 62u : 3u, 2); // EM_X86_64 / EM_386
        Write(0xABCDEF01, 4); // Distinct from EI_VERSION=1.
        Write(is64Bit ? 0xFEDCBA9876543210 : 0xF1234567, width);
        // Out-of-file offsets skip table parsing and exercise unsigned large numbers.
        Write(is64Bit ? 0xE123456789ABCDEF : 0xE1234567, width);
        Write(is64Bit ? 0xD123456789ABCDEF : 0xD1234567, width);
        Write(0xFEDCBA98, 4);
        Write(is64Bit ? 64u : 52u, 2);
        Write(is64Bit ? 56u : 32u, 2);
        Write(65535, 2);
        Write(is64Bit ? 64u : 40u, 2);
        Write(65534, 2);
        Write(65533, 2);
        return stream.ToArray();
    }

    private static string Expected(bool is64Bit, byte data)
    {
        string dataName = data switch { 0 => "NONE", 1 => "LSB", 2 => "MSB", _ => $"UNKNOWN({data})" };
        string dataDescription = data switch { 1 => "2's complement, little endian", 2 => "2's complement, big endian", _ => "none" };
        string magic = $"7F 45 4C 46 {(is64Bit ? 2 : 1):X2} {data:X2} 01 00 FF 00 01 0F 10 80 FE FF";
        FormattableString[] fields =
        [
            $"  Magic:            {magic}",
            $"  类别:             {(is64Bit ? "BIT64 (64-bit)" : "BIT32 (32-bit)")}",
            $"  数据:             {dataName} ({dataDescription})",
            $"  版本:             1 (current)",
            $"  OS/ABI:           UNIX - System V (0)",
            $"  ABI 版本:         255",
            $"  类型:             ET_EXEC (可执行文件)",
            $"  系统架构:         {(is64Bit ? "EM_X86_64 (AMD x86-64 architecture)" : "EM_386 (Intel 80386)")}",
            $"  版本:             0xABCDEF01",
            $"  入口点地址:       0x{(is64Bit ? 0xFEDCBA9876543210 : 0xF1234567):X}",
            $"  程序头起点:       {(is64Bit ? 0xE123456789ABCDEF : 0xE1234567)} (bytes into file)",
            $"  节头的起点:       {(is64Bit ? 0xD123456789ABCDEF : 0xD1234567)} (bytes into file)",
            $"  标志:             0xFEDCBA98  0xFEDCBA98",
            $"  本头的大小:       {(is64Bit ? 64 : 52)} (bytes)",
            $"  程序头的大小:     {(is64Bit ? 56 : 32)} (bytes)",
            $"  程序头数量:       65535",
            $"  节头大小:         {(is64Bit ? 64 : 40)} (bytes)",
            $"  节头数量:         65534",
            $"  字符串表索引节头: 65533"
        ];
        return string.Join(Environment.NewLine,
            new[] { "ELF 头:", new string('=', 80) }
                .Concat(fields.Select(f => f.ToString(CultureInfo.InvariantCulture)))
                .Append(string.Empty));
    }
}
'@
[IO.File]::WriteAllText((Join-Path $testDir 'Regression.csproj'), $project, $utf8)
[IO.File]::WriteAllText((Join-Path $testDir 'Program.cs'), $source, $utf8)
$reportName = 'ELFHeaderHelper.metrics.json'
if ($IncludeUIHelper) { $reportName = 'ELFUIHelper.metrics.json' }
$reportPath = Join-Path $testDir $reportName
$validationArgs = @($inputPath, $MinimumMaintainability, $reportPath, $MinimumClassMaintainability)
if ($IncludeUIHelper) {
    $validationArgs += Join-Path $PSScriptRoot 'fixtures/ELFUIHelper.expected.json'
    $validationArgs += Join-Path $PSScriptRoot 'fixtures/ELFUIHelperRegression.cs.txt'
}
dotnet run --project (Join-Path $testDir 'Regression.csproj') -c $Configuration -v:q -- @validationArgs
if ($LASTEXITCODE -ne 0) { throw 'ELF 回归测试或代码指标检查失败。' }
Write-Host "Metrics report: $reportPath"
