# 全量代码评审 第31轮 修复计划（2026-07-10）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku，模块组×视角），9 个零发现
- 原始 findings：44 条；主代理（fable）逐条核实裁决：**确认 2 条，误报/不修 42 条**（模型冲突以高级别模型为准）
- 基线：`dotnet build -c Debug` 0 Warning / 0 Error（提交 fb105de 后验证）

## 批次 1：ELF 解析健壮性（1 条）

- [x] F7 `ELFAnalyzer/Core/VersionSymbolParser.ParseDefinitions.cs:54` `CalculateVerDefEntryCount`
  对不可信 `sh_size / sh_entsize` 的商直接 `(int)` 截断：商 > int.MaxValue 时截成负值/小值，
  解析路径 `ParseVerDefEntries` 静默跳过整节、格式化路径 past-end 启发式失效。
  修复：夹紧到 `[0, int.MaxValue]`，与本文件 verdefNum / verneedNum 的既定夹紧约定一致。
  （两个调用点均安全：解析路径另有 MaxVerDefEntries=1M 与节边界兜底；VersionSymbolFormatter:121 仅作比较阈值不用于分配。）

## 批次 2：readelf 显示对齐（1 条）

- [x] F43 `Enums/EMachine.cs:10` 缺 `EM_486 = 6`（gABI 保留值，readelf 显示 "Intel 80486"）。
  修复：补枚举值，并在 `ELFAnalyzer/Core/ELFHeaderDescriptions.cs` 机器名字典补 `"Intel 80486"`。

## 误报/不修 42 条（裁决理由）

| 类别 | 条目 | 裁决理由 |
|---|---|---|
| ELF/PE (int) 截断系列 | F1-F6, F10-F14 | 所有读取点前置边界检查（offset+N ≤ sectionEnd/FileData.Length ≤ int.MaxValue）；`byte[]` 无法承载 >2GB 文件；PEHeaderParser 已有 Math.Min/Max 夹紧；PEParserUtils:105 / PEExportParser:107 已是 long 运算；Icon fullIconDataSize 溢出为负会被 `<= 0` 拦截 |
| InBounds 回绕 | F8 | C# int 减法无回绕语义，`data.Length - size` 为负时比较结果正确 |
| ICO 宽高截断 | F9 | ICO 目录字节本无法表达 >256；256 恰好正确编码为 0；解码器以 DIB 头为准 |
| PE 流位置未恢复 | F15-F20 | 全库读取点自定位（TryReadEntry/WalkEntries/ReadDataEntry 前均显式 fs.Position=...），无受害读取路径；RunAtOffset 的异常不恢复语义有文档注释明示（对齐历史行为） |
| FileInfo/DirectoryInfo 未 Dispose | F21-F22 | `FileInfo`/`DirectoryInfo` 不实现 IDisposable，指控不成立 |
| UC-ELF SetXxx 缺 null 防护 | F23-F26 | WPF TextBox.Text/TextBlock.Text 对 null 有框架 coerce（显示为空），调用方不传 null，无缺陷 |
| RSA 模式判断反向 | F27-F28 | `RsaCryptoService.Encrypt(..., bool isString)` 第三参就是 isString（内部 `InputBytes(input, !isString)`），传 `RsaInputStringRadio.IsChecked == true` 正确 |
| WPF 对话框未 Dispose | F29-F33 | 三个文件均 `using Microsoft.Win32`（WPF 对话框），不实现 IDisposable |
| Hash RadioButton 绑定 | F34 | 同面板单选自动互斥 + Hex 单选 TwoWay 回写 false，XAML 注释已明示该既定模式 |
| IsHexMode 无 INPC | F35 | 该属性无代码侧写入（仅 UI→源单向变更 + 代码读取），无需变更通知 |
| close.Click 事件泄漏 | F36 | 事件引用方向为 publisher(Button)→subscriber(控件)；被移除的是 publisher，无泄漏 |
| async void 非事件方法 | F37-F42 | 六处均为「大小预检 + 重入 token + 内部完整 try/catch + ConfigureAwait(true) + 卸载检查」的既定加固模式，与 async Task+await 行为等价 |
| DT_ENCODING 缺失 | F44 | DT_ENCODING 标准值为 32（非 31），且枚举注释已明示不收录边界值以保证 GetName(32) 确定性解析为 DT_PREINIT_ARRAY（readelf 对齐） |

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十一批(2026-07-10)
