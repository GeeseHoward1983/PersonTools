# 全量代码评审修复进度（2026-07-07）

来源：superpowers 多视角评审，7 分片并行，共 24 条 finding（Critical 0 / Important 2 / Minor 22）。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复。

## Important（2）

- [x] I1. `PEAnalyzer/Parsers/PEImportParser.cs` — 导入函数总数无全局上限，放大型 DoS。加贯穿标准表+延迟加载表的 `MaxTotalImports` 运行时总计数器，超限即停。
- [x] I2. `PEAnalyzer/Resources/PEResourceParser.Version.cs` — 已解析出的正确版本被畸形兄弟条目的错误占位串覆盖。加"已成功"标志，仅未成功时才写占位串。

## Minor — PE 解析器（2）

- [x] M1. `PEAnalyzer/Parsers/PEImportParser.cs:302` — 延迟加载导入表缺"全零终止符"判断。读到全零描述符即 break。
- [x] M2. `PEAnalyzer/Parsers/PEParser.CLR.Metadata.cs:270` — TypeDef 行数无显式上限。加与 MaxExportEntries 同量级上限。

## Minor — PE 资源解析（3）

- [x] M3. `PEAnalyzer/Resources/PEResourceParser.Icon.cs:129-140` — 组图标"命中类型即算成功"漏 RT_ICON 回退。以实际新增图标数判断。
- [x] M4. `PEAnalyzer/Resources/PEResourceParser.Icon.Data.cs:24` + `Icon.Helpers.cs:72` — 图标数据 RvaToOffset 未传 requiredLength=Size。传 dataEntry.Size。
- [x] M5. `PEAnalyzer/Resources/PEResourceParser.Icon.Group.cs:35` — 未校验 Size>=6 就读 ICONDIR 头。加 Size>=6 校验。

## Minor — ELF 解析器（5）

- [x] M6. `ELFAnalyzer/Core/ELFNoteInfo.cs:111` — GetBuildID 硬性要求 20 字节丢弃 16 字节 build-id。放宽为 `< 4`。
- [x] M7. `ELFAnalyzer/UIHelper/RelocationHelper.cs:82` — 命中同名非 REL/RELA 节即 return -1 漏真实节。改为 continue。
- [x] M8. `ELFAnalyzer/Core/ELFExidxInfo.cs:58` — 判断 exidx 节存在前就全符号表排序。先探测节存在再排序。
- [x] M9. `ELFAnalyzer/UIHelper/ELFHeaderHelper.cs:25` — 版本行用 e_version 却配 EI_VERSION 标签。该行统一用 EI_VERSION。
- [x] M10. `ELFAnalyzer/Core/ELFNoteInfo.cs:122` — build-id 输出串括号不配对。补右括号。

## Minor — MarkdownToWord（4）

- [x] M11. `MarkdownToWord/Docx/DocxImageEmbedder.cs:91` — 单图兜底 catch 太窄，part 创建/FeedData 异常逃逸终止整篇导出。纳入 per-image 兜底。
- [x] M12. `MarkdownToWord/Docx/DocxBlockRenderer.cs:217-231` — 脚注编号仅在首个 ParagraphBlock 输出。渲染第一个内容块前无条件输出编号。
- [x] M13. `MarkdownToWord/Docx/DocxInlineRenderer.cs:159` — 超链接关系无 URL 去重；空文本链接生成空 hyperlink。缓存 URL→relId；无内容退回纯文本。
- [x] M14. `MarkdownToWord/Docx/DocxWriter.cs:52-69` — 正文空时 Body 仅含 sectPr。兜底加一个空段落。

## Minor — Utils（3）

- [x] M15. `Utils/MessageHelper.cs:11,17,23` — 弹窗默认标题硬编码中文忽略语言。按 GlobalState.CurrentLanguageType 本地化。
- [x] M16. `Utils/Crypto/RsaCryptoService.cs:95-102` — GenerateKeyPair keySize 无上限卡 UI。加上限（<=16384）。
- [x] M17. `Utils/Hash/CrcCalculator.cs:4-14,22` — CrcAlgorithm 属性 get;set; 可就地改共享单例。改 init;。

## Minor — UserControls（4）

- [x] M18. `UserControls/MarkdownToWordControl.xaml.cs:62,71` — WebView2 Preview 未 Dispose。卸载/退出时释放。
- [x] M19. `UserControls/AesEncryptionControl.xaml.cs:247`（+ Rsa:283/Crc:118/Base64:154/Hash:217）— ProcessFileFor* async void catch 太窄，OOM/SecurityException 冒泡。补就地兜底。
- [x] M20. `UserControls/MarkdownToWordControl.xaml.cs:133` — RefreshPreview 由 DispatcherTimer 触发 catch 太窄。加更宽兜底。
- [x] M21. `UserControls/DependencyNode.cs:161-182` — TryParsePE catch 比主加载器窄。与主加载器 catch 对齐。

## Minor — App 启动（1）

- [x] M22. `App.xaml.cs:19-20` — 未订阅 TaskScheduler.UnobservedTaskException。订阅并记日志 + SetObserved()。

---

## 收尾

- [x] 编译验证 `dotnet build`（EXIT=0，**0 Warning / 0 Error**，含 NetAnalyzers latest-all + EnforceCodeStyleInBuild）。

---

## 完成情况

**24/24 全部修复并通过编译**：Important 2（I1 导入放大 DoS、I2 版本覆盖）+ Minor 22。
`dotnet build -c Debug` → Build succeeded，0 Warning(s) 0 Error(s)。
