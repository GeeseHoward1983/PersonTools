# 全量代码评审 第37轮 修复计划（2026-07-10）— haiku 验证轮 1/3（被打破）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku），19 个零发现
- 原始 findings：4 条；主代理（fable）裁决：**确认 1 条，误报/不修 3 条**
- 验证轮再次被打破；修复提交后 haiku 需重新归 0 再连续 3 轮验证

## 批次 1：ELF Note 解析健壮性（1 条）

- [x] F1 `ELFAnalyzer/Core/ELFNoteInfo.cs:45` `FormatNoteSection` 中 `Math.Min(offset + size, fileLen)`
  的 offset/size 均为不可信节头原始 ulong（sh_offset/sh_size 可为任意 64 位值），`offset + size`
  回绕后 endOffset 变为极小值而非文件长度，「夹紧到文件实际长度」的注释意图失效，
  整段 note 被静默跳过（而非解析文件内可读部分）。
  修复：减法式防回绕判断，与 GetSectionEndOffset / IsRangeWithin 的既定处理同款。
  说明：此前误报清单中「偏移加法回绕不可达」的论证仅适用于「偏移已被循环边界约束 < 2^31」
  的场景，本处两个操作数皆为原始头字段，不在该论证覆盖范围内——本条为 haiku 的有效发现。

## 误报/不修 3 条

| # | 位置 | 指控 | 裁决理由 |
|---|---|---|---|
| 2 | ELFAnalyzer/Core/ELFDynamicInfo.cs:56 | 枚举名切片可能越界 | Enum.GetValues 只迭代已声明成员，ToString 恒返回声明名；DynamicFlags1 全部 31 个成员均带 DF_1_ 前缀，切片恒安全 |
| 3 | UserControls/ELFSymbolTableControl.xaml.cs:2 | 缺 using System.Collections.Generic | .NET 10 项目启用 ImplicitUsings（obj/GlobalUsings.g.cs），构建 0 错误证伪 |
| 4 | UserControls/ELFAnalyzerControl.xaml.cs:8 | 缺 using System 等 | 同上，隐式全局 using |

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十三批(2026-07-10)
