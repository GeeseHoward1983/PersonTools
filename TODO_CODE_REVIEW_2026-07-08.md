# 全量代码评审修复清单（2026-07-08，第10轮，分批次执行）

来源：superpowers 多视角评审，10 分片 × 多视角共 23 个并行评审代理（ConstString/Enums 两个数据分片因结果截断已重跑恢复）。
共 27 条 finding（Critical 1 / Important 7 / Minor 19；状态：确认 18 / 疑似 9）。
执行规则：按批次顺序修复；「疑似」项修复前先核实代码，若属误报则标记 `[!]` 并注明理由不改代码。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — Enums 规范值修正（3）

- [x] B1-1 (Critical/确认) `Enums/AArch64RelocationType.cs:36` — 从 R_AARCH64_JUMP26 起整段静态重定位枚举值错位（283/284/285/288/295/296/309，官方应为 282/283/284/287/299/300/313），起因是 CONDBR19(280) 后多跳一个空位（仅 281 是真空位）。已按 LLVM/binutils 官方值重排整段（含 MOVW_PREL_G3 后 294-298 空档）。
- [x] B1-2 (Important/确认) `Enums/SectionType.cs:42` — SHT_ARM_PREEMPTMAP/DEBUGOVERLAY/OVERLAYSECTION 改为 0x70000002/0x70000004/0x70000005。
- [x] B1-3 (Minor/疑似→核实属实) `Enums/DynamicTag.cs:79` — DT_SYMTAB_SHNDX 由 0x6ffffff5 改回官方值 34；同时移除占用 34 的 DT_NUM（glibc 计数常量、非真实 tag、无代码引用），保证 GetName(34) 确定性。

## 批次2 — ELF 解析器（4）

- [x] B2-1 (Minor/确认) `ELFAnalyzer/Core/ELFExidxInfo.cs:585` — pop wR 的 AppendRegRange 改传 cccc（原 ssss+cccc），与 VFP D 范围一致。
- [x] B2-2 (Minor/确认) `ELFAnalyzer/Core/ELFAttributeInfo.cs:607` — Tag_RISCV_arch 改为先 MeasureCStringByteLength(limit) 再有界 ExtractStringFromBytes，缺 NUL 不再串读相邻子节。
- [x] B2-3 (Minor/确认) `ELFAnalyzer/Core/ELFNoteInfo.cs:138` — gold-version 描述串改用带 descSize 的重载。
- [x] B2-4 (Minor/疑似→核实属实) `ELFAnalyzer/UIHelper/DynamicHelper.cs:49` — Tag 列按 Parser.Is64Bit 分 x16/x8。

## 批次3 — PE 解析器核心（3）

- [x] B3-1 (Important/确认) `PEAnalyzer/Parsers/PEParser.CLR.Metadata.cs:47` — BSJB 魔数改为 0x424A5342 并更正注释。
- [x] B3-2 (Minor/疑似→核实属实) `PEAnalyzer/Parsers/PEParser.CLR.Helpers.cs:90` — 堆上界校验改为 `index >= heapSize`（heapSize==0 恒拒绝；合法 #Strings 堆至少含 index 0 空串），heapEnd 同步简化。
- [x] B3-3 (Minor/疑似→核实属实) `PEAnalyzer/Parsers/PEImportParser.cs:278` — savePos 提到 try 外、`fs.Position = savePos` 移入 finally，异常路径也恢复流位置。

## 批次4 — PE 资源（4）

- [x] B4-1 (Important/确认) `PEAnalyzer/Resources/ResourceDirectoryReader.cs:138` — WalkEntries 改为整轮遍历"已访问目录偏移"全局去重（不再离开时移除），另加单轮 4096 目录节点总数硬上限，杜绝梯形 DAG 指数遍历。
- [x] B4-2 (Important/疑似→核实属实) `PEAnalyzer/Resources/PEResourceParser.Icon.Group.cs:119` — 新增 PEResourceParserIconData.TryAddIcon 统一入口：每文件图标总数 ≤4096、累计字节 ≤256MB，全部 5 处 Icons.Add 均已收口，达限即停止收集。
- [x] B4-3 (Minor/疑似→核实属实) `PEAnalyzer/Resources/PEResourceParser.Icon.Data.cs:136` — IsIconData 与 ConvertDibToIco 的 DIB 判定统一收紧为 biSize ∈ {40,108,124} 且 ≤ 数据长度，PNG/JPEG 不再被误判为位图头。
- [x] B4-4 (Minor/疑似→核实属实) `PEAnalyzer/Resources/PEResourceParser.Icon.cs:141` — ParseResourceDirectoryForNamedIcons 拆分「待扫描目录偏移」与「资源根基址」两个参数；根路径传 (root, root)，命名类型回退路径传 (子目录, root)，消除偏移重复叠加。

## 批次5 — MarkdownToWord + Utils（4）

- [x] B5-1 (Important/确认) `MarkdownToWord/Docx/DocxBlockRenderer.cs:35` — 正则改为无回溯歧义的 `^\s*\d+\.(?:\d+\.)*\d*[ \t　]+`（语言不变、每段由字面点号锚定），消除 ReDoS。
- [x] B5-2 (Important/确认) `Utils/Crypto/AesCryptoService.cs:81` — CreateAes 在 `aesAlg.Mode = mode` 赋值处捕获 Mode setter 对不支持模式（OFB/CTS 等）抛出的 CryptographicException，翻译为带清晰指引的 ArgumentException（不引用 CipherMode.OFB，天然不触发 CA5358、零新增抑制）；配置中途失败时 Dispose 防实例泄漏；AesEncryptionControl 下拉框移除 OFB 选项；CFB 反馈位保留 128。
- [x] B5-3 (Minor/确认) `Utils/FileDropHelper.cs:43` — 阈值由 int.MaxValue 改为 Array.MaxLength。
- [x] B5-4 (Minor/疑似→核实属实) `Utils/GlobalState.cs:22` — 改按 TwoLetterISOLanguageName=="zh" + 脚本/区域细分：zh-Hant*/zh-TW/HK/MO/zh-CHT→繁体，其余中文（含 zh-SG/zh-Hans/中性 zh）→简体。

## 批次6 — UserControls（2）

- [x] B6-1 (Minor/确认) `UserControls/MarkdownToWordControl.xaml.cs:245` — LoadMarkdownFile 改 async：读前 IsWithinHexDisplayLimit 预检、File.ReadAllText 移 Task.Run 后台、await 后校验 IsLoaded、catch 补 OutOfMemoryException。
- [x] B6-2 (Minor/疑似→核实属实) `UserControls/MarkdownToWordControl.xaml.cs:91` — OnLoaded 的 catch when 过滤补入 UnauthorizedAccessException 与 ObjectDisposedException。

## 批次7 — ConstString 简繁混排数据（7）

- [x] B7-1 (Important/确认) `ConstString/WindowsSystemErrors.TraditionalChinese.cs:5691` — 尾句改「請嘗試重新啟動實體電腦。」（与 5689 行措辞对齐）。
- [x] B7-2 (Minor/确认) 同文件 :29 —「就绪」→「就緒」。
- [x] B7-3 (Minor/确认) 同文件 :1936 —「进行中」→「進行中」。
- [x] B7-4 (Minor/疑似→核实属实) 同文件 :2111/:2113 —「要么」→「要麼」（共 4 处）。
- [x] B7-5 (Minor/确认) 同文件 :3548 —「一个」→「一個」。
- [x] B7-6 (Minor/确认) `ConstString/WindowsSystemErrors.SimplifiedChinese.cs:3125/:3131` —「數據」→「数据」（2 处）。
- [x] B7-7 (Minor/确认) `ConstString/WindowsStandardErrno.cs:125` —「剩余」→「剩餘」（值与注释）。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**（含 NetAnalyzers latest-all + EnforceCodeStyleInBuild；B5-2 采用异常翻译方案，未新增任何 CA5358 抑制）。
- [x] F2. 更新本文件全部勾选状态，提交「全量代码评审修复第十批(2026-07-08)」。

---

## 完成情况

**27/27 全部修复并通过编译**（9 条「疑似」项逐一核实后均属实，无误报）：Critical 1（AArch64 重定位表错位）+ Important 7 + Minor 19。
