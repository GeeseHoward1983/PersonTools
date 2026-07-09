# 全量代码评审修复清单（2026-07-09，第18轮，分批次执行）

来源：superpowers 多视角评审，25 个并行评审代理（25/25 成功、0 失败，21 个代理零发现）。
findings 共 **5 条**（Important 1 / Minor 4；确认 2 / 疑似 3，均核实属实）。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — PE LCID 表尾部（2，第17轮修复的延续）

- [x] B1-1 (Important/确认) `Version.Language.cs:1168` — 0x580A 标"西班牙语(古巴)"，[MS-LCID] 0x580A=es-419(拉美)，真实 es-CU(0x5C0A) 整表缺失。改 idx216→"拉丁美洲"(三语)；复用杜撰的 idx217(原 French Mayotte)→"古巴"(三语)；映射表新增 0x5C0A→217。
- [x] B1-2 (Minor/确认) `Version.Language.cs` — 尾部 21 个 LCID 超出 [MS-LCID] 真实上限且重复既有国名：French 0x400C-0x7C0C(12)、English 0x6009/0x6809/0x7009(3)、Spanish 0x640A-0x7C0A(6)。从 LanguageIdToIndex 删除，使其回退"未知语言"（脚本核实：删 21 键，名字数组多余项无 LCID 引用、无害）。

## 批次2 — ELF（1）

- [x] B2-1 (Minor/疑似→核实属实) `ELFAnalyzer/Core/ELFHeaderDescriptions.cs:473` — 内存模型(EF_SPARCV9_MM)仅 V9 有意义，但 EM_SPARC/SPARC32PLUS 也附加，致 32 位 SPARC(flags 常为 0)误显"tso"。GetSPARCFormattedELFFlags 加 isV9 参数：仅 V9 附加 tso/pso/rmo，非 V9 只返回命中的位标志。

## 批次3 — UserControls（1）

- [x] B3-1 (Minor/疑似→核实属实) `UserControls/HashComputeControl.xaml.cs:213` — 大文件(超 hex 上限)分支只更新提示，未清空 SHA3 输入框：拖入大文件后 SHA3 框保留上一个小文件的 hex，用户算 SHA3 得旧文件摘要、与上方各哈希不一致。else 分支补 SHA3InputTextBox.Clear() + SHA3ResultLabel 复位。

## 批次4 — Enums（1）

- [x] B4-1 (Minor/疑似→核实属实) `Enums/EMachine.cs:189` — 值 248 官方 gABI 登记为 EM_GRAPHCORE_IPU(Intelligent Processing Unit)，"GCN" 系 AMD 架构术语、Graphcore 无 GCN。改名 EM_GRAPHCORE_IPU + 注释，同步 ELFHeaderDescriptions.cs:257 引用与描述。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 提交「全量代码评审修复第十八批(2026-07-09)」（不 push）。

---

## 完成情况

**5/5 全部修复并通过编译**（3 条疑似核实后均属实，无误报）：Important 1 + Minor 4。
findings 收敛：17 → 13 → 7 → 10 → 5 → 18 → 7 → 5。本轮多为前几轮 LCID/SPARC 修复的尾部收尾。
