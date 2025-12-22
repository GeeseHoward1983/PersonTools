using System.Collections.Generic;

namespace MyTool
{
    public static partial class ConstString
    {
        // Windows标准C库 errno 错误码 (Windows平台支持的errno值)
        public static readonly Dictionary<int, string> WindowsStandardErrnoMap = new Dictionary<int, string>
        {
            { 0, "无错误" },                          // 成功
            { 1, "操作不允许" },                     // EPERM: 操作不允许
            { 2, "没有此类文件或目录" },             // ENOENT: 没有此类文件或目录
            { 3, "没有此进程" },                     // ESRCH: 没有此进程
            { 4, "系统调用被中断" },                 // EINTR: 系统调用被中断
            { 5, "输入/输出错误" },                 // EIO: 输入/输出错误
            { 6, "没有此设备或地址" },               // ENXIO: 没有此设备或地址
            { 7, "参数列表过长" },                   // E2BIG: 参数列表过长
            { 8, "执行格式错误" },                   // ENOEXEC: 执行格式错误
            { 9, "错误的文件描述符" },               // EBADF: 错误的文件描述符
            { 10, "没有子进程" },                   // ECHILD: 没有子进程
            { 11, "资源暂时不可用" },               // EAGAIN: 资源暂时不可用
            { 12, "内存不足" },                     // ENOMEM: 无法分配内存
            { 13, "权限被拒绝" },                   // EACCES: 权限被拒绝
            { 14, "错误地址" },                     // EFAULT: 错误地址
            { 16, "设备或资源忙" },                 // EBUSY: 设备或资源忙
            { 17, "文件已存在" },                   // EEXIST: 文件已存在
            { 18, "无效的跨设备链接" },             // EXDEV: 无效的跨设备链接
            { 19, "没有此设备" },                   // ENODEV: 没有此设备
            { 20, "不是目录" },                     // ENOTDIR: 不是目录
            { 21, "是目录" },                       // EISDIR: 是目录
            { 22, "无效参数" },                     // EINVAL: 无效参数
            { 23, "系统中打开的文件过多" },         // ENFILE: 系统中打开的文件过多
            { 24, "打开的文件过多" },               // EMFILE: 打开的文件过多
            { 25, "对设备不适当的ioctl" },          // ENOTTY: 对设备不适当的ioctl
            { 27, "文件过大" },                     // EFBIG: 文件过大
            { 28, "设备上没有剩余空间" },           // ENOSPC: 设备上没有剩余空间
            { 29, "非法seek" },                     // ESPIPE: 非法seek
            { 30, "只读文件系统" },                 // EROFS: 只读文件系统
            { 31, "链接过多" },                     // EMLINK: 链接过多
            { 32, "破损的管道" },                   // EPIPE: 破损的管道
            { 33, "数值参数超出域" },               // EDOM: 数值参数超出域
            { 34, "数值结果超出范围" },             // ERANGE: 数值结果超出范围
            { 36, "避免资源死锁" },                 // EDEADLK: 避免资源死锁
            { 38, "文件名过长" },                   // ENAMETOOLONG: 文件名过长
            { 39, "没有可用锁" },                   // ENOLCK: 没有可用锁
            { 40, "函数未实现" },                   // ENOSYS: 函数未实现
            { 41, "目录非空" },                     // ENOTEMPTY: 目录非空
            { 42, "无效或多字节字符不完整" }        // EILSEQ: 无效或多字节字符不完整
        };
    }
}