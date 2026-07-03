# 全量代码评审修复清单（第七轮 · 2026-07-03）

> 来源：8 个并行多视角评审代理对全量代码的 findings。本文件为修复进度跟踪，**每完成一项即更新对应勾选与备注**。
> 约定：`[ ]` 待修 / `[x]` 已修 / `[-]` 经复核不修（附原因）。
> 验证：全部修完后 `dotnet build PersonalTools.csproj -c Debug`，以「0 个错误」为准。

---

## 🔴 高危（真实功能错误 / DoS）

- [x] **H1** `PEAnalyzer/Resources/PEResourceParser.Icon.Group.cs:69-102` — 组图标目录项按 16 字节读，实为 14 字节 GRPICONDIRENTRY（末字段 2 字节 nID）。改 14 字节步长 + `ReadUInt16` 读 nID。（两代理交叉确认）✅ 已改步长 16→14、末字段 `ReadUInt32`→`ReadUInt16`。
- [x] **H2** `PEAnalyzer/Models/CLRInfo.cs:34-40` — `Architecture` getter 逻辑错，忽略 `PEMachineType`。修正四组合并对 (false,false) 按 Machine 判 x64/ARM64。✅ 已重写 switch；并同步更新 `PEHeaderPresenter.GetBitInfo` 处理新值 `Any CPU (32-bit preferred)` / `ARM`。
- [x] **H3** `ELFAnalyzer/Core/ELFExidxInfo.cs:261-315` — `FindContainingSymbolName` 未命中时回扫到 0，O(N×M) CPU DoS。✅ 预计算 maxSymbolSize 沿调用链传入，未命中回扫设「窗口上界(maxSymbolSize) + 硬步数上界(4096)」双重界。
- [x] **H4** `MarkdownToWord/Docx/DocxInlineRenderer.cs:130-140` — `AppendText` 非法 XML 控制字符未过滤→损坏 .docx。✅ 新增 `SanitizeXmlText`（惰性分配、保留合法代理对、剥离非法 XML 1.0 字符）并在 `AppendText` 出口统一净化。

## 🟠 中危（正确性 / 静默丢内容 / 可诱导 OOM）

- [x] **M1** `ELFAnalyzer/Core/ELFNoteInfo.cs:83-86` — ELF64 note 硬编码 8 字节对齐，应固定 4 字节。✅ `AlignNoteOffset` 改固定 4 字节、去 is64Bit 参数与未用局部。
- [x] **M2** `ELFAnalyzer/UIHelper/GotHelper.cs:56-65` — GOT 条目大小盲信 sh_entsize→可诱导 OOM。✅ 移除 sh_entsize 采信、恒用指针宽度，并加 MaxGotEntries=2,000,000 硬上界。
- [x] **M3** `UserControls/DependencyNode.cs:80` — `EnsureLoadedAsync` 提前置 IsLoaded 导致并发读 Info=null。✅ 拆出 `LoadCoreAsync`，`EnsureLoadedAsync` 返回共享 `loadTask ??= LoadCoreAsync()`，IsLoaded 移至完成后置位。
- [x] **M4** `PEAnalyzer/PEHeaderDescriptions.cs:218` — WDM 位常量 `0x0020` 应为 `0x2000`。✅ 已改。
- [x] **M5** `PEAnalyzer/PEHeaderDescriptions.cs:119-121` — MSVC 14.x→VS 年份映射偏一代。✅ 重写为 14.0x→2015 / 14.1x→2017 / 14.2x→2019 / 14.3x+→2022（Range 标签同步，供 GetLinkerVersionDescription 使用）。
- [x] **M6** `PEAnalyzer/PEHeaderDescriptions.cs:186` — `GetDetailedFileType` native 分支字符串黏连+误称 Driver。✅ 改为三目 `" (Native Driver)"` / `" (Native Image)"`（带前导空格括号、不再误称 Driver）。
- [x] **M7** `MarkdownToWord/Docx/DocxInlineRenderer.cs:31-66` — HTML 实体（`&copy;` 等）静默丢失。✅ 补 `HtmlEntityInline` 分支（取 `Transcoded`）。
- [x] **M8** `MarkdownToWord/Docx/DocxInlineRenderer.cs:64`(+block) — 数学公式静默丢失。✅ 补行内 `MathInline` 分支（`$内容$` 兜底）。**块级 `$$…$$` 经核实为误报**：Markdig `MathBlock` 继承 `FencedCodeBlock`，已由代码块分支渲染（编译器 CS8120 佐证），无需改。
- [x] **M9** `MarkdownToWord/Docx/DocxImageEmbedder.cs:307` vs `:64` — 解压炸弹像素校验在整图解码后。✅ 改 `BitmapCacheOption.None + DelayCreation` 只读图像头维度，超限即拒绝、不整图解码。

## 🟡 低危 / 轻微

- [x] **L1** `Utils/Crypto/RsaCryptoService.cs:82-86` — `Verify` 把密钥/hex 非法与真失败静默混为 false 且吞日志。✅ 拆分：解析/导入失败→记日志+抛 CryptographicException（UI 已捕获并显示原因）；仅 VerifyData 的 false 作验签失败。
- [x] **L2** `Utils/AppLogger.cs:115-130` — `SanitizeForLog` 未覆盖 U+2028/U+2029/DEL/C1。✅ 补 0x7F、0x80-0x9F、U+2028/U+2029。
- [x] **L3** `Utils/Hash/CrcCalculator.cs:54` — Width<8 潜在负移位。✅ 加 `Debug.Assert(Width>=8)` 防御 + 注释。
- [x] **L4** `ELFAnalyzer/Core/ELFNoteInfo.cs:145` — `descSize*3` int 溢出。✅ 加 64KB 显示上限 + 溢出安全的容量计算，超限追加省略号。
- [x] **L5** `ELFAnalyzer/Core/ELFHeaderInfo.cs:9-54` — 仅校验 16 字节，截断文件抛 EndOfStream。✅ 读到 EI_CLASS 后按 32/64 位(52/64)补校验完整头长度。
- [x] **L6** `ELFAnalyzer/Core/ELFAttributeInfo.cs:428/443/323` — GNU 属性路径取串不受 endOffset 限制，跨子节。✅ 四处(323/350/428/443)改为 `MeasureCStringByteLength` 定长 + 3 参有界 `ExtractStringFromBytes`。
- [x] **L7** `ELFAnalyzer/Core/ELFExidxInfo.cs:355` — `StValue+StSize` ulong 回绕误判。✅ 加 end>=StValue 回绕判定。
- [x] **L8** `ELFAnalyzer/UIHelper/RelocationHelper.cs:185` — 符号名硬编码 SHT_DYNSYM，应由 sh_link 目标 sh_type 推导。✅ 新增 `ResolveSymbolTableType` 由 sh_link 目标 sh_type 判 SHT_SYMTAB/SHT_DYNSYM 并沿链下传。
- [x] **L9** `ELFAnalyzer/UIHelper/ELFHeaderHelper.cs:19-23` — EI_DATA default 把 NONE 标 big-endian。✅ 补 MSB 分支，其余显示 none。
- [x] **L10** `ELFAnalyzer/UIHelper/ProgramHeaderHelper.cs:107` — `sh_addr+sh_size` 未防 ulong 回绕。✅ 对称加溢出夹紧。
- [x] **L11** `ELFAnalyzer/UIHelper/SectionHeaderHelper.cs:22-23` + `ProgramHeaderHelper.cs:23-25` — hex 宽度硬编码不分 32/64 位。✅ 五处改 `Is64Bit ? x16 : x8`。
- [x] **L12** `ELFAnalyzer/Models/*.cs`（ELFHeader/ELFProgramHeader/ELFSymbol/ELFSectionHeader/ELFDynamic）— 死且误导的 `[StructLayout(Pack=1)]`。✅ 五个结构体移除 StructLayout/MarshalAs 及无用 using，补说明注释。
- [x] **L13** `UserControls/AesEncryptionControl.xaml.cs:236` + `RsaEncryptionControl.xaml.cs:259` — 拖放在 UI 线程同步读 50MB。✅ 两处改 `async void` + `await Task.Run(读盘+ToHexString)`，对齐 CrcComputeControl。
- [x] **L14** `UserControls/ELFAnalyzerControl.xaml.cs:143` — 缺重入令牌。✅ 加 `loadToken` 自增令牌，await 后比对丢弃过期结果。
- [x] **L15** `UserControls/ELFAnalyzerControl.xaml.cs:130` — `ApplyDisplayData` 空值假设与 `SetNoteInfo` 的 `??` 矛盾。✅ Note/Attribute/Exidx 三处改 `IsNullOrEmpty` 空值安全判断。
- [x] **L16** `UserControls/PEAnalyzerControl.xaml.cs:69-70` — null 时静默 return 留空白 tab。✅ 加 `MessageHelper.ShowWarning` 提示。
- [x] **L17** `PEAnalyzer/Parsers/PEHeaderParser.cs:197-222` — 数据目录数未受 SizeOfOptionalHeader 约束。✅ `ReadDataDirectories` 增 `optionalHeaderEnd` 参数并按可选头结尾再夹一次。
- [x] **L18** `PEAnalyzer/Resources/PEResourceParser.Icon.Data.cs:17-34` — 图标数据 Size 无绝对上限。✅ 加 10MB 上限。
- [x] **L19** `PEAnalyzer/Resources/PEResourceParser.Certificate.cs:96` — 对齐 `+7u` 回绕。✅ 改为在 long 上对齐 `((long)dwLength + 7) & ~7L`。
- [x] **L20** `PEAnalyzer/Models/IMAGE_ARCHITECTURE_HEADER.cs` — 死代码 + 注释错。✅ 已 grep 确认全库零代码引用（csproj 为 SDK 自动包含），删除该文件。
- [x] **L21** `PEAnalyzer/Models/IMAGE_DOS_HEADER.cs:29` — `e_lfanew` 规范为 LONG（当前 uint，下游有界，无实际风险）。✅ 补注释留档说明。
- [x] **L22** `PEAnalyzer/Models/ImportFunctionInfo.cs:13` + `ExportFunctionInfo.cs:11` — 序号 `X8` 应 `X4`。✅ 两处已改。
- [x] **L23** `Enums/EMachine.cs:21` — `EM_S390` 注释误为 IBM System/370，应 S/390。✅ 注释改为 `IBM S/390`。
- [x] **L24** `MarkdownToWord/Docx/DocxImageEmbedder.cs:380` — 多图 `pic:cNvPr Id=0` 重复。✅ 改用自增 `drawingId`。
- [x] **L25** `MarkdownToWord/Docx/DocxInlineRenderer.cs:69-77` — EmphasisExtras 下标/上标/插入/高亮误映射。✅ `ResolveEmphasis` 按 DelimiterChar 精确分派；DocxRunStyle 增 4 修饰、BuildRunProperties 按 OOXML 顺序渲染 highlight/vertAlign/下划线。
- [x] **L26** `MarkdownToWord/Docx/DocxBlockRenderer.cs:29/123` — 标题数字前缀误删（如 `2024 年度报告`）。✅ 正则收紧为 `^\s*\d+\.(?:\d+\.?)*[ \t　]+`（要求含点号，纯数字前缀不再剥离）。
- [x] **L27** `MarkdownToWord/Docx/DocxInlineRenderer.cs:111-127` — 超链接无 scheme 白名单（`javascript:`/`file:`）。✅ 仅登记 http/https/mailto，其余降级纯文字。

---

## 验证结果

- [x] `dotnet build PersonalTools.csproj -c Debug` → **0 个警告 0 个错误**（含增量 4 次 + 最终 1 次全量构建均通过）

### 修复统计
- 高危 4：H1(组图标14字节·两代理交叉确认)、H2(CLR架构判断)、H3(exidx O(N×M) DoS)、H4(非法XML字符损坏docx) —— 全部修复。
- 中危 9：M1-M9 —— 8 项修复；**M8 块级 `$$..$$` 经核实为误报**（MathBlock 继承 FencedCodeBlock，已渲染），仅行内 `$..$` 需补。
- 低危 27：L1-L27 —— 全部处理（含 L20 删死代码文件、L21 仅注释留档）。
- 另修复 1 处评审顺带发现：`PEHeaderPresenter.GetBitInfo` 同步适配 H2 新增架构值。

## 已核验为「刻意设计 / 无恙」（不修，仅记录）

- 弱算法（CRC/SHA-224/ECB 可选、AES 无认证）作工具刻意保留 —— 非误用。
- AES IV 契约收紧、RSA OAEP/PKCS#1、PathSafety 多层防穿越、27k 字典无重复键/跨语言一致、58 枚举无值冲突、PE 资源递归防环、CLR ECMA-335 索引宽度 —— 均已核验正确。
