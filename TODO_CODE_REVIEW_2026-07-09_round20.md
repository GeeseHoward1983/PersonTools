# 全量代码评审修复清单（2026-07-09，第20轮，分批次执行）

来源：superpowers 多视角评审，25 个并行评审代理（25/25 成功、0 失败，21 个代理零发现）。
findings 共 **4 条**（Important 1 / Minor 3；确认 1 / 疑似 3，均核实属实）。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — PE 编译器版本（1）

- [x] B1-1 (Minor/疑似→核实属实) `PEAnalyzer/PEHeaderDescriptions.cs:95` — isNetAssembly(=CLRInfo!=null) 仅在 default 分支生效，MajorLinkerVersion 落在 6-15 的 .NET 程序集被误标"Microsoft Visual C++"。改为 switch 前先判 isNetAssembly，是则统一返回"Microsoft .NET Compiler"；移除 default 分支中已冗余的 isNetAssembly 判断。

## 批次2 — PE LCID（1）

- [x] B2-1 (Minor/确认) `Version.Language.cs:148` — 0x0845=bn-BD(孟加拉国)、0x0846=pa-Arab-PK(巴基斯坦) 被误标"(印度)"。三数组 idx132/133 改为 Bengali(Bangladesh)/Punjabi(Pakistan) 与简繁对应。

## 批次3 — PE 版本覆盖保护（1）

- [x] B3-1 (Minor/疑似→核实属实) `Version.Helpers.cs:51` — wValueLength==0(仅字符串版本)分支即使已从 StringTable 取得有效 FileVersion 仍返回 false，succeeded 未置位，后续畸形兄弟叶子可用占位串覆盖正确版本。PEAdditionalInfo 加 FileVersionResolved 标志（StringTable 的 FileVersion setter 置位），该分支改 return peInfo.AdditionalInfo.FileVersionResolved，成功取得版本即短路后续兄弟。

## 批次4 — MarkdownToWord（1）

- [x] B4-1 (Important/疑似→核实属实) `MarkdownToWord/Docx/DocxTableRenderer.cs:76` — 稀疏行按 totalColumns 补空单元格，畸形管道表(分隔行声明极多列 × 多稀疏行)生成 O(N×M) 单元格致 OOM。加 MaxTableCells=200_000 整表单元格预算：列数先夹到预算内(防 new int[] 单次分配过大)，生成时逐单元格扣预算、耗尽即停止渲染该表。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 提交「全量代码评审修复第二十批(2026-07-09)」（不 push）。

---

## 完成情况

**4/4 全部修复并通过编译**（3 条疑似核实后均属实，无误报）：Important 1 + Minor 3。
findings 收敛：17 → 13 → 7 → 10 → 5 → 18 → 7 → 5 → 3 → 4。低位波动，均为边角健壮性/数据细节。
