using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // DOS头结构
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_DOS_HEADER
    {
        public ushort e_magic;       // 魔数 "MZ"
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] e_res1;
        public ushort e_oemid;
        public ushort e_oeminfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] e_res2;
        // NT头偏移。PE 规范类型为 LONG(有符号)，此处用 uint：该偏移恒非负，且下游 RvaToOffset/边界校验
        // 均对其做范围检查，故以无符号存储无实际风险（仅与规范类型不完全一致）。
        public uint e_lfanew;
    }
}