# 全量代码评审 第44轮 裁决记录（2026-07-10）— haiku 重新归 0

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku），19 个零发现
- 原始 findings：2 条；主代理（fable）裁决：**确认 0 条，误报 2 条**（均为清单内明确重复）
- 第35批修复后 haiku 重新归 0；验证轮重新计数（第45/46/47轮）

## 误报 2 条

| # | 位置 | 指控 | 裁决理由 |
|---|---|---|---|
| 1 | MarkdownToWord/Docx/DocxImageEmbedder.cs:323 | BitmapDecoder 未 using | 第 2 次重复（第39轮已裁决）：WPF BitmapDecoder 不实现 IDisposable |
| 2 | UserControls/MarkdownToWordControl.xaml.cs:157 | RefreshPreview 缺 ObjectDisposedException | 第 3 次重复（第38轮已裁决）：所有调用路径均无法在 Preview.Dispose 后到达 |
