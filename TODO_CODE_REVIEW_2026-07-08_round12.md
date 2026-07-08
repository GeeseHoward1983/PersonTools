# 全量代码评审修复清单（2026-07-08，第12轮，分批次执行）

来源：superpowers 多视角评审，7 代码分片 × 3 视角 + 4 数据分片，共 25 个并行评审代理（25/25 成功、0 失败，14 个代理零发现）。
原始 findings 15 条，去重合并后 **13 条**（Important 6 / Minor 7，无 Critical；状态：确认 8 / 疑似 5）。
执行规则：按批次顺序修复；「疑似」项修复前先核实代码，若属误报则标记 `[!]` 并注明理由不改代码。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — Enums ARM 构建属性枚举（3）

- [x] B1-1 (Minor/确认) `Enums/AdvancedSIMDArch.cs:12` — Tag_Advanced_SIMD_arch 值 2/3/4 按 ARM ABI/readelf 更正：2=NEONv1_FusedMAC、3=NEON_ARMv8、4=NEON_ARMv8_1（枚举无外部引用）。
- [x] B1-2 (Minor/确认) `Enums/ABIFPNumberModel.cs:12` — 值 1→Finite、值 3→IEEE_754（完整模型），补值 2=RTABI。
- [x] B1-3 (Minor/确认) `Enums/ABIFPHardUse.cs:21` — 值 0→As_Tag_FP_arch、值 2→Reserved。

## 批次2 — ELF 解析器（4）

- [x] B2-1 (Important/确认) `ELFAnalyzer/Core/ELFExidxInfo.cs:204` — BuildCompactInstructions 改为：index==0 走 3 字节内联；1/2 共用「bits16-23 额外字数」长格式；保留索引(≥3)返回空不解码。
- [x] B2-2 (Important/确认) `ELFAnalyzer/Core/ELFAttributeInfo.cs:258` — AppendAEABIStringAttr 改为先 MeasureCStringByteLength（止于内部 NUL）再有界提取，字符串值不再串入后续属性字节。
- [x] B2-3 (Minor/疑似→核实属实) `ELFAnalyzer/Core/ELFNoteInfo.cs:106` — NT_GNU_ABI_TAG 改按 readelf 语义输出 "(OS: Linux, ABI: 2.6.32)"：desc[0] 解码为 OS 名（Linux/Hurd/Solaris/FreeBSD/NetBSD/Syllable/NaCl），后三字为版本。
- [x] B2-4 (Important/疑似→核实属实) `ELFAnalyzer/Core/VersionSymbolParser.ParseDependencies.cs:86` — WalkVerneed 外层加 MaxVerneedEntries=1_000_000 硬上界（与 verdef 侧 MaxVerDefEntries 同款），封堵 maxCount<=0 且 vn_cnt=0 时 vn_next=1 小步进迭代至节尾。

## 批次3 — 遍历上限防护（2）

- [x] B3-1 (Important/疑似→核实属实) `ELFAnalyzer/UIHelper/ProgramHeaderHelper.cs:76` — 段×节映射（两者各可达 65535，解析处仅按 16 位字段自然上限）加全局工作量预算 MaxMappingChecks=2_000_000，超限截断并输出省略标注。
- [x] B3-2 (Important/疑似→核实属实) `PEAnalyzer/Resources/ResourceDirectoryReader.cs:105` — WalkEntries 增设单轮条目总数上限 MaxEntriesPerWalk=65536（原目录数上限 4096 不限单目录 131070 条目，最坏约 5 亿次回调），随 ThreadStatic 遍历状态统一复位。
- [x] B3-2 顺带核实：图标数据路径本身已有 TryAddIcon 总量上限（第10轮 B4-2），本项补的是条目回调放大面。

## 批次4 — MarkdownToWord（1）

- [x] B4-1 (Important/疑似→核实属实) `MarkdownToWord/Docx/DocxInlineRenderer.cs:36` — RenderInlines/RenderInline 加 depth 参数与 MaxNestingDepth 守卫（公开签名不变，内部私有重载）；Emphasis/Container/Link 三处递归点 +1；DocxImageEmbedder.AppendLiterals（alt 文本展开）同款守卫。

## 批次5 — ConstString（3）

- [x] B5-1 (Minor/确认) `ConstString/MySqlErrors.TraditionalChinese.cs:425` — 1228/1229 误译「…是只讀的」更正为 SESSION/GLOBAL 变量的 SET 语法语义（对齐 EN/SC，占位符 %-.64s 同步补齐）。
- [x] B5-2 (Minor/疑似→核实属实并扩展) — 占位符**数量**不一致全量清零：实测 SC 65 键 / TC 41 键。其中 6 键（1037/1039/1045/1074/1459/1707）系 **EN 文件自身占位符损坏**（"% d"/"% s"/"% lu"/"% -.32s" 多空格），修 EN 并同步 SC/TC；其余 SC 63 键 / TC 41 键以 EN 为基准重译对齐（占位符逐一保序）。脚本终态：SC/TC 与 EN 占位符数量差 **0**，EN 无残留损坏占位符。注：同数量不同精度拼写（如 %s 对 %-.192s）为历史风格、非本 finding 范围，已记录。
- [x] B5-3 (Minor/确认) `ConstString/SqlServerErrors.cs:135/200` — 99999 兜底项分别译为「SQL Server 错误 - 未知错误」/「SQL Server 錯誤 - 未知錯誤」；顺带修复同文件 L193 繁体错字「複製錶」→「複製表」。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**（含 NetAnalyzers latest-all + EnforceCodeStyleInBuild）。
- [x] F2. 更新本文件全部勾选状态，提交「全量代码评审修复第十二批(2026-07-08)」（不 push）。

---

## 完成情况

**13/13 全部修复并通过编译**（5 条「疑似」项逐一核实后均属实，无误报）：Important 6 + Minor 7。
MySQL 三语占位符数量差终态为 0（含 EN 自身 6 键损坏占位符的修复）。
