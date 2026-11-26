# 验证 UIHelper 全目录的方法和类级指标，以及 ELF 头和其它界面数据的输出回归。
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [ValidateRange(0, 100)]
    [int]$MinimumMaintainability = 80,
    [ValidateRange(0, 100)]
    [int]$MinimumClassMaintainability = 90,
    [ValidateRange(0, 100)]
    [int]$MinimumPublicMaintainability = 90,
    [ValidateRange(0, 100)]
    [int]$MinimumSupportingMaintainability = 80
)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Test-ELFHeaderHelper.ps1') @PSBoundParameters -IncludeUIHelper
