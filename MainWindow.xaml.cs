using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MyTool
{
    public partial class MainWindow : Window
    {
        private PEInfo? currentPEInfo = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Windows errno 查询按钮点击事件
        private void WindowsErrnoQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(WindowsErrnoInput.Text, ConstString.WindowsErrnoMap, WindowsErrnoResult);
        }

        // Linux errno 查询按钮点击事件
        private void LinuxErrnoQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(LinuxErrnoInput.Text, ConstString.LinuxErrnoMap, LinuxErrnoResult);
        }

        // Mac errno 查询按钮点击事件
        private void MacErrnoQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(MacErrnoInput.Text, ConstString.MacErrnoMap, MacErrnoResult);
        }

        // HTTP 状态码查询按钮点击事件
        private void HttpStatusCodeQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(HttpStatusCodeInput.Text, ConstString.HttpStatusMap, HttpStatusCodeResult);
        }

        // SQL Server 错误码查询按钮点击事件
        private void SqlServerErrorQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(SqlServerErrorInput.Text, ConstString.SqlServerErrorsMap, SqlServerErrorResult);
        }

        // MySQL 错误码查询按钮点击事件
        private void MySqlErrorQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(MySqlErrorInput.Text, ConstString.MySqlErrorsMap, MySqlErrorResult);
        }

        // Oracle SQLCODE 查询按钮点击事件
        private void OracleSqlCodeQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(OracleSqlCodeInput.Text, ConstString.OracleSqlCodeMap, OracleSqlCodeResult);
        }

        // ODBC 错误码查询按钮点击事件
        private void OdbcErrorQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(OdbcErrorInput.Text, ConstString.OdbcErrorsMap, OdbcErrorResult);
        }

        // Windows 系统错误码查询按钮点击事件
        private void WindowsSystemErrorQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(WindowsSystemErrorInput.Text, ConstString.WindowsSystemErrorsMap, WindowsSystemErrorResult);
        }

        // 通用错误码查询方法
        private void QueryErrorCode(string input, Dictionary<int, string> errorCodeMap, TextBlock resultTextBlock)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                resultTextBlock.Text = "请输入错误码";
                return;
            }

            if (int.TryParse(input.Trim(), out int errorCode))
            {
                string? errorMessage;
                if (errorCodeMap.TryGetValue(errorCode, out errorMessage))
                {
                    resultTextBlock.Text = $"错误码: {errorCode}\n错误信息: {errorMessage}";
                }
                else
                {
                    resultTextBlock.Text = $"未找到错误码 {errorCode} 的相关信息";
                }
            }
            else
                {
                resultTextBlock.Text = "输入的不是有效的数字";
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PE Files (*.exe;*.dll;*.sys)|*.exe;*.dll;*.sys|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadPEFile(openFileDialog.FileName);
            }
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    LoadPEFile(files[0]);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LoadPEFile(string filePath)
        {
            try
            {
                currentPEInfo = PEParser.ParsePEFile(filePath);
                DisplayHeaderInfo();
                DisplayDependencies();
                DisplayImportExportFunctions();
                DisplayAdditionalInfo();
                DisplayIcons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisplayHeaderInfo()
        {
            HeaderInfoPanel.Children.Clear();

            if (currentPEInfo == null) return;

            // 准备文件信息
            var fileInfo = new Dictionary<string, string>
            {
                { "文件路径", currentPEInfo.FilePath },
                { "文件类型", PEParser.GetDetailedFileType(currentPEInfo.NtHeaders.FileHeader.Characteristics, currentPEInfo.OptionalHeader.Subsystem) },
                { "架构", GetArchitectureInfo() },
                { "位数", GetBitInfo() }
            };

            // 只有在是驱动程序时才添加驱动程序类型信息
            string driverType = PEParser.GetDriverType(currentPEInfo.NtHeaders.FileHeader.Characteristics, currentPEInfo.OptionalHeader.Subsystem, currentPEInfo.OptionalHeader.DllCharacteristics);
            if (!string.IsNullOrEmpty(driverType))
            {
                fileInfo.Add("驱动程序类型", driverType);
            }

            // 显示文件基本信息
            AddHeaderInfo("文件信息", fileInfo);

            // 显示DOS头信息
            AddHeaderInfo("DOS头信息", new Dictionary<string, string>
            {
                { "签名(e_magic)", $"0x{currentPEInfo.DosHeader.e_magic:X4} ('{(char)(currentPEInfo.DosHeader.e_magic & 0xFF)}{(char)(currentPEInfo.DosHeader.e_magic >> 8)}')" },
                { "NT头偏移(e_lfanew)", $"0x{currentPEInfo.DosHeader.e_lfanew:X8}" }
            });

            // 显示NT头信息
            AddHeaderInfo("NT头信息", new Dictionary<string, string>
            {
                { "签名", $"0x{currentPEInfo.NtHeaders.Signature:X8}" },
                { "机器类型", PEParser.GetMachineTypeDescription(currentPEInfo.NtHeaders.FileHeader.Machine) },
                { "节数量", $"0x{currentPEInfo.NtHeaders.FileHeader.NumberOfSections:X4}" },
                { "时间戳", $"{currentPEInfo.NtHeaders.FileHeader.TimeDateStamp:X8} ({UnixTimeStampToDateTime(currentPEInfo.NtHeaders.FileHeader.TimeDateStamp):yyyy-MM-dd HH:mm:ss})" },
                { "可选头大小", $"0x{currentPEInfo.NtHeaders.FileHeader.SizeOfOptionalHeader:X4}" },
                { "特征标志", $"0x{currentPEInfo.NtHeaders.FileHeader.Characteristics:X4}" }
            });

            // 显示可选头信息
            AddHeaderInfo("可选头信息", new Dictionary<string, string>
            {
                { "魔数", $"0x{currentPEInfo.OptionalHeader.Magic:X4} ({(currentPEInfo.OptionalHeader.Magic == 0x10b ? "PE32" : currentPEInfo.OptionalHeader.Magic == 0x20b ? "PE32+" : "Unknown")})" },
                { "链接器版本", $"{PEParser.GetLinkerVersionDescription(currentPEInfo.OptionalHeader.MajorLinkerVersion, currentPEInfo.OptionalHeader.MinorLinkerVersion)}" },
                { "编译器版本", $"{PEParser.GetCompilerVersionDescription(currentPEInfo.OptionalHeader.MajorLinkerVersion, currentPEInfo.OptionalHeader.MinorLinkerVersion, currentPEInfo.CLRInfo != null)}" },
                { "代码大小", $"0x{currentPEInfo.OptionalHeader.SizeOfCode:X8}" },
                { "已初始化数据大小", $"0x{currentPEInfo.OptionalHeader.SizeOfInitializedData:X8}" },
                { "未初始化数据大小", $"0x{currentPEInfo.OptionalHeader.SizeOfUninitializedData:X8}" },
                { "入口点RVA", $"0x{currentPEInfo.OptionalHeader.AddressOfEntryPoint:X8}" },
                { "代码基址", $"0x{currentPEInfo.OptionalHeader.BaseOfCode:X8}" },
                { "数据基址", PEParser.Is64Bit(currentPEInfo.OptionalHeader) ? "N/A (PE32+)" : $"0x{currentPEInfo.OptionalHeader.BaseOfData:X8}" },
                { "镜像基址", $"0x{currentPEInfo.OptionalHeader.ImageBase:X8}" },
                { "节对齐", $"0x{currentPEInfo.OptionalHeader.SectionAlignment:X8}" },
                { "文件对齐", $"0x{currentPEInfo.OptionalHeader.FileAlignment:X8}" },
                { "操作系统版本", PEParser.GetOperatingSystemVersionDescription(currentPEInfo.OptionalHeader.MajorOperatingSystemVersion, currentPEInfo.OptionalHeader.MinorOperatingSystemVersion) },
                { "镜像版本", PEParser.GetImageVersionDescription(currentPEInfo.OptionalHeader.MajorImageVersion, currentPEInfo.OptionalHeader.MinorImageVersion) },
                { "子系统版本", PEParser.GetSubsystemVersionDescription(currentPEInfo.OptionalHeader.MajorSubsystemVersion, currentPEInfo.OptionalHeader.MinorSubsystemVersion) },
                { "镜像大小", $"0x{currentPEInfo.OptionalHeader.SizeOfImage:X8}" },
                { "头部大小", $"0x{currentPEInfo.OptionalHeader.SizeOfHeaders:X8}" },
                { "校验和", $"0x{currentPEInfo.OptionalHeader.CheckSum:X8}" },
                { "子系统", PEParser.GetSubsystemDescription(currentPEInfo.OptionalHeader.Subsystem) },
                { "DLL特征", $"0x{currentPEInfo.OptionalHeader.DllCharacteristics:X4}" },
                { "栈保留大小", $"0x{currentPEInfo.OptionalHeader.SizeOfStackReserve:X8}" },
                { "栈提交大小", $"0x{currentPEInfo.OptionalHeader.SizeOfStackCommit:X8}" },
                { "堆保留大小", $"0x{currentPEInfo.OptionalHeader.SizeOfHeapReserve:X8}" },
                { "堆提交大小", $"0x{currentPEInfo.OptionalHeader.SizeOfHeapCommit:X8}" },
                { "数据目录数量", $"0x{currentPEInfo.OptionalHeader.NumberOfRvaAndSizes:X8}" }
            });

            // 显示CLR信息（如果是.NET程序集）
            if (currentPEInfo.CLRInfo != null)
            {
                AddHeaderInfo(".NET CLR信息", new Dictionary<string, string>
                {
                    { "运行时版本", $"v{currentPEInfo.CLRInfo.RuntimeVersion}" },
                    { "架构类型", currentPEInfo.CLRInfo.Architecture },
                    { "标志位", string.Join(", ", currentPEInfo.CLRInfo.FlagDescriptions) },
                    { "入口点", $"0x{currentPEInfo.CLRInfo.EntryPointTokenOrRva:X8}" },
                    { "包含元数据", currentPEInfo.CLRInfo.HasMetaData ? "是" : "否" },
                    { "包含资源", currentPEInfo.CLRInfo.HasResources ? "是" : "否" },
                    { "强名称签名", currentPEInfo.CLRInfo.HasStrongNameSignature ? "是" : "否" }
                });
            }

            // 显示节信息
            var sectionDict = CreateSectionInfo();
            AddHeaderInfo("节信息", sectionDict);
        }

        private Dictionary<string, string> CreateSectionInfo()
        {
            var sectionInfo = new Dictionary<string, string>();
            
            // 显示所有节信息，不再限制数量
            for (int i = 0; i < currentPEInfo?.SectionHeaders.Count; i++)
            {
                var section = currentPEInfo.SectionHeaders[i];
                string sectionName = System.Text.Encoding.UTF8.GetString(section.Name).Trim('\0');
                sectionInfo[$"节 {i} ({sectionName})"] = $"RVA: 0x{section.VirtualAddress:X8}, 大小: 0x{section.VirtualSize:X8}, Raw大小: 0x{section.SizeOfRawData:X8}, 特征: 0x{section.Characteristics:X8}";
            }
            
            sectionInfo[$"总计"] = $"{currentPEInfo?.SectionHeaders.Count} 个节";
            
            return sectionInfo;
        }

        private void DisplayAdditionalInfo()
        {
            AdditionalInfoPanel.Children.Clear();

            // 总是创建面板，即使没有附加信息
            if (currentPEInfo == null) return;

            // 显示版本信息
            AddAdditionalInfo("版本信息", new Dictionary<string, string>
            {
                { "文件版本", currentPEInfo.AdditionalInfo.FileVersion },
                { "产品版本", currentPEInfo.AdditionalInfo.ProductVersion }
            });

            // 显示公司和产品信息
            AddAdditionalInfo("公司和产品信息", new Dictionary<string, string>
            {
                { "公司名称", currentPEInfo.AdditionalInfo.CompanyName },
                { "产品名称", currentPEInfo.AdditionalInfo.ProductName },
                { "文件描述", currentPEInfo.AdditionalInfo.FileDescription }
            });

            // 显示版权和商标信息
            AddAdditionalInfo("版权和商标信息", new Dictionary<string, string>
            {
                { "版权信息", currentPEInfo.AdditionalInfo.LegalCopyright },
                { "商标信息", currentPEInfo.AdditionalInfo.LegalTrademarks },
                { "原始文件名", currentPEInfo.AdditionalInfo.OriginalFileName },
                { "内部名称", currentPEInfo.AdditionalInfo.InternalName }
            });

            // 显示证书信息
            AddAdditionalInfo("证书信息", new Dictionary<string, string>
            {
                { "是否签名", currentPEInfo.AdditionalInfo.IsSigned ? "是" : "否" },
                { "证书详情", currentPEInfo.AdditionalInfo.CertificateInfo }
            });
            
            // 显示翻译信息（来自VarFileInfo）
            if (!string.IsNullOrEmpty(currentPEInfo.AdditionalInfo.TranslationInfo))
            {
                AddAdditionalInfo("翻译信息", new Dictionary<string, string>
                {
                    { "语言和代码页", currentPEInfo.AdditionalInfo.TranslationInfo }
                });
            }
        }

        private void DisplayIcons()
        {
            var iconViewModels = new List<IconViewModel>();

            if (currentPEInfo?.Icons != null && currentPEInfo.Icons.Count > 0)
            {
                foreach (var icon in currentPEInfo.Icons)
                {
                    try
                    {
                        // 检查图标数据是否有效
                        if (icon.Data == null || icon.Data.Length == 0)
                            continue;

                        // 验证图标尺寸，避免添加无效图标
                        if (icon.Width <= 0 || icon.Height <= 0)
                            continue;

                        var iconViewModel = new IconViewModel
                        {
                            Width = icon.Width,
                            Height = icon.Height,
                            BitsPerPixel = icon.BitsPerPixel,
                            Size = icon.Size
                        };

                        // 从字节数组创建位图
                        using (var stream = new MemoryStream(icon.Data))
                        {
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = stream;
                                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 提高性能并确保图像加载完成
                                bitmap.EndInit();
                                bitmap.Freeze(); // 提高性能
                                iconViewModel.ImageSource = bitmap;
                            }
                            catch (Exception ex)
                            {
                                // 如果图像解码失败，记录日志但不中断其他图标显示
                                Console.WriteLine($"图标解码失败: {ex.Message}");
                                continue;
                            }
                        }

                        iconViewModels.Add(iconViewModel);
                    }
                    catch (Exception ex)
                    {
                        // 图标加载失败时跳过该图标
                        Console.WriteLine($"图标加载失败: {ex.Message}");
                    }
                }
            }

            IconsDataGrid.ItemsSource = iconViewModels;
        }

        private void AddHeaderInfo(string title, Dictionary<string, string> info)
        {
            var groupBox = new GroupBox { Header = title, Margin = new Thickness(0, 5, 0, 5) };
            var panel = new StackPanel { Margin = new Thickness(5) };

            foreach (var item in info)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{item.Key}: {item.Value}",
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
                panel.Children.Add(textBlock);
            }

            groupBox.Content = panel;
            HeaderInfoPanel.Children.Add(groupBox);
        }

        private void AddAdditionalInfo(string title, Dictionary<string, string> info)
        {
            var groupBox = new GroupBox { Header = title, Margin = new Thickness(0, 5, 0, 5) };
            var panel = new StackPanel { Margin = new Thickness(5) };

            int itemsAdded = 0;
            foreach (var item in info)
            {
                // 添加所有信息，即使是空的也显示出来，这样用户可以看到哪些信息不存在
                var textBlock = new TextBlock
                {
                    Text = $"{item.Key}: {item.Value ?? "无"}",
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
                panel.Children.Add(textBlock);
                itemsAdded++;
            }

            // 如果没有任何信息，添加提示文本
            if (itemsAdded == 0)
            {
                var textBlock = new TextBlock
                {
                    Text = "未找到相关信息",
                    Margin = new Thickness(0, 2, 0, 2),
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                panel.Children.Add(textBlock);
            }

            groupBox.Content = panel;
            AdditionalInfoPanel.Children.Add(groupBox);
        }

        private void DisplayDependencies()
        {
            DependencyTree.Items.Clear();

            if (currentPEInfo?.Dependencies == null) return;

            foreach (var dep in currentPEInfo.Dependencies)
            {
                var treeNode = new TreeViewItem { Header = dep.Name };
                // 这里可以添加递归依赖显示逻辑
                DependencyTree.Items.Add(treeNode);
            }
        }

        private void DisplayImportExportFunctions()
        {
            // 显示导入函数
            ImportFunctionsGrid.ItemsSource = currentPEInfo?.ImportFunctions;

            // 显示导出函数
            // 对于.NET程序集，导出函数实际上可能是公开的类型
            var exportItems = new List<object>();
            
            if (currentPEInfo != null)
            {
                // 添加传统的导出函数
                exportItems.AddRange(currentPEInfo.ExportFunctions);
                
                // 如果是.NET程序集，添加额外的导出信息
                if (currentPEInfo.CLRInfo != null)
                {
                    // 可以在这里添加额外的.NET特定信息
                }
            }
            
            ExportFunctionsGrid.ItemsSource = exportItems;
        }

        private void DependencyTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DependencyTree.SelectedItem is TreeViewItem selectedItem)
            {
                // 双击依赖项时的处理逻辑
                MessageBox.Show($"双击了依赖项: {selectedItem.Header}", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dt.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        private string GetArchitectureInfo()
        {
            if (currentPEInfo == null) 
                return "Unknown";
                
            // 对于.NET程序，显示PE头架构和.NET架构信息
            if (currentPEInfo.CLRInfo != null)
            {
                return $"{PEParser.GetMachineTypeDescription(currentPEInfo.NtHeaders.FileHeader.Machine)} (.NET: {currentPEInfo.CLRInfo.Architecture})";
            }
            
            // 对于非.NET程序，只显示PE头架构
            return PEParser.GetMachineTypeDescription(currentPEInfo.NtHeaders.FileHeader.Machine);
        }

        private string GetBitInfo()
        {
            if (currentPEInfo == null) 
                return "Unknown";
                
            // 对于.NET程序，根据.NET架构判断位数
            if (currentPEInfo.CLRInfo != null)
            {
                string arch = currentPEInfo.CLRInfo.Architecture;
                if (arch == "x86")
                    return "32位";
                else if (arch == "Any CPU")
                    return "Any CPU";
                else if (arch == "x64" || arch == "ARM64")
                    return "64位";
                else
                    return "未知";
            }
            
            // 对于非.NET程序，根据PE头判断位数
            return PEParser.Is64Bit(currentPEInfo.OptionalHeader) ? "64位" : "32位";
        }
    }
}