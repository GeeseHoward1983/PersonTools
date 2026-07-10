# 全量代码评审 第34轮 裁决记录（2026-07-10）— haiku 验证轮 2/3

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku），提示含累积误报清单
- 原始 findings：25 条；主代理（fable）逐条核实裁决：**确认 0 条，误报/不修 25 条**
- 连续第 3 轮归 0；验证轮 2/3 通过。无代码变更，无需编译与提交

## 误报/不修 25 条（裁决理由，按类归并）

| 类别 | 条目 | 裁决理由 |
|---|---|---|
| ulong/long 偏移加法回绕 | ParseDefinitions.cs:111、ParseDependencies.cs:115 | offset < FileData.Length ≤ 2^31，vd_next/auxNext ≤ 2^32，和 < 2^33 远小于 2^64/2^63，回绕不可达；循环条件随后重新校验边界 |
| Icon 尺寸 int 溢出 | Icon.Data.cs:194、Icon.Group.cs:157 | Data 路径溢出为负被 `is <= 0` 拦截（第31轮已裁决）；Group 路径上游 `6L+16+BytesInRes < 10MB` long 计算已拦截，imageData.Length < 10MB |
| PE 流位置泄漏 | PEParser.CLR.cs:63、CLR.Metadata.cs:191/210/223/245、CLR.Helpers.cs:126、ResourceDirectoryReader.cs:325 | 第31轮已裁决类别：全库读取点自定位，无环境位置依赖的受害读取；RunAtOffset 文档注释明示既定语义（本轮报告自己也承认"虽文档说明"） |
| WPF 对话框 using | FileTabHost:40、MarkdownToWord:171/203/279、Rsa:207 | Microsoft.Win32 对话框不实现 IDisposable，`using` 包装根本无法编译（第31轮已裁决） |
| WebView2 事件泄漏 | MarkdownToWordControl.xaml.cs:87 | OnPreviewNavigationStarting 为 static 方法，委托无实例引用；CoreWebView2 由宿主窗口 Closed 统一 Dispose（注释明示"切 Tab 不释放以便复用"的设计） |
| Click 处理器无重入令牌 | HashCompute:126、Aes:68/99、RsaGen:173、Crc:32 | 纯计算型点击：读取当下输入、完成时整体写回，同输入同结果、完成序覆盖为良性；历轮仅对会切换底层文件状态的 Drop/加载路径加令牌，属深思后的范围决策 |
| ConstString 访问修饰符 | WindowsSystemErrors.{English,SimplifiedChinese,TraditionalChinese}.cs:6 | 风格/一致性类（评审标准明确排除）；构建含分析器 0 警告 |
