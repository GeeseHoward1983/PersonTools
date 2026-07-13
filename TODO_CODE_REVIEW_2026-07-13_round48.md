# 全量代码评审 第48轮 修复计划（2026-07-13）— opus 首轮（sonnet 因网关故障跳过）

- 阶梯说明：sonnet 通道在 2026-07-10 起持续 502（网关 sonnet 上游不可用，21 代理全灭 + 两次独立探针复现），
  按「以高级别模型为准」升级 opus 继续（opus 覆盖为 sonnet 超集）；haiku 阶段已于第47轮收官。
- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 opus），19 个零发现
- 原始 findings：2 条；主代理（fable）裁决：**确认 2 条**（opus 首轮即两条有效发现，0 误报）

## 批次 1：MarkdownToWord 导出与交互正确性（2 条）

- [x] F1 `MarkdownToWord/Docx/DocxBlockRenderer.cs:116` H6 标题编号丢失。
  Markdown 6 级标题映射 wordLevel=5、styled=false（仅 1-4 级套 Heading 样式获得 Word 自动编号），
  但 StripLeadingNumber 无条件剥离文本编号前缀 → 超 4 级标题的编号被剥离后无任何编号来源。
  修复：仅 styled 分支执行剥离。

- [x] F2 `UserControls/MarkdownToWordControl.xaml.cs:245` LoadMarkdownFile 缺重入令牌。
  六个拖放处理器中唯一无令牌者（其余五个均有 xxxDropToken），连续拖入大文件A→小文件B时，
  B 先显示、A 的慢读盘后完成会把编辑器覆盖回 A（显示与用户最后操作不符）。
  修复：仿兄弟处理器加 mdLoadToken 自增令牌，await 后令牌不符即丢弃。

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十六批(2026-07-13)
