# 全量代码评审 第52轮 修复计划（2026-07-13）— sonnet 重新归 0 尝试（确认 1）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 sonnet），20 个零发现
- 原始 findings：1 条；主代理（fable）裁决：**确认 1 条**（0 误报）
- 修复提交后 sonnet 继续归 0 尝试

## 批次 1：导出重入防护（1 条）

- [x] F1 `UserControls/MarkdownToWordControl.xaml.cs:282` Export_Click 无重入防护。
  await 窗口（OOXML 写盘 + Word COM 域更新，WordFieldUpdater 超时上限 60 秒）内按钮仍可点击，
  二次导出与首次并发写同一 docx（SaveFileDialog 预填同名）、多开隐藏 Word 实例，
  轻则 IOException 重则文档损坏。与已裁决「纯计算型 Click 良性」类别本质不同：导出有外部副作用。
  修复：exportInProgress 布尔重入防护（UI 线程串行读写无需锁），原方法体零改动抽为 ExportAsync，
  try/finally 保证任何异常路径都复位。

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十九批(2026-07-13)
