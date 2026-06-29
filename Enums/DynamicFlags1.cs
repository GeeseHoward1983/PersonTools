namespace PersonalTools.Enums
{
    // DT_FLAGS_1 (0x6ffffffb) 专用位命名空间，与 DT_FLAGS 的 DF_* 完全不同；现代 PIE/动态库几乎都带此项
    [Flags]
    internal enum DynamicFlags1 : uint
    {
        DF_1_NOW = 0x1,                 // 立即执行重定位（等价 RTLD_NOW）
        DF_1_GLOBAL = 0x2,             // 符号对全局可见（RTLD_GLOBAL）
        DF_1_GROUP = 0x4,             // 对象是依赖组成员
        DF_1_NODELETE = 0x8,         // 加载后不可卸载
        DF_1_LOADFLTR = 0x10,        // 立即加载过滤器（filtee）
        DF_1_INITFIRST = 0x20,       // 对象的初始化先于其他对象
        DF_1_NOOPEN = 0x40,          // 禁止 dlopen 打开此对象
        DF_1_ORIGIN = 0x80,          // 使用 $ORIGIN 处理
        DF_1_DIRECT = 0x100,         // 直接绑定
        DF_1_TRANS = 0x200,          // 透明（保留）
        DF_1_INTERPOSE = 0x400,      // 对象是插入项（interposer）
        DF_1_NODEFLIB = 0x800,       // 搜索时忽略默认库路径
        DF_1_NODUMP = 0x1000,        // 不可被 dldump 转储
        DF_1_CONFALT = 0x2000,       // 生成的备用配置项
        DF_1_ENDFILTEE = 0x4000,     // 过滤器在此 filtee 终止
        DF_1_DISPRELDNE = 0x8000,    // 位移重定位已处理完毕
        DF_1_DISPRELPND = 0x10000,   // 位移重定位待处理
        DF_1_NODIRECT = 0x20000,     // 含非直接绑定符号
        DF_1_IGNMULDEF = 0x40000,    // 忽略多重定义（内部）
        DF_1_NOKSYMS = 0x80000,      // 无内核符号（内部）
        DF_1_NOHDR = 0x100000,       // 首页不含 ELF 头（内部）
        DF_1_EDITED = 0x200000,      // 对象在生成后被修改
        DF_1_NORELOC = 0x400000,     // 内部使用
        DF_1_SYMINTPOSE = 0x800000,  // 含符号级插入项
        DF_1_GLOBAUDIT = 0x1000000,  // 全局审计
        DF_1_SINGLETON = 0x2000000,  // 单例符号
        DF_1_STUB = 0x4000000,       // 桩对象（stub）
        DF_1_PIE = 0x8000000,        // 位置无关可执行文件（PIE）
        DF_1_KMOD = 0x10000000,      // 内核模块
        DF_1_WEAKFILTER = 0x20000000,// 弱过滤器
        DF_1_NOCOMMON = 0x40000000   // 不使用 common 符号
    }
}
