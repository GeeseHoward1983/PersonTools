using PersonalTools;
using PersonalTools.Enums;

namespace PersonalTools.ConstString
{
    internal static partial class WindowsStandardErrno
    {
        // 英文 errno 错误描述
        private static readonly Dictionary<long, string> WindowsStandardErrnoMapEnglish = new()
        {
            { 0, "No error" },                                  // Success
            { 1, "Operation not permitted" },                   // EPERM: Operation not permitted
            { 2, "No such file or directory" },                // ENOENT: No such file or directory
            { 3, "No such process" },                          // ESRCH: No such process
            { 4, "Interrupted system call" },                  // EINTR: Interrupted system call
            { 5, "Input/output error" },                       // EIO: Input/output error
            { 6, "No such device or address" },                // ENXIO: No such device or address
            { 7, "Argument list too long" },                   // E2BIG: Argument list too long
            { 8, "Exec format error" },                        // ENOEXEC: Exec format error
            { 9, "Bad file descriptor" },                      // EBADF: Bad file descriptor
            { 10, "No child processes" },                      // ECHILD: No child processes
            { 11, "Resource temporarily unavailable" },        // EAGAIN: Resource temporarily unavailable
            { 12, "Cannot allocate memory" },                  // ENOMEM: Cannot allocate memory
            { 13, "Permission denied" },                       // EACCES: Permission denied
            { 14, "Bad address" },                             // EFAULT: Bad address
            { 16, "Device or resource busy" },                 // EBUSY: Device or resource busy
            { 17, "File exists" },                             // EEXIST: File exists
            { 18, "Invalid cross-device link" },               // EXDEV: Invalid cross-device link
            { 19, "No such device" },                          // ENODEV: No such device
            { 20, "Not a directory" },                         // ENOTDIR: Not a directory
            { 21, "Is a directory" },                          // EISDIR: Is a directory
            { 22, "Invalid argument" },                        // EINVAL: Invalid argument
            { 23, "Too many open files in system" },           // ENFILE: Too many open files in system
            { 24, "Too many open files" },                     // EMFILE: Too many open files
            { 25, "Inappropriate ioctl for device" },          // ENOTTY: Inappropriate ioctl for device
            { 27, "File too large" },                          // EFBIG: File too large
            { 28, "No space left on device" },                 // ENOSPC: No space left on device
            { 29, "Illegal seek" },                            // ESPIPE: Illegal seek
            { 30, "Read-only file system" },                   // EROFS: Read-only file system
            { 31, "Too many links" },                          // EMLINK: Too many links
            { 32, "Broken pipe" },                             // EPIPE: Broken pipe
            { 33, "Numerical argument out of domain" },        // EDOM: Numerical argument out of domain
            { 34, "Numerical result out of range" },           // ERANGE: Numerical result out of range
            { 36, "Resource deadlock avoided" },                // EDEADLK: Resource deadlock avoided
            { 38, "File name too long" },                      // ENAMETOOLONG: File name too long
            { 39, "No locks available" },                      // ENOLCK: No locks available
            { 40, "Function not implemented" },                // ENOSYS: Function not implemented
            { 41, "Directory not empty" },                     // ENOTEMPTY: Directory not empty
            { 42, "Invalid or incomplete multibyte or wide character" } // EILSEQ: Invalid or incomplete multibyte or wide character
        };

        // 简体中文 errno 错误描述
        private static readonly Dictionary<long, string> WindowsStandardErrnoMapSimplifiedChinese = new()
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

        // 繁体中文 errno 错误描述
        private static readonly Dictionary<long, string> WindowsStandardErrnoMapTraditionalChinese = new()
        {
            { 0, "無錯誤" },                          // 成功
            { 1, "操作不允許" },                     // EPERM: 操作不允許
            { 2, "沒有此類文件或目錄" },             // ENOENT: 沒有此類文件或目錄
            { 3, "沒有此進程" },                     // ESRCH: 沒有此進程
            { 4, "系統調用被中斷" },                 // EINTR: 系統調用被中斷
            { 5, "輸入/輸出錯誤" },                 // EIO: 輸入/輸出錯誤
            { 6, "沒有此設備或地址" },               // ENXIO: 沒有此設備或地址
            { 7, "參數列表過長" },                   // E2BIG: 參數列表過長
            { 8, "執行格式錯誤" },                   // ENOEXEC: 執行格式錯誤
            { 9, "錯誤的文件描述符" },               // EBADF: 錯誤的文件描述符
            { 10, "沒有子進程" },                   // ECHILD: 沒有子進程
            { 11, "資源暫時不可用" },               // EAGAIN: 資源暫時不可用
            { 12, "內存不足" },                     // ENOMEM: 無法分配內存
            { 13, "權限被拒絕" },                   // EACCES: 權限被拒絕
            { 14, "錯誤地址" },                     // EFAULT: 錯誤地址
            { 16, "設備或資源忙" },                 // EBUSY: 設備或資源忙
            { 17, "文件已存在" },                   // EEXIST: 文件已存在
            { 18, "無效的跨設備鏈接" },             // EXDEV: 無效的跨設備鏈接
            { 19, "沒有此設備" },                   // ENODEV: 沒有此設備
            { 20, "不是目錄" },                     // ENOTDIR: 不是目錄
            { 21, "是目錄" },                       // EISDIR: 是目錄
            { 22, "無效參數" },                     // EINVAL: 無效參數
            { 23, "系統中打開的文件過多" },         // ENFILE: 系統中打開的文件過多
            { 24, "打開的文件過多" },               // EMFILE: 打開的文件過多
            { 25, "對設備不適當的ioctl" },          // ENOTTY: 對設備不適當的ioctl
            { 27, "文件過大" },                     // EFBIG: 文件過大
            { 28, "設備上沒有剩余空間" },           // ENOSPC: 設備上沒有剩余空間
            { 29, "非法seek" },                     // ESPIPE: 非法seek
            { 30, "只讀文件系統" },                 // EROFS: 只讀文件系統
            { 31, "鏈接過多" },                     // EMLINK: 鏈接過多
            { 32, "破損的管道" },                   // EPIPE: 破損的管道
            { 33, "數值參數超出域" },               // EDOM: 數值參數超出域
            { 34, "數值結果超出範圍" },             // ERANGE: 數值結果超出範圍
            { 36, "避免資源死鎖" },                 // EDEADLK: 避免資源死鎖
            { 38, "文件名過長" },                   // ENAMETOOLONG: 文件名過長
            { 39, "沒有可用鎖" },                   // ENOLCK: 沒有可用鎖
            { 40, "函數未實現" },                   // ENOSYS: 函數未實現
            { 41, "目錄非空" },                     // ENOTEMPTY: 目錄非空
            { 42, "無效或多字節字符不完整" }        // EILSEQ: 無效或多字節字符不完整
        };

        // Windows标准C库 errno 错误码 (Windows平台支持的errno值)
        public static Dictionary<long, string> WindowsStandardErrnoMap => GlobalState.CurrentLanguageType switch
        {
            LanguageType.SimplifiedChinese => WindowsStandardErrnoMapSimplifiedChinese,
            LanguageType.TraditionalChinese => WindowsStandardErrnoMapTraditionalChinese,
            _ => WindowsStandardErrnoMapEnglish
        };
    }
}