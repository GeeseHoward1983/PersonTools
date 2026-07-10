# 全量代码评审 第35轮 修复计划（2026-07-10）— haiku 验证轮 3/3（被打破）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku），19 个零发现
- 原始 findings：3 条；主代理（fable）裁决：**确认 1 条，误报/不修 2 条**
- 验证轮计数被打破（32/33/34 连续 0 后本轮确认 1）；修复提交后 haiku 需重新归 0 再连续 3 轮验证

## 批次 1：ELF 分析器 UI 一致性（1 条）

- [x] F3 `UserControls/ELFAnalyzerControl.xaml.cs:119` 「版本依赖信息」Tab 未按数据有效性折叠。
  `GetFormattedVersionDependencyInfo` 在无 `.gnu.version_r`（如静态链接 ELF）时返回空串，
  该 Tab 恒可见且空白，与全部兄弟 Tab（SymbolTable/Dynsym/Dynamic/Rela*/Got*/Note/Attribute/Exidx）
  「空数据即 Collapsed」的既定约定不一致。
  修复：ApplyDisplayData 中按 `IsNullOrEmpty` 切换 `ELFVersionDependencyInfoTabItem.Visibility`。

## 误报/不修 2 条

| # | 位置 | 指控 | 裁决理由 |
|---|---|---|---|
| 1 | ELFAnalyzer/Core/ELFAttributeInfo.cs:330 | string.Create 签名不匹配 | `string.Create(IFormatProvider, 插值字符串)` 是 .NET 6+ 标准重载（DefaultInterpolatedStringHandler）；构建 0 错误即证伪 |
| 2 | UserControls/ELFAnalyzerControl.xaml.cs:118 | 版本符号 Tab 未按有效性折叠 | `GetFormattedVersionSymbolInfo` 恒输出解释性文本（节缺失时输出 "not found or empty"），内容永不为空，可见性切换为死代码；恒可见展示解释文本与 readelf 行为一致 |

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十二批(2026-07-10)
