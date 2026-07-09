# 全量代码评审修复清单（2026-07-09，第25轮，分批次执行）

来源：superpowers 多视角评审，27 个并行评审代理（27/27 成功、0 失败，23 个代理零发现）。
findings 共 **9 条**（Important 7 / Minor 2；确认 8 / 疑似 1），均核实属实。
状态图例：`[ ]` 待修 · `[x]` 已修复 · `[!]` 误报不修。

## 批次1 — PE CLR 元数据（1）

- [x] B1-1 (Minor/疑似→核实属实) `PEAnalyzer/Parsers/PEParser.CLR.Metadata.cs:218` — #~ 表流未处理 HeapSizes 的 ExtraData 位(0x40)：置位时行数数组后有额外 4 字节(ECMA-335)，未跳过致 tablesDataOffset 提前 4 字节、TypeDef 及后续表偏移全部错位。读完 rowCounts 后若 (heapSizes & 0x40)!=0 则 fs.Position += 4（含越界校验）。

## 批次2 — MySQL 残留（2，债基本清空）

- [x] B2-1 (Important/确认) `MySqlErrors.TraditionalChinese.cs:331` — 1134 "符合的列"→"符合的行"（Rows matched=行，本档 列=column/行=row，SC 作行）。修正第21轮引入的用词。
- [x] B2-2 (Important/确认) `MySqlErrors.SimplifiedChinese.cs:342` — 1145 "错误"→"过长"（EN "is too long"，对齐 TC "過長"）。

## 批次3 — SqlServer 错误码↔含义错映射（6 码 × 三语，官方文档核实）

经 Microsoft Learn 官方错误表核实（Msg 150 采用最著名的经典含义，现值均非任何官方版本）：
- [x] 150 三语 — "Foreign key reference…"（非任一版本）→ "外连接的两个词条都必须包含列"（Both terms of an outer join must contain columns）
- [x] 310 三语 — "参数过多" → "MAXRECURSION 值超过允许最大值 32767"（官方确证）
- [x] 517 三语 — "长度/精度无效" → "向 datetime 列添加值导致溢出"（官方确证）
- [x] 550 三语 — "未授权操作" → "违反视图 WITH CHECK OPTION 约束"（官方确证）
- [x] 1007 三语 — "数据库已存在" → "值超出数值表示范围（最大精度 38）"（官方确证；database already exists 实为 Msg 1801，本档 1801 已正确）
- [x] 1511 三语 — "无法删除聚集索引" → "排序无法与事务日志协调"（官方确证）

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 提交「全量代码评审修复第二十五批(2026-07-09)」（不 push）。

---

## 完成情况

**9/9 全部修复并通过编译**（1 条疑似核实属实）。MySQL 债降至 2 条，本轮暴露 SqlServer 错误码错映射（6 码，官方文档逐一核实）。
findings 收敛：…→26→10→9。MySQL 翻译债接近清空；SqlServer 档质量存疑，下轮拟穷尽核对该档（约 60 键）。
