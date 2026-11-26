# ELF 界面数据分层与指标验收

## 职责与依赖方向

```text
UI / ELFAnalyzer -> UIHelper（兼容调用入口）
                      |                 |
                      v                 v
                Presentation --------> Core
                      |                 |
                      +------> Models <-+
```

- `UIHelper` 保留原类名、方法签名及返回类型，只作为界面调用适配层。
- `Presentation` 将已解析的数据转换成原有的 `*Info` 行模型和文本，统一管理十六进制位宽、列顺序、区域性、符号标签和换行。
- 新增 `Core` 服务负责文件读取、节定位、REL/RELA 解码、MIPS64 的 `r_info` 布局、符号缓存、GOT 关联和段映射预算，不构造 UI 行模型，也不依赖 `Presentation` 或 `UIHelper`。
- 新增 `*Data`/`*Reference` 模型保持地址、类型、符号索引、加数和槽值等原始类型。特别是 GOT 直接按数值地址索引重定位，避免显示字符串的反解析。
- 既有 Core 功能保持兼容；本次架构和指标检查覆盖新增的 Core 服务，而不是扩大到整个历史代码库。

## 默认验收门槛

| 范围 | 类整体 | 方法 |
|---|---:|---:|
| `UIHelper` | ≥90 | 对外方法 ≥90；私有方法 ≥80 |
| 新增 `Core` / `Presentation` / 原始数据模型 | ≥80 | ≥80 |

“对外方法”包括 public、internal 和 protected 系列访问级别。指标由项目 NetAnalyzers 的 CodeMetrics API 根据实际源文件和编译引用计算；报告列出 UIHelper 和支撑层，而不是只统计薄适配层。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-ELFUIHelper.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Test-ELFUIHelper.ps1 -Configuration Debug
```

`-MinimumClassMaintainability` 和 `-MinimumPublicMaintainability` 默认为 90；`-MinimumMaintainability`（UIHelper 私有实现）和 `-MinimumSupportingMaintainability` 默认为 80。运行结束会输出临时目录中的 JSON 指标报告路径。

## 回归覆盖

- 原有 2048 个 ELF 头格式化用例。
- 重构前固定的 273 组 UI 快照，在四种区域性下逐字段比对；不以当前生产代码自行生成期望值。
- 原始数据契约：32/64 位、大小端、MIPS 与 MIPS_RS3_LE、REL/RELA、GOT 覆盖顺序和保留槽、动态字符串及地址边界。
- 语义依赖检查：新增 Core 不依赖 UIHelper/Presentation 或 `Models.*Info`，Presentation 不反向依赖 UIHelper。
- 既有防御边界继续验证：截断节、非法偏移、零/伪造 `sh_entsize`、GOT 可读数量夹紧和段映射预算。
