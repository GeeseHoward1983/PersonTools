# 全量代码评审修复清单（2026-07-09，第21轮，分批次执行）

来源：superpowers 多视角评审，25 个并行评审代理（25/25 成功、0 失败，23 个代理零发现）。
findings 共 **5 条**（Important 4 / Minor 1；确认 4 / 疑似 1，均核实属实）。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — MarkdownToWord（1，第20轮 B4 的漏网）

- [x] B1-1 (Important/疑似→核实属实) `DocxTableRenderer.cs:147` — BuildTableGrid 按未夹取的 GetColumnCount 生成 GridColumn，绕过第20轮 Render 里加的列数上限，超多列畸形表致内存/输出爆炸。把 MaxTableCells 提升为类级常量，Render 与 BuildTableGrid 共用；BuildTableGrid 的列数同样夹取到 MaxTableCells。

## 批次2 — ConstString MySQL 繁体错译（4）

- [x] B2-1 (Important/确认) `MySqlErrors.TraditionalChinese.cs:476` — 1279 ER_UNTIL_COND_IGNORED："完全啟動" 与 EN "not to be started" 反义、"直到條件" 误译 UNTIL 子句（SC 正确）。改"SQL執行緒未啟動，因此UNTIL選項被忽略"。
- [x] B2-2 (Important/确认) 同文件:331 — 1134 ER_UPDATE_INFO 标签错为"記錄/重複"，EN 为 Rows matched/Changed（SC 正确）。改"符合的列: %ld 已變更: %ld 警告: %ld"（占位符 3×%ld 一致）。
- [x] B2-3 (Important/确认) 同文件:404 — 1207 ER_READ_ONLY_TRANSACTION 臆造"嘗試重新啟動事務"且丢失"无法取得更新锁"原意。改"在READ UNCOMMITTED事務期間無法取得更新鎖"。
- [x] B2-4 (Minor/确认) 同文件:542 — 1345 ER_VIEW_NO_EXPLAIN 臆造 SELECT 且漏译"缺少底层表权限"（SC 正确）。改"無法發出EXPLAIN/SHOW; 缺少基礎表的權限"。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 提交「全量代码评审修复第二十一批(2026-07-09)」（不 push）。

---

## 完成情况

**5/5 全部修复并通过编译**（1 条疑似核实属实，无误报）：Important 4 + Minor 1。
findings 收敛：17 → 13 → 7 → 10 → 5 → 18 → 7 → 5 → 3 → 4 → 5。持续低位，本轮为表格防护漏网 + MySQL 繁体错译。
