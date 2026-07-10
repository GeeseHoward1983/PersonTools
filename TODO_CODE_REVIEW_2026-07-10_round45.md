# 全量代码评审 第45轮 裁决记录（2026-07-10）— haiku 验证轮 1/3

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku），18 个零发现
- 原始 findings：3 条；主代理（fable）裁决：**确认 0 条，误报 3 条**（全部为重复指控）
- 连续 2 轮归 0（44/45）；验证轮 1/3 通过

## 误报 3 条

| # | 位置 | 指控 | 裁决理由 |
|---|---|---|---|
| 1 | ResourceDirectoryReader.cs:323 | RunAtOffset 异常未恢复流位置 | 第 8 次重复：全库读取点自定位，无受害路径 |
| 2 | DocxBlockRenderer.cs:292 | 列表序号 long 溢出 | 第 2 次重复（第33轮已裁决）：CommonMark/Markdig 限制起始序号 ≤9 位数字，溢出不可达 |
| 3 | MarkdownToWordControl.xaml.cs:87 | NavigationStarting 重复订阅/未解除 | 第 4 次重复；新说法「OnLoaded 重复订阅」不成立：previewReady 分支保证仅首次初始化订阅一次即提前返回；处理器为 static，窗口关闭统一 Dispose |
