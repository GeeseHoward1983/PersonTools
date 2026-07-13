# 全量代码评审 第51轮 修复计划（2026-07-13）— sonnet 验证轮 1/3（被打破）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 sonnet），20 个零发现
- 原始 findings：1 条；主代理（fable）裁决：**确认 1 条**（0 误报）
- 验证轮被打破；修复提交后 sonnet 需重新归 0 再连续 3 轮验证

## 批次 1：Word COM 互操作资源残留（1 条）

- [x] F1 `MarkdownToWord/WordFieldUpdater.cs:65` 点号链中间 COM 对象未释放。
  `app.Documents` / `doc.Fields` / `doc.TablesOfContents(.Item)` 产生的中间 RCW 从未
  Release，仅 doc/app 有 FinalReleaseComObject，且无 GC 兜底（Office 自动化 KB317109 经典缺陷）。
  加重因素：RCW 建在专用 STA 线程上，线程结束后终结器无法向已死单元封送释放调用，
  Visible=false 的 WINWORD.EXE 在正常路径也会长期残留（代码注释原本只在超时路径担心此事）。
  修复：CleanUp 末尾、STA 线程尚存时执行两轮 GC.Collect+WaitForPendingFinalizers，
  确定性回收全部中间 RCW（对 dynamic 晚绑定演化保持稳健，优于逐个持名释放）。

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十八批(2026-07-13)
