# 全量代码评审修复清单（2026-07-08，第13轮，分批次执行）

来源：superpowers 多视角评审，7 代码分片 × 3 视角 + 4 数据分片，共 25 个并行评审代理（25/25 成功、0 失败，17 个代理零发现）。
原始 findings 9 条，去重合并后 **7 条**（Important 3 / Minor 4；状态：确认 6 / 疑似 1）。
执行规则：按批次顺序修复；「疑似」项修复前先核实代码，若属误报则标记 `[!]` 并注明理由不改代码。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — Enums（1）

- [x] B1-1 (Minor/确认) `Enums/SymbolType.cs:13` — STT_LOOS 由 11 改为 10（gABI），以 `STT_LOOS = STT_GNU_IFUNC` 别名实现；GetTypeName 走 Enum.GetName，声明序在前的 STT_GNU_IFUNC 优先用于显示。

## 批次2 — ELF 解析器（1）

- [x] B2-1 (Important/确认) `ELFAnalyzer/Core/ELFExidxInfo.cs:578` — 新增 ProcessVspLargeIncrement：0xB2 在主循环于定长分派前拦截，操作数按完整 ULEB128 解码（≤5 字节），按实际消耗推进 offset；移除旧的定长双字节 0xB2 分支。

## 批次3 — PE 资源遍历预算（1，两代理同根因合并）

- [x] B3-1 (Important/确认) `ResourceDirectoryReader.cs` + `PEResourceParser.Icon(.Helpers).cs` — 新增 ScanScope 扫描会话（BeginScan/Dispose，可嵌套）与 TryConsumeEntry 统一预算入口：ParseIconInfo 全程一个会话，WalkEntries/ScanTypeEntries/ScanNamedEntries 与 Icon.Helpers 两处手写条目循环共用同一 65536 条目预算，预算不再随每次顶层遍历清零；无会话时保持旧语义（版本资源路径不受影响）。

## 批次4 — MarkdownToWord（1）

- [x] B4-1 (Minor/确认) 居中特殊段落显式重置首行缩进（Indentation FirstLine=0，且按 OOXML 架构序置于 jc 前）：CaptionBuilder(图/表题注)、CoverBuilder(封面标题)、TocBuilder(目录标题)、SectionBuilder(页脚页码)、BlockRenderer(居中图片段 + Figure 题注) 共 6 处。

## 批次5 — ConstString（3）

- [x] B5-1 (Minor/确认) `ConstString/LinuxErrno.TraditionalChinese.cs:111/152/157` — 「緩沖區」→「緩衝區」(2处)、「沖突」→「衝突」(1处)；顺带修复同类 `OdbcErrors.cs:282` S1090「緩沖區」→「緩衝區」。
- [x] B5-2 (Minor/确认) `ConstString/WindowsSystemErrors.TraditionalChinese.cs:1847` — 码 6630「多工復用」→「多工複用」。
- [x] B5-3 (Important/确认) `ConstString/SqlServerErrors.cs` — 按 Microsoft Learn 官方错误表核实并修正三语言块：170=语法错误(near)、266=EXECUTE 后事务计数不匹配、421=数据类型不可比较不能作 DISTINCT、615=找不到数据库 ID(可能离线)；**814 在官方 0–999 表中不存在**（808 直接跳 821，原「找不到存储过程」实为 2812 的文案），三块中删除该键。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 更新本文件全部勾选状态，提交「全量代码评审修复第十三批(2026-07-08)」（不 push）。

---

## 完成情况

**7/7 全部修复并通过编译**（1 条「疑似」项核实属实，无误报）：Important 3 + Minor 4。findings 收敛趋势：17 → 13 → 7。
