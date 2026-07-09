# 全量代码评审修复清单（2026-07-09，第17轮，分批次执行）

来源：superpowers 多视角评审，25 个并行评审代理（25/25 成功、0 失败，20 个代理零发现）。
findings 共 **7 条**（Important 1 / Minor 6；确认 5 / 疑似 2）。收紧提示后主观用词偏好类已归零。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — PE 资源语言表（1）

- [x] B1-1 (Important/确认) `PEAnalyzer/Resources/PEResourceParser.Version.Language.cs` — 写脚本按 [MS-LCID] 全表交叉核对 232 个映射，定位 30 处不符，其中 27 处为真错配（3 处 Kashmiri/Croatian/Trinidad 现值更精确非错，不动）。系统性错位：Arabic 二级(0x1401-0x2801，因缺 0x1001 Libya 整体慢一格)、French 二级(0x200C-0x3C0C)、English 二级(0x3C09-0x4809)、Spanish 二级(0x400A-0x540A) + 单点 0x0491(Yakut→Scottish Gaelic)/0x083B(芬兰→瑞典 se-SE)/0x0849(Tamil India→Sri Lanka)。按 [MS-LCID] 重建这 27 个 index 的三语并行名字（en/simp/trad 各 27 行，共 81 行）；脚本复核 mismatch 30→3(剩 3 为可接受措辞变体)。非标准高位(0x74xx+)超出 [MS-LCID] 可靠范围，不动。

## 批次2 — ELF（1）

- [x] B2-1 (Minor/疑似→核实属实) `ELFAnalyzer/Core/ELFExidxInfo.cs:514` — 0xB4 按 EHABI PACBTI 输出 "pop {ra_auth_code}"（与 readelf/LLVM 一致）；0xB5 并入 Spare 分支，去除杜撰的 "pop"/"pop vsp"。

## 批次3 — Enums（2）

- [x] B3-1 (Minor/确认) `Enums/CPUUnalignedAccess.cs:12` — 删除杜撰的 v7=2（Tag_CPU_unaligned_access 仅 0/1）。
- [x] B3-2 (Minor/疑似→核实属实) `Enums/ProgramHeaderType.cs:17` — 删除杜撰的 PT_EXTAB=0x70000002（AArch32 仅 ARCHEXT/EXIDX；0x70000002 在 AArch64 是 MEMTAG_MTE，extab 是节非段类型），无外部引用。

## 批次4 — ConstString（3）

- [x] B4-1 (Minor/确认) `MySqlErrors.SimplifiedChinese.cs:539/541` — 键 1342/1344 改为「解析注释'%-.200s'时文件意外结束」「跳过未知参数'%-.192s'时文件意外结束」（对齐 EN/TC，1342 占位符精度同步为 %-.200s）。
- [x] B4-2 (Minor/确认) `MySqlErrors.SimplifiedChinese.cs:586` — 键 1389 改「%-.64s中的魔数错误」（magic→魔数，占位符对齐 %-.64s）。
- [x] B4-3 (Minor/确认) `ConstString/SqlServerErrors.cs:155/179` — 「對錶執行」「錶或索引碎片」改回「表」。经查证：该文件不在第16轮 tc_terms 脚本的 FILES 列表内，这两处「錶」系历史既存数据（非本轮回归）；全局 grep 确认仅此 2 处误用「錶」。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 提交「全量代码评审修复第十七批(2026-07-09)」（不 push）。

---

## 完成情况

**7/7 全部修复并通过编译**（2 条疑似核实属实，LCID 表按 [MS-LCID] 脚本化核对重建）：Important 1 + Minor 6。
findings 收敛：17 → 13 → 7 → 10 → 5 → 18 → 7。收紧提示后主观用词偏好类已归零，剩余全为确凿硬错误。
