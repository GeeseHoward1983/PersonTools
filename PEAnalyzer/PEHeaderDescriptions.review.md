# PEHeaderDescriptions 代码审查与回归记录

审查日期：2026-11-25。范围：PE 头的展示描述、映射目录及其全部字段/属性、方法、嵌套类型；不包含整个 PE 解析器的正确性证明。

## 1. 真实 .NET 代码指标

使用主项目已还原的 `Microsoft.CodeAnalysis.NetAnalyzers 10.0.103` 中
`Microsoft.CodeAnalysis.CodeMetrics.CodeAnalysisMetricData.ComputeAsync` 测量，
配合实际 .NET 10 SDK 的 Roslyn 程序集。不是按代码行数自行估算。
仓库原有 `PersonalTools.Metrics.xml` 的 0 行报告不能作为本次验证依据。

| 公开接口 | 修改前 MI | 修改后 MI |
| --- | ---: | ---: |
| GetMachineTypeDescription | 90 | 90 |
| GetSubsystemDescription | 84 | 93 |
| GetLinkerVersionDescription | 83 | 91 |
| GetCompilerVersionDescription（三参数） | 68 | 91 |
| GetCompilerVersionDescription（两参数重载） | 原三参数接口的可选参数调用 | 93 |
| GetOperatingSystemVersionDescription | 93 | 93 |
| GetSubsystemVersionDescription | 93 | 93 |
| GetImageVersionDescription | 96 | 97 |
| GetDetailedFileType | 69 | 88 |
| GetDriverType | 68 | 88 |

初次重构只检查方法，遗漏了字段初始化，导致七个字典字段仍为 72–82。
本轮纠正检查范围，递归检查本类、映射目录、自定义描述 Attribute、所有嵌套枚举，
不把低指标代码移到辅助类后排除，也不使用规则抑制或生成代码排除。

| 字段 | 补测时 MI | 当前 MI |
| --- | ---: | ---: |
| s_machineTypes | 72 | 93 |
| s_subsystems | 75 | 93 |
| s_legacyToolchains | 77 | 93 |
| s_msvc14Families | 79 | 93 |
| s_windowsVersions | 75 | 93 |
| s_fileKinds | 80 | 93 |
| s_efiDrivers | 82 | 93 |

本类 22 个方法的指标保持不变，公开接口最低 88、私有辅助方法最低 87。
本类全部 13 个字段（含常量）均为 93；类整体 MI 由 82 提升到 **87**。
包含新增目录后，共检查 40 个方法/访问器/构造器、100 个字段、1 个属性及 10 个类型：
全部方法最低 87、全部字段最低 89、全部类型最低 86、属性 MI 100，所有成员 ≥85。

### 映射数据的组织方式

`PEHeaderDescriptionCatalog.cs` 使用强类型枚举标识每个目录条目，
通过继承 `DescriptionAttribute` 的 `PEHeaderDescriptionAttribute` 原子声明协议码与描述。
例如 `[PEHeaderDescription(0x0000014C, "Intel 386 (x86)")] I386`。
枚举的隐式序号仅是内部条目标识，**不是协议码**，不可直接强转成 Machine/SubSystem 值。
真正的协议码从 Attribute 读取，仅在构建不可变字典时进行反射，正常查询仍为字典查找。

通用构建器使用 `Enum.GetValues` / `ToFrozenDictionary`，新增条目无需修改查询算法。
缺失标注会导致初始化失败，重复协议码由 `ToFrozenDictionary` 拒绝，
ushort/int 键转换使用 checked，版本键使用移位/掩码拆分，不依赖 unchecked 编译设置。
链接器键编码为 `(Major << 8) | Minor`，Windows 键编码为 `(Major << 16) | Minor`。
新增独立预期表检查七个目录的条目数量、键及全部描述，防止多出/遗漏映射。

主要重构：不可变 `FrozenDictionary` 保存协议映射；共享子系统与 Windows 版本表；
把文件属性、驱动线索与格式化拆为职责单一的小方法。
编译器接口通过显式两参数重载保留原调用行为，三参数接口不再声明可选参数。
这保留了原有源代码调用方式，但改变了该内部方法的 optional 参数反射元数据。

## 2. 工具链版本修正

PE `MajorLinkerVersion/MinorLinkerVersion` 是链接器版本，不是 `_MSC_VER`、
MSVC 编译器版本、IDE 版本，也不是 .NET 目标框架版本。仅凭这些字段不能确认厂商。
因此链接器显示“可能的工具链”，非托管编译器显示“编译器未知”，
托管程序集不再被确定标为 Microsoft 编译器，也不推断 .NET 版本。

| PE 链接器版本线索 | 常见 MSVC 工具集 / VS 版本族 |
| --- | --- |
| 6.0 | Visual C++ 6.0 |
| 7.0 | VS.NET 2002 |
| 7.10 | VS.NET 2003，MSVC 工具集名为 7.1 |
| 8.0 / 9.0 | VS 2005 / 2008 |
| 10.0 / 11.0 / 12.0 | VS 2010 / 2012 / 2013 |
| 14.0x | VS 2015 工具集族 |
| 14.1x | VS 2017 工具集族 |
| 14.2x | VS 2019 工具集族 |
| 14.3x / 14.4x | VS 2022 工具集族 |
| 14.5x | VS 2026 起的独立版本工具集族 |

- 14.40 不是 VS 2026；14.50 才是 VS 2026 的起始工具集版本。
- VS 的 15/16/17/18 不能写成链接器版本；保留实际原始 `[major.minor]`。
- 14.x 的十进制区间只是版本族线索，不声明每个区间内的次版本都实际发布过。
- VS 2026 起 MSVC 与 IDE 版本解耦，不能继续凭十位数推算未来 VS 年份。
- 未识别的历史版本组合、14.60 及更大的未知次版本、major=15/48/255 等均返回未知工具链，
  不再因为数字大就断言是“较新的 Microsoft Visual C++”。
- 新 IDE 可以安装旧工具集，因此版本族不是实际 IDE 的确定识别结果。

## 3. 系统、架构、文件与驱动逻辑

- OS 和子系统版本描述明确为**所需版本族**，不是生成/运行本文件的 OS。
- 补充 NT 3.5；5.2 同时对应 XP x64 与 Server 2003/R2；10.0 补充 Server 2025，
  且无法区分 Windows 10/11 或对应 Server。未知版本不再虚构 `Windows NT 11.x` 等名字。
- 校正 `0x01C2` 为 ARM Thumb，Thumb-2 使用 `0x01C4`；补充 R3000BE、
  ARM64EC/ARM64X、RISC-V、LoongArch 机器码及 Xbox Code Catalog 子系统。
  原 `0x0268 → Motorola 68000` 未能在 PE/COFF 规范或核对的 SDK/UEFI 头中找到依据，
  改为保留原码的 Unknown，避免把未定义值确定识别为某种架构。
- DLL、可执行标志和 SYSTEM 属性分别展示，SYSTEM 不再覆盖 DLL 分类。
- `IMAGE_FILE_SYSTEM` 只说明系统文件，不能证明是驱动。
- Native+SYSTEM 只提示“可能为内核驱动”，不当成确定的内核驱动。
- WDM 由 `DllCharacteristics & 0x2000` 识别，不依赖 SYSTEM 标志；
  不要把同样数值的 COFF DLL 标志或 `0x1000` AppContainer 当成 WDM。
- EFI 子系统 11/12 足以描述 EFI 驱动角色，不要求 Windows 的 SYSTEM 标志。
  如果同时设置 WDM，则保留 EFI 角色并提示标志不一致。
- EFI Application、EFI ROM、Windows Boot Application 不自动标为驱动。
- 没有驱动线索时返回空串，表示**无法从这些字段确认驱动类型**，不代表文件一定不是驱动。

## 4. 验证

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-PEHeaderDescriptions.ps1 -Configuration Release
```

脚本先构建主项目，再编译独立回归项目；仅将产物写入系统临时目录，
输出完整签名的指标与 `PEHeaderDescriptions.metrics.json` 路径。
任何低于 85 的字段、属性、方法、构造器、访问器、类或嵌套枚举均导致失败。`-SkipMainBuild` 仅用于已有构建结果时复测；
`-MetricsOnly` 用于单独测量，不能代替完整回归。

验证覆盖：

- 四种区域设置：Invariant、en-US、zh-CN、ar-SA；
- 所有 65,536 个机器码、子系统码、链接器 byte 版本组合；
- 所有 ushort 文件特征与 DLL 特征，覆盖 Native、GUI、EFI 驱动、Boot Application 和未知子系统；
- Windows 已知版本、未知版本与 ushort 边界；
- 用 `System.Reflection.PortableExecutable.PEReader` 读取真实托管程序集和 `kernel32.dll`；
- 七个映射目录的完整键集、条目数量与描述逐项对照；
- 共 10,749,032 个字符串/逻辑断言全部通过，Release 构建 0 错误、0 警告。

注意：穷举参数域与独立预期表能验证本类实现和已核对规则，
不能证明任意第三方文件的编译器身份、未知未来工具集、或整个 PE 解析流程完全正确。

## 5. 核对依据

- [Microsoft：MSVC compiler versions](https://learn.microsoft.com/en-us/cpp/overview/compiler-versions)
- [Microsoft：PE/COFF format](https://learn.microsoft.com/en-us/windows/win32/debug/pe-format)
- [Microsoft：IMAGE_OPTIONAL_HEADER32](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-image_optional_header32)
- [Microsoft：Operating System Version](https://learn.microsoft.com/en-us/windows/win32/sysinfo/operating-system-version)
- [Microsoft：WinSDK winnt.h](https://github.com/microsoft/win32metadata/blob/main/generation/WinSDK/RecompiledIdlHeaders/um/winnt.h)
