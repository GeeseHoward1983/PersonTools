# 全量代码评审 第40轮 修复计划（2026-07-10）— haiku 验证轮 2/3（被打破）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku），19 个零发现
- 原始 findings：3 条；主代理（fable）裁决：**确认 1 条，误报/不修 2 条**
- 验证轮第三次被打破；修复提交后 haiku 需重新归 0 再连续 3 轮验证

## 批次 1：ARM ABI 枚举一致性（1 条）

- [x] F3 `Enums/ARMCPUArch.cs:27` 枚举停在 v8-M.Mainline=17，而同一 ABI 值空间的显示表
  `s_aeabiCpuArch`（ELFAttributeInfo.cs:123）已按 binutils arm_attr_tag_CPU_arch 列到 v9（值 0-22），
  两份表述不一致且设计文档声明该枚举服务 .ARM.attributes 解析。
  修复：补 v8_1_A=18、v8_2_A=19、v8_3_A=20、v8_1_M_Mainline=21、v9=22。

## 误报/不修 2 条

| # | 位置 | 指控 | 裁决理由 |
|---|---|---|---|
| 1 | ResourceDirectoryReader.cs:323 | RunAtOffset 异常未恢复流位置 | 第 5 次重复报告，无新证据（第31轮起既定裁决：全库读取点自定位，无受害路径） |
| 2 | PEAnalyzer/IconImageFactory.cs:36 | BitmapImage 异常未 Dispose | WPF BitmapImage 不实现 IDisposable，指控不成立 |

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十四批(2026-07-10)
