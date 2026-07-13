# 全量代码评审 第49轮 修复计划（2026-07-13）— sonnet 阶段第 1 轮

- 阶梯说明：sonnet 网关恢复（用户确认），按指示恢复 sonnet 阶段：sonnet 归 0 后再升级 opus。
  第48轮（opus，网关故障期替代轮）的 2 处修复保留（第36批）。
- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 sonnet）；首跑 5 个视角 502，
  经 resumeFromRunId 断点续跑补齐（16 个缓存 + 5 个重跑），最终 21 代理全成功、20 个零发现
- 原始 findings：1 条；主代理（fable）裁决：**确认 1 条**（0 误报）

## 批次 1：MarkdownToWord Figure 题注正确性（1 条）

- [x] F1 `MarkdownToWord/Docx/DocxBlockRenderer.cs:192` Figure(^^^) 容器题注双重渲染且编号挂错。
  Figure 容器落泛化 ContainerBlock 分支：容器内图片段以 **alt 文字**生成带 SEQ 编号的题注，
  FigureCaption（真实题注）又单独渲染为**无编号**斜体段——题注出现两次、真实题注拿不到编号。
  修复：RenderBlock 在 ContainerBlock 分支前特判 Figure；FigureCaption 文本经
  ctx.PendingFigureCaption 交给容器内首个成功嵌入的图片作 SEQ 题注（优先于 alt）；
  容器内无成功嵌入图片时回退原无编号渲染（题注不丢失）；
  DocxImageEmbedder 提取 ExtractInlineText 供 alt 与题注共用。

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十七批(2026-07-13)
