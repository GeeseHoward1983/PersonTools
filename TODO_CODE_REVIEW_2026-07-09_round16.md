# 全量代码评审修复清单（2026-07-09，第16轮，分批次执行）

来源：superpowers 多视角评审，25 个并行评审代理（25/25 成功、0 失败，18 个代理零发现）。
findings 共 **18 条**（Important 2 / Minor 16；确认 12 / 疑似 6）。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — 代码（4）

- [x] B1-1 (Important/确认) `PEAnalyzer/Resources/PEResourceParser.Icon.Named.cs:28` — 补 TryConsumeEntry 短路，全项目手写条目循环全部纳入扫描会话预算。
- [x] B1-2 (Important/确认) `MarkdownToWord/Docx/DocxImageEmbedder.cs:203` — catch 扩为兼捕 IOException/NotSupportedException/UnauthorizedAccessException/SecurityException，超长/异形 URL 降级占位不中断导出。
- [x] B1-3 (Minor/疑似→核实属实) `ELFAnalyzer/Core/ELFHeaderDescriptions.cs` — SPARC e_flags 位表按 binutils readelf 重写为 v8+/ultrasparcI/halr1/ultrasparcIII/ledata，并新增内存模型掩码字段(EF_SPARCV9_MM=0x3→tso/pso/rmo)。
- [x] B1-4 (Minor/疑似→核实属实) `ELFAnalyzer/Core/ELFExidxInfo.cs:342` — maxSymbolSize==0 时跳过窗口剪枝(仅硬步数上界兜底)，全零大小符号表不再丢失 exidx 符号注解。

## 批次2 — Enums（1）

- [x] B2-1 (Minor/疑似→核实为杜撰、删除) `Enums/EMachine.cs:193-196` — EM_COGEY=303/EM_COFFEE=304/EM_CISCO_IOS=305/EM_CISCO_IOS64=306：三方独立核实（SCO gABI 官方表无、Web 搜索作为 ELF 常量零结果、评审判断）+ 本文件 NFP/VE 误标先例，确认为杜撰值，连同 ELFHeaderDescriptions.cs 引用一并删除；未知 e_machine 走 default 显示 UNKNOWN(n)。

## 批次3 — ConstString 繁体术语收敛清扫（脚本按累计术语对照表全量扫描 4 档，共 44 处）

- [x] B3-1 (Minor/确认) MySqlErrors.TC 16 处：句柄→控制代碼、套接字→通訊端、插件→外掛程式(×8)、訪問→存取(×4)、配置文件→設定檔、非法尋址→非法查找(ESPIPE 对齐 Linux/Mac)。
- [x] B3-2 (Minor/确认) WindowsSystemErrors.TC 26 处：驅動器→磁碟機(×5)、微型端口→微型連接埠、幀緩衝區→影格緩衝區(×4)、地址→位址(×16，逐行核对，電子郵件地址已脚本 sentinel 保护)。
- [x] B3-3 (Minor/确认) LinuxErrno.TC:50 / MacErrno.TC:98 — 標識符→識別碼（各 1 处）。

## 批次4 — ConstString 简体（2）

- [x] B4-1 (Minor/确认) `MySqlErrors.SimplifiedChinese.cs:475` — 键 1278 杜撰选项名 SKIP_SLAVE_COUNTER 更正为 --skip-slave-start（对齐 EN/TC 语义）。
- [x] B4-2 (Minor/疑似→核实属实) `MySqlErrors.SimplifiedChinese.cs:715` — 键 1518 反译（EN="不允许在此命令关闭binlog"），简中与繁中(:715)均改为「不允许在此命令上关闭二进制日志(binlog)」。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 提交「全量代码评审修复第十六批(2026-07-09)」（不 push）。

---

## 完成情况

**18/18 全部修复并通过编译**（6 条疑似逐一核实：5 条属实、EMachine 1 条经三方核实为杜撰删除，无误报）：Important 2 + Minor 16。
findings 收敛：17 → 13 → 7 → 10 → 5 → 18（本轮回升因繁体术语深挖，但代码类仅 4 条且均为边角健壮性/规范，已全部闭环）。
