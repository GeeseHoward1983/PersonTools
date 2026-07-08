# 全量代码评审修复清单（2026-07-08，第11轮，分批次执行）

来源：superpowers 多视角评审，7 代码分片 × 3 视角 + 4 数据分片，共 25 个并行评审代理（25/25 成功、0 失败，13 个代理零发现）。
原始 findings 20 条，去重合并后 **17 条**（Critical 1 / Important 5 / Minor 11；状态：确认 14 / 疑似 3）。
执行规则：按批次顺序修复；「疑似」项修复前先核实代码，若属误报则标记 `[!]` 并注明理由不改代码。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — Enums ARM ABI 属性枚举（2）

- [x] B1-1 (Minor/确认) `Enums/ABIOptimizationGoals.cs:11` — 值与 ARM ABI Tag_ABI_optimization_goals(=30) 规范不符：2 应为 Aggressive Speed（现标 Balanced 语义）、6 应为 Aggressive Debug（现标 AggressiveSpeed 语义）。按规范重排：1=Prefer Speed, 2=Aggressive Speed, 3=Prefer Size, 4=Aggressive Size, 5=Prefer Debug, 6=Aggressive Debug。
- [x] B1-2 (Minor/确认) `Enums/ABIAlignPreserved.cs:22` — 值 2 名为 Four_byte，但 ARM ABI Tag_ABI_align_preserved(=25) 值 2 含义为「SP 始终 8 字节对齐」；改为 8-byte 语义命名与描述。

## 批次2 — ELF 解析器（5）

- [ ] B2-1 (Important/确认) `ELFAnalyzer/Core/ELFExidxInfo.cs:483` — ARM EHABI 解卷指令 1001nnnn（Set vsp = r[nnnn]）保留判定误用 `imm is 0xC or 0xF`（r12/r15），规范保留值为 nnnn=13(sp)/15(pc)；改为 `imm is 0xD or 0xF`。
- [ ] B2-2 (Important/确认) `ELFAnalyzer/Core/ELFHeaderDescriptions.cs:310` — MIPS e_flags 架构段（高 4 位）错位：0x60000000 应为 mips64（现 mips32r2）、0x80000000 应为 mips64r2（现 mips64）、0x90000000 应为 mips32r6（现 mips64r2），缺 0xa0000000=mips64r6。按 binutils E_MIPS_ARCH_* 全表修正。
- [ ] B2-3 (Important/确认) `ELFAnalyzer/Core/ELFHeaderDescriptions.cs:339` — MIPS e_flags 标志位表错标：0x10 实为 EF_MIPS_UCODE（现误 nan2008）、0x20 实为 EF_MIPS_ABI2/n32（现误 nan2001）、0x4000 属 ABI 掩码 0xF000 中的 EABI64（现误 n32）；真 nan2008=0x400、fp64=0x200 漏标。修正位表并按掩码处理 ABI 字段。
- [ ] B2-4 (Minor/确认) `ELFAnalyzer/Core/ELFHeaderDescriptions.cs:406` — ARM e_flags 0x00400000 被标 interworking enabled，实为 EF_ARM_LE8；EF_ARM_INTERWORK=0x04。改 0x00400000→LE8，interworking 判 0x04。
- [ ] B2-5 (Minor/确认) `ELFAnalyzer/Core/VersionSymbolFormatter.cs:91` — `SectionHeaders`(List<值类型>).Find 未命中返回全零默认结构体，L93 `== null` 判空永不成立；无 SHT_GNU_verdef 节时用全零节头输出错误 addr/offset/link。改 FindIndex>=0 判命中。

## 批次3 — PE 解析器（3）

- [x] B3-1 (Minor/确认) `PEAnalyzer/PEHeaderDescriptions.cs:20` — 机器类型 0x0169 描述「MIPS WCI v2」系笔误，官方 IMAGE_FILE_MACHINE_WCEMIPSV2 = MIPS WCE v2（Windows CE）。
- [x] B3-2 (Minor/疑似→核实属实) `PEAnalyzer/IconImageFactory.cs:41` — 仅设 DecodePixelWidth 未设 DecodePixelHeight，构造「小宽度、超高」内嵌 PNG 的图标可让解码位图按宽高比在高度轴膨胀（数 GB 非托管内存）。两轴同时封顶。
- [x] B3-3 (Minor/确认) `PEAnalyzer/Resources/PEResourceParser.Icon.Helpers.cs:31` — FindSpecificIconData(L31) 与 FindIconDataByResourceId(L103) 按 `NameOrId & 0xFFFF` 匹配 ID 前未跳过命名条目（高位 0x80000000=1），命名条目名偏移低 16 位恰等于目标 ID 时误匹配；与 ScanTypeEntries 已有守卫不一致。两处比较前先 `if ((entry.NameOrId & 0x80000000) != 0) continue;`。

## 批次4 — MarkdownToWord（3）

- [x] B4-1 (Important/疑似→核实属实) `MarkdownToWord/Docx/DocxBlockRenderer.cs:96` — 泛化 ContainerBlock 分支递归时未 `indentLevel + 1`，绕过 MaxNestingDepth 守卫；深层嵌套容器可致 StackOverflow 崩进程。该分支与 quote/list 等入口一致改传 `indentLevel + 1`。
- [x] B4-2 (Minor/确认) `MarkdownToWord/Docx/DocxTableRenderer.cs:202` — 单元格未应用 GFM 列对齐（ColumnDefinitions[col].Alignment），`:--:`/`--:` 全按左对齐输出。按列取 Alignment，Center/Right 时给单元格段落加 Justification。
- [x] B4-3 (Minor/疑似→核实属实) `MarkdownToWord/Docx/DocxInlineRenderer.cs:136` — 邮箱自动链接 `<a@b.com>` 的 Url 无 mailto: 前缀，Uri.TryCreate(Absolute) 失败降级纯文本，Word 中不可点击。IsEmail 时补 mailto: 前缀。

## 批次5 — ConstString MySQL 数据错位（3）

- [x] B5-1 (Critical/确认) `ConstString/MySqlErrors.SimplifiedChinese.cs:835-910` — 键 1638–1713 共 76 条整体错位：值为其它错误码的消息（1638–1652 错入 SLAVE_CHANNEL 块、1653–1713 重复 1055–1115 的消息）。已以 EN 为基准全部重译替换（占位符逐一保序）；另将 1333/1514-1516/1547-1548/1557/1749/1778 共 9 键的 ER 常量名前缀统一为 EN 官方名（译文保留，1547/1749 译文同步补齐占位符）。脚本校验：SC 与 EN 键名不匹配数 86→0。
- [x] B5-2 (Important/确认) `ConstString/MySqlErrors.TraditionalChinese.cs` — 实测错位共 363 键（1340–1346、1358–1713，非估算的 280）：179 键按值内 ER 常量名从本文件找回原繁体译文回位；184 键（原译文缺失 107 + 原译文占位符/语义不符 77）以 EN 为基准补译；另 1265/1333/1749/1778 共 4 键仅统一常量名（1265/1749 译文同步补齐占位符）。脚本校验：TC 与 EN 键名不匹配数 367→0，全部改动键占位符与 EN 严格一致。注：未错位区间尚存历史遗留的占位符数量差异（SC 65/TC 40 处，11 轮评审均未列为 finding），不属本项范围。
- [x] B5-3 (Minor/确认) `ConstString/MySqlErrors.SimplifiedChinese.cs:1041` — 键 1844 常量名 STT→STMT（随 B5-1 重命名批量修复）。

## 批次6 — ConstString ODBC 文案（1）

- [x] B6-1 (Minor/确认) `ConstString/OdbcErrors.cs:21` — 01S05 英文误作 "Function sequence error"（那是 S1010），应为 "Cancel treated as FreeStmt/Close"；S1DE0(L104) 误作 "Operation completed successfully with no data"，应为 "No data at execution values pending"。与本文件中文含义对齐。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**（含 NetAnalyzers latest-all + EnforceCodeStyleInBuild）。
- [x] F2. 更新本文件全部勾选状态，提交「全量代码评审修复第十一批(2026-07-08)」（不 push）。

---

## 完成情况

**17/17 全部修复并通过编译**（3 条「疑似」项逐一核实后均属实，无误报）：Critical 1（MySQL 简中 1638–1713 共 76 键错位）+ Important 5 + Minor 11。
MySQL 键名对照脚本终态：SC 86→0、TC 367→0 不匹配，全部改动键占位符与 EN 严格一致。
