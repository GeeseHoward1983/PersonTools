# 全量代码评审修复清单（2026-07-09，第19轮，分批次执行）

来源：superpowers 多视角评审，25 个并行评审代理（25/25 成功、0 失败，23 个代理零发现）。
findings 共 **3 条**（全 Minor；确认 2 / 疑似 1，均核实属实）。
状态图例：`[ ]` 待修 · `[~]` 进行中 · `[x]` 已修复 · `[!]` 核实为误报不修。

## 批次1 — PE LCID（1）

- [x] B1-1 (Minor/确认) `Version.Language.cs:1062` — 0x0800 是 LOCALE_SYSTEM_DEFAULT 保留值（主语言中立），被错误标为"中文(简体)"；zh-CN 是已正确映射的 0x0804。删除 { 0x0800, 110 }，使其回退"未知语言"（idx110 名字数组项无其他 LCID 引用、无害）。

## 批次2 — ConstString 英文 TPM（1）

- [x] B2-1 (Minor/确认) `WindowsSystemErrors.English.cs:4831` — 180 条 TPM 错误英文项前缀与消息无分隔符连写（"TPM 1.2Authentication failed."），而简繁两档均用全角"："分隔。脚本批量在 "TPM 1.2"/"TPM 2.0" 后补 ": "（半角冒号+空格，符合英文习惯）；脚本复核 fixed=180 / remaining=0。

## 批次3 — ConstString 简体（1）

- [x] B3-1 (Minor/疑似→核实属实) `MySqlErrors.SimplifiedChinese.cs:622` — 键 1425 ER_TOO_BIG_SCALE 把 scale 误译为"比例"，应为"小数位数/标度"（EN="Too big scale"，繁中已作"小數位數"）。改"比例"→"小数位数"；顺带将 max 占位符 %d 对齐 EN/繁中的 %lu。

## 收尾

- [x] F1. `dotnet build -c Debug` → EXIT=0，**0 Warning / 0 Error**。
- [x] F2. 提交「全量代码评审修复第十九批(2026-07-09)」（不 push）。

---

## 完成情况

**3/3 全部修复并通过编译**（1 条疑似核实属实，无误报）：全 Minor。
findings 收敛：17 → 13 → 7 → 10 → 5 → 18 → 7 → 5 → 3。持续向 0 逼近。
