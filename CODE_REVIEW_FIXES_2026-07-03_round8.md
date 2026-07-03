# 全量代码评审修复清单（第八轮 · 2026-07-03）

> 来源：8 个并行多视角评审代理对全量代码的 26 条 findings（详见 `E:\tmp\review-round8\FINDINGS_2026-07-03_round8.md`）。
> 本文件为修复进度跟踪，**每完成一项即更新对应勾选与备注**。
> 约定：`[ ]` 待修 / `[x]` 已修 / `[-]` 经复核不修（附原因）。
> 验证：全部修完后 `dotnet build PersonalTools.csproj -c Debug`，以「0 个错误」为准。

---

## 🔴 高危（1）

- [x] **H1** `ELFAnalyzer/Core/VersionSymbolParser.ParseDependencies.cs`（WalkVerneed）— verneed 双循环无全局 vernaux 步数上界 + offset 未夹节边界 → O(N×M) 挂死/OOM。修法：加全局累计步数硬上界 + offset/auxOffset 夹到 verneed 节 [sh_offset, sh_offset+sh_size)。✅ WalkVerneed 新增 sectionEnd 参数（夹文件长度）+ MaxTotalVernaux=1,000,000 全局步数上界；新增 internal GetSectionEndOffset（sh_offset+sh_size 回绕/越界退回文件末尾）；解析路径与 VersionSymbolFormatter 格式化路径两个调用点同步传节边界。

## 🟠 中危（8）

- [x] **M1** `ELFAnalyzer/Core/VersionSymbolParser.ParseDefinitions.cs`（ParseVerDefEntries）— verdef 单循环 O(count×strTab)。修法：步数按节内条目数封顶 + offset 夹节边界。✅ offset 上界改用 GetSectionEndOffset（夹节+文件）+ MaxVerDefEntries=1,000,000 条目硬上界。
- [x] **M2** `ELFAnalyzer/Core/ELFRelocation.cs` — 缺 EM_RISCV(243) 分派且无 RISCVRelocationType 枚举。修法：新建枚举 + 加 switch 臂。✅ 新建 `Enums/RISCVRelocationType.cs`（NONE/32/64/RELATIVE/JUMP_SLOT/TLS/PCREL 等 65 项）+ ELFRelocation switch 加 `EM_RISCV => "R_RISCV_"` 臂。
- [x] **M3** `MarkdownToWord/Docx/DocxInlineRenderer.cs` — TaskList 复选标记丢弃。修法：case TaskList 按 Checked 追加 ☑/☐。✅ 加 using + case TaskList。
- [x] **M4** `MarkdownToWord/Docx/DocxInlineRenderer.cs` + `DocxBlockRenderer.cs` — 脚注引用/定义丢失。修法：FootnoteLink 追加上标编号 + FootnoteGroup 带编号脚注区。✅ 内联 case FootnoteLink（上标 Order）+ 块级 RenderFootnoteGroup（分隔线+编号前缀，MdFootnote 别名避开 OpenXml.Footnote 冲突）。
- [x] **M5** `MarkdownToWord/Docx/DocxBlockRenderer.cs` — 定义列表术语行丢弃。修法：case DefinitionTerm 加粗渲染。✅ 加 case + RenderDefinitionTerm（加粗段落）。
- [x] **M6** `MarkdownToWord/Docx/DocxTableRenderer.cs` — 网格表合并单元格丢失。修法：ColumnSpan→GridSpan、RowSpan→VerticalMerge。✅ 提取 GetColumnCount，Render 按占用几何重建行（rowSpanRemaining/Width 跟踪续行补 vMerge=Continue）；BuildCell 加 columnSpan/verticalMergeRestart 出 GridSpan/vMerge=Restart；新增 BuildContinuationCell/BuildEmptyCell。
- [x] **M7** `Utils/Hash/Sha224.cs` — 整段复制致大文件内存翻倍 OOM。修法：按块流式压缩，仅尾块小缓冲。✅ 抽出 ProcessBlock，HashData 直接按 64 字节块压缩输入（h 用 Span<uint>[8]），尾块仅 128 字节 stackalloc，去掉整段 padded 复制。
- [x] **M8** `UserControls/PEAnalyzerControl.xaml.cs`（DisplayDependenciesAsync）— 依赖树 ItemsSource 写回未受 loadToken 守卫。修法：捕获 token，写前比对丢弃过期。✅ DisplayDependenciesAsync 加 token 参数，await 后写 ItemsSource 前比对 loadToken，过期丢弃；LoadPEFile 传入 token。

## 🟡 低危（17）

- [x] **L1** `ELFAnalyzer/Core/ELFHeaderDescriptions.cs:82` — EM_S390 描述误为 System/370。修法：改 "IBM S/390"。✅ 已改。
- [x] **L2** `ELFAnalyzer/Core/ELFNoteInfo.cs:109-111`（GetBuildID）— build-id hex 无显示上限。修法：加显示字节上限。✅ 加 MaxBuildIdBytes=64KB 上限，超限追加省略号（与 FormatNoteDescriptionData 一致）。
- [x] **L3** `Enums/EMachine.cs:190-191` — EM_RISCV32=250/EM_RISCV64=251 杜撰值。修法：删除（250/251 官方为 NFP/VE）。✅ 改名为 EM_NFP=250(Netronome Flow Processor)/EM_VE=251(NEC SX-Aurora VE)，ELFHeaderDescriptions 描述表两条同步改正（strictly 优于删除，为这两个真实架构给正确名）。
- [x] **L4** `ELFAnalyzer/UIHelper/RelocationHelper.cs:156-167`（SplitRelocInfo）— 未处理 MIPS64 r_info 布局。修法：EM_MIPS+CLASS64 单独拆分。✅ SplitRelocInfo 签名改传 Parser，EM_MIPS/EM_MIPS_RS3_LE + 64 位按字节序拆分（LE: sym=低32/type=最高字节；BE: sym=高32/type=最低字节）。
- [x] **L5** `ELFAnalyzer/Core/ELFSectionHeaderReader.cs:21-34` — 标志字符表缺 SHF_ARM_PURECODE。修法：追加 (SHF_ARM_PURECODE,'y')。✅ 已在 s_sectionFlagChars 末尾追加。
- [x] **L6** `PEAnalyzer/Parsers/PEExportParser.cs:64-69` — EAT RVA==0 空槽当幻影导出。修法：functionRVA==0 跳过。✅ 循环内加 `if (functionAddresses[i]==0) continue;`。
- [x] **L7** `PEAnalyzer/PEHeaderDescriptions.cs:185-200`（GetDetailedFileType）— 缺 subsystem case 8。修法：加 `8 => " (Native Windows)"`。✅ 已加。
- [x] **L8** `PEAnalyzer/Resources/PEResourceParser.Icon.Data.cs:220`（IsIconData）— `==40` 与 ConvertDibToIco `>=40` 矛盾。修法：改 `>= 40`。✅ 已改，V4/V5 头位图不再被闸门丢弃。
- [x] **L9** `PEAnalyzer/Resources/PEResourceParser.Certificate.cs:90` — 0x0003 误标 PKCS#1。修法：改 "Reserved(0x0003)"。✅ 已改。
- [x] **L10** `PEAnalyzer/Resources/PEResourceParser.Version.Language.cs` — 语言名数组错位。修法：复核 0x0486–0x048C 段。✅ **经程序化核查纠正 finder 的错误诊断**：错位仅在索引 103–106（0x0486–0x048C），112+ 本就正确，故不能「插入一项」（会推错 112+）；改为就地把四槽改为 K'iche'/Kinyarwanda/Wolof/Dari（三语数组各改 4 行、长度不变），末尾错配的 SiSwati 丢弃。核查确认 0x0804/0x0C0A 等仍正确。
- [x] **L11** `MarkdownToWord/Docx/DocxBlockRenderer.cs` — Figure 题注丢弃。修法：case FigureCaption 渲染 Inline。✅ 加 case + RenderFigureCaption（居中斜体段落）。
- [x] **L12** `MarkdownToWord/Docx/DocxInlineRenderer.cs` — 缩写替换词丢弃。修法：case AbbreviationInline 输出标签文本。✅ 加 case AbbreviationInline（输出 Abbreviation.Label）。
- [x] **L13** `Utils/AppLogger.cs:49-64` — Mutex 超时后仍写盘（绕过 TOCTOU 保护）。修法：超时跳过写入/滚动。✅ 滚动+写盘包进 `if (mtx==null || acquired)`，超时(acquired=false)跳过本次写入。
- [x] **L14** `UserControls/AesEncryptionControl.xaml.cs` 等（Crc/Hash-SHA3/Rsa-keygen）— UI 线程同步重活。修法：compute 步骤 await Task.Run。✅ AES 加解密、SHA3 计算、CRC 计算、RSA keygen 均改 async + UI 线程捕获参数后 `await Task.Run(纯计算)`。
- [x] **L15** `UserControls/HashComputeControl.xaml.cs` 等（Crc/Base64/Aes/Rsa 拖放）— 缺重入令牌。修法：加自增令牌比对。✅ 5 个控件各加自增 dropToken 字段，拖放 await 后比对丢弃过期结果。
- [x] **L16** `UserControls/DependencyNode.cs:131-154`（TryParsePE）— 吞异常不记日志 + 无失败标记。修法：catch 补 AppLogger + 节点失败标记。✅ 四个 catch 各补 AppLogger.Log（含 path）；实现 INotifyPropertyChanged，解析失败置 parseFailed 并 raise Display，节点显示「(解析失败)」。
- [x] **L17** `UserControls/DependencyNode.cs:14,35` — MaxDepth=16 静默截断。修法：达上限插占位提示节点。✅ 构造函数对 depth>=MaxDepth 且可解析节点插「(超出最大展开深度)」占位提示；EnsureLoadedAsync 对 depth>=MaxDepth 直接返回，保留提示不再下钻。

---

## 验证结果

- [x] `dotnet build PersonalTools.csproj -c Debug` → **0 个警告 0 个错误**（增量+完整构建均确认；大量新增 Markdig/OpenXml API 用法编译通过）

### 修复统计
- 高危 1：H1（verneed 双循环 DoS）—— 已修（双上界）。
- 中危 8：M1-M8 —— 全部修复。
- 低危 17：L1-L17 —— 全部处理。其中 **L10 纠正了 finder 的错误诊断**（错位仅局限 103–106 段，非全表；就地改正而非插入，避免推错 112+）。

### 关键决策留档
- **H1/M1（verneed/verdef DoS）**：与七轮已修 exidx 回扫同款「offset 夹节边界 + 全局步数硬上界(100 万)」，是同一 O(N×M) 范式的漏网入口。
- **L3（EM_RISCV32/64 杜撰值）**：改名 EM_NFP=250/EM_VE=251（官方登记）而非删除，为这两个真实架构给正确名。
- **L10（语言数组错位）**：经程序化核查（映射 232 项密集 0..231、数组 233 项、高 LANGID 全对齐）确认错位仅在索引 103–106，就地改四槽为 K'iche'/Kinyarwanda/Wolof/Dari，长度不变。
- **M6（网格表合并）**：按占用几何重建行以正确产出 gridSpan/vMerge，非简单加属性。
