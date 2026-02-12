using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 延迟加载导入描述符
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEDELAYLOADDESCRIPTOR
    {
        public uint Attributes;          // 可能包含标志位
        public uint DllNameRVA;         // 指向DLL名称的RVA
        public uint ModuleHandleRVA;    // 指向模块句柄的RVA
        public uint ImportAddressTableRVA;  // 导入地址表的RVA
        public uint ImportNameTableRVA;     // 导入名称表的RVA
        public uint BoundImportAddressTableRVA; // 边界导入地址表的RVA
        public uint UnloadInformationTableRVA;  // 卸载信息表的RVA
        public uint TimeDateStamp;              // 时间戳
    }
}