using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
                DisplayAdditionalInfo(); // 添加显示附加信息的调用
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
                { "架构", PEParser.GetMachineTypeDescription(currentPEInfo.NtHeaders.FileHeader.Machine) },
                { "位数", PEParser.Is64Bit(currentPEInfo.OptionalHeader) ? "64位" : "32位" }
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
                { "编译器版本", $"{PEParser.GetCompilerVersionDescription(currentPEInfo.OptionalHeader.MajorLinkerVersion, currentPEInfo.OptionalHeader.MinorLinkerVersion)}" },
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

        private void ShowAllSectionsDialog()
        {
            if (currentPEInfo == null) return;

            var dialog = new Window
            {
                Title = "所有节详细信息",
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                ItemsSource = currentPEInfo.SectionHeaders.Select((section, index) =>
                {
                    string sectionName = System.Text.Encoding.UTF8.GetString(section.Name).Trim('\0');
                    return new
                    {
                        Index = index,
                        Name = sectionName,
                        VirtualAddress = $"0x{section.VirtualAddress:X8}",
                        VirtualSize = $"0x{section.VirtualSize:X8}",
                        RawSize = $"0x{section.SizeOfRawData:X8}",
                        Characteristics = $"0x{section.Characteristics:X8}"
                    };
                }).ToList()
            };

            grid.Columns.Add(new DataGridTextColumn { Header = "索引", Binding = new System.Windows.Data.Binding("Index"), Width = 50 });
            grid.Columns.Add(new DataGridTextColumn { Header = "节名称", Binding = new System.Windows.Data.Binding("Name"), Width = 100 });
            grid.Columns.Add(new DataGridTextColumn { Header = "虚拟地址", Binding = new System.Windows.Data.Binding("VirtualAddress"), Width = 100 });
            grid.Columns.Add(new DataGridTextColumn { Header = "虚拟大小", Binding = new System.Windows.Data.Binding("VirtualSize"), Width = 100 });
            grid.Columns.Add(new DataGridTextColumn { Header = "原始大小", Binding = new System.Windows.Data.Binding("RawSize"), Width = 100 });
            grid.Columns.Add(new DataGridTextColumn { Header = "特征", Binding = new System.Windows.Data.Binding("Characteristics"), Width = 100 });

            dialog.Content = grid;
            dialog.Show();
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

            return;
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
            ExportFunctionsGrid.ItemsSource = currentPEInfo?.ExportFunctions;
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
    }
}