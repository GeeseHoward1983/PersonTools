# 全量代码评审 第72轮 修复计划（2026-07-13）— opus 验证轮 3/3（被打破）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 opus），20 个零发现
- 原始 findings：1 条；主代理（fable）裁决：**确认 1 条**（0 误报）
- 验证轮 3/3 被打破（69/70/71 三连零后）；修复提交后 opus 重新归 0 计数

## 批次 1：图标扫描累计读取字节 DoS 加固（1 条）

- [x] F1 `PEAnalyzer/Resources/PEResourceParser.Icon.Data.cs:63` 图标数据读取缺累计字节预算。
  `TryAddIcon` 的 256MB/4096 项上限只约束【已存储】字节，但 `reader.ReadBytes((int)dataEntry.Size)`
  在存储上限之前就分配并读取至多 10MB；扫描会话仅限【条目数】(MaxEntriesPerWalk=65536)，
  畸形 PE 用大量资源数据项重复指向同一 10MB 区域，可在条目上限内放大出约 640GB 的
  一次性 ReadBytes 分配/IO（CPU/IO 型 DoS），既有存储上限拦不住。
  修复：为扫描会话新增累计读取字节预算 `MaxScanReadBytes=512MB`（`_scanReadBytes` ThreadStatic，
  与 `_walkEntries` 在 BeginScan/Dispose/WalkEntries-finally 三处同步复位），
  `ReadAndProcessIconDataEntry` 读前 `TryConsumeReadBudget(Size)`，预算耗尽即跳过该次读取。
  512MB 远高于 shell32 级图标大户，正常文件不受影响。

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第四十六批(2026-07-13)
