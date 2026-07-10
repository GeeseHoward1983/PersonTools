# 全量代码评审 第36轮 裁决记录（2026-07-10）— haiku 重新归 0

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku），18 个零发现
- 原始 findings：3 条；主代理（fable）裁决：**确认 0 条，误报/不修 3 条**
- 第32批修复后 haiku 重新归 0；验证轮重新计数（第37/38/39轮）

## 误报/不修 3 条

| # | 位置 | 指控 | 裁决理由 |
|---|---|---|---|
| 1 | ELFAnalyzer/Core/ELFAttributeInfo.cs:681 | ULEB128 shift=28 时左移溢出 | C# `<<` 移出位直接丢弃、不抛异常；shift=28 保留该字节是覆盖 32 位值第 28-31 位所必需（注释明示设计）；建议的 shift<=24 反而使 ≥2^28 的值解码错误 |
| 2 | PEAnalyzer/Parsers/PEParser.CLR.Helpers.cs:103 | ReadStringFromHeap 异常未恢复流位置 | 流位置类第 4 次重复报告：全库读取点自定位，无受害路径（第31轮起既定裁决） |
| 3 | MarkdownToWord/WordFieldUpdater.cs:32 | Thread 未 Dispose | System.Threading.Thread 不实现 IDisposable，指控不成立 |
