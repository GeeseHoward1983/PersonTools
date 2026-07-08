# 全量代码评审修复清单（2026-07-08，第14轮，分批次执行）

来源：superpowers 多视角评审，25 个并行评审代理（25/25 成功、0 失败，18 个代理零发现）。
findings 共 **10 条**（Important 2 / Minor 8；状态：确认 9 / 疑似 1）。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — Enums（1）

- [x] B1-1 (Important/确认) `Enums/ProgramHeaderFlags.cs:9` — PF_MASKOS/PF_MASKPROC 按 gABI 改为 0x0FF00000/0xF0000000。

## 批次2 — ELF（1）

- [x] B2-1 (Minor/疑似→核实属实) `ELFAnalyzer/Core/VersionSymbolFormatter.cs:118` — 正常链接器对变长 verdef 节置 sh_entsize=0，估算容量恒 0 使「past end of section」对正常 .so 恒误报；改为仅在 estimatedCapacity>0 时比较。

## 批次3 — PE（1，第13轮 ScanScope 的回归）

- [x] B3-1 (Minor/确认) `PEAnalyzer/Resources/PEResourceParser.Version.cs:19` — ParseVersionInfo 同样包 using BeginScan()；并让 BeginScan 进入最外层会话时防御性重置 _walkVisited/_walkEntries，杜绝任何裸扫描残留泄漏。

## 批次4 — MarkdownToWord（1）

- [x] B4-1 (Important/确认) `MarkdownToWord/Docx/DocxBlockRenderer.cs:291` — 有序列表改为自 OrderedStart 起顺序递增（CommonMark：仅首项序号决定起始），不再逐项采用源字面 Order。

## 批次5 — ConstString（6）

- [x] B5-1 (Minor/确认) `MySqlErrors.TraditionalChinese.cs` — 台式术语统一 7 处：查詢緩存×2/鍵緩存×1→快取、存儲程序×3/存儲函數×1→儲存。
- [x] B5-2 (Minor/确认) `LinuxErrno.TraditionalChinese.cs` — 陆式词汇→台式共 18 处：網絡×5→網路、軟件×2→軟體、計算機×1→電腦、內存×2→記憶體、硬件×1→硬體、鏈接×6→連結、字節×1→位元組。
- [x] B5-3 (Minor/确认) `MacErrno.TraditionalChinese.cs` — 同类 9 处（WindowsSystemErrors.TC 扫描 0 处、已是台式）。
- [x] B5-4 (Minor/确认) `MySqlErrors.SimplifiedChinese.cs:352` — 键 1155 "fctnl"→"fcntl"（译文同步对齐 EN/TC 语义为 "fcntl()返回错误"）。
- [x] B5-5 (Minor/确认) `WindowsSystemErrors.SimplifiedChinese.cs:6551` — UCEERR_CHANNELSYNCTABANDONED→UCEERR_CHANNELSYNCABANDONED。
- [x] B5-6 (Minor/确认) `OdbcErrors.cs:86` — S1094 英文→"Invalid scale value"（原与 S1104 重复且与中文矛盾）。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 提交「全量代码评审修复第十四批(2026-07-08)」（不 push）。

---

## 完成情况

**10/10 全部修复并通过编译**（1 条「疑似」项核实属实，无误报）：Important 2 + Minor 8。
findings 收敛：17 → 13 → 7 → 10（本轮 6 条为 ConstString 文案细节，代码类仅 4 条且含 1 条上轮回归，已闭环）。
