# 全量代码评审 第56轮 修复计划（2026-07-13）— sonnet 验证轮 1/3（被打破）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 sonnet），20 个零发现
- 原始 findings：1 条；主代理（fable）裁决：**确认 1 条**（0 误报）
- 验证轮被打破；修复提交后 sonnet 继续归 0 尝试

## 批次 1：PE 依赖树双击的加载令牌（1 条）

- [x] F1 `UserControls/PEAnalyzerControl.xaml.cs:196` DependencyTree_MouseDoubleClick 缺 I6 令牌比对。
  await EnsureLoadedAsync 期间加载新文件后，过期续体把旧文件依赖节点的导入/导出写进表格，
  与树/其余面板显示的新文件「新旧混合」；LoadPEFile/DisplayDependenciesAsync 均有令牌检查，唯此入口遗漏。
  修复：await 前记录 loadToken，await 后不符即 return（不自增——双击不是新的加载代际）。
  （DependencyNode_Expanded 仅写已脱离显示的旧树节点、不触碰表格，无需修改。）

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第四十二批(2026-07-13)
