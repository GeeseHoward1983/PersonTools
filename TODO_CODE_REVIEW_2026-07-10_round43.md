# 全量代码评审 第43轮 修复计划（2026-07-10）— haiku 验证轮 2/3（被打破）

- 评审方式：superpowers 多视角只读评审，21 个子代理（模型 haiku），13 个零发现
- 原始 findings：17 条；主代理（fable）裁决：**确认 1 条，误报/不修 16 条**
- 验证轮第四次被打破；修复提交后 haiku 需重新归 0 再连续 3 轮验证

## 批次 1：Markdown 列表导出正确性（1 条）

- [x] F7 `MarkdownToWord/Docx/DocxBlockRenderer.cs:312` 列表项首块非段落时项目标记丢失/错位。
  合法 CommonMark 中列表项可直接以代码块/引用/嵌套列表开头（如 `1.` 后直接跟代码围栏），
  此时 marker 仅在遇到 ParagraphBlock 时输出：项内无段落 → 标记整项丢失；
  首个段落在后 → 标记错挂其上（与子块实际顺序颠倒）。
  修复：首块非段落时先输出仅含标记的缩进段落并置 first=false。

## 误报/不修 16 条（按类归并）

| 类别 | 条目 | 裁决理由 |
|---|---|---|
| ELF64 r_type 低 8 位 | RelocationHelper:186 | 第 2 次重复：ELF64_R_TYPE=低 32 位（低 8 位是 ELF32 规则） |
| 直接数组访问无边界 | RelocationHelper:113/125 | symCount=Length/entrySize 保证 b+entrySize-1<Length，恒有界；报告自述"增强防御性" |
| PE 流位置 | PEParser.CLR.cs:86、Version.Helpers.cs:117/170 | 第 7 次重复，无新证据 |
| SetXxx null 防护 | ELFHeader/SectionToSegment/VersionSymbol/VersionDependency/Attribute/Exidx 六控件 | 第 2 次重复：WPF Text 对 null 框架 coerce，调用方不传 null |
| WebView2 退订 | MarkdownToWordControl:87 ×2 | 第 2/3 次重复：static 处理器无实例引用，Dispose 统一释放 |
| App 全局事件 OnExit 未退订 | App.xaml.cs:19 | 进程生命周期对象，退出时解除订阅无意义（进程即将结束） |
| TILE-Gx 缺 90-91 | Enums/TILEGxRelocationType.cs:100 | glibc/binutils 明示 "Relocs 90-91 are currently not defined"（保留空洞）；LAST_TLS_IE 实为 94-97，指控的值分配错误 |

## 验证与提交

- [x] `dotnet build -c Debug` → 0 Warning / 0 Error
- [x] git commit：全量代码评审修复第三十五批(2026-07-10)
