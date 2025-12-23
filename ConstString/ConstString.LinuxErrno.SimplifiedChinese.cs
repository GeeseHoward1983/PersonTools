using System.Collections.Generic;

namespace MyTool
{
    public static partial class ConstString
    {
        // Linux errno 错误码 - 简体中文
        private static readonly Dictionary<long, string> LinuxErrnoMapSimplifiedChinese = new()
        {
            { 0, "成功" },
            { 1, "操作不被允许" }, /* EPERM */
            { 2, "没有这个文件或目录" }, /* ENOENT */
            { 3, "没有这个进程" }, /* ESRCH */
            { 4, "系统调用被中断" }, /* EINTR */
            { 5, "输入/输出错误" }, /* EIO */
            { 6, "没有这个设备或地址" }, /* ENXIO */
            { 7, "参数列表过长" }, /* E2BIG */
            { 8, "执行格式错误" }, /* ENOEXEC */
            { 9, "错误的文件描述符" }, /* EBADF */
            { 10, "没有子进程" }, /* ECHILD */
            { 11, "再试一次" }, /* EAGAIN or EWOULDBLOCK */
            { 12, "内存不足" }, /* ENOMEM */
            { 13, "权限被拒绝" }, /* EACCES */
            { 14, "错误地址" }, /* EFAULT */
            { 15, "需要块设备" }, /* ENOTBLK */
            { 16, "设备或资源忙" }, /* EBUSY */
            { 17, "文件已存在" }, /* EEXIST */
            { 18, "跨设备链接" }, /* EXDEV */
            { 19, "没有这个设备" }, /* ENODEV */
            { 20, "不是目录" }, /* ENOTDIR */
            { 21, "是一个目录" }, /* EISDIR */
            { 22, "无效参数" }, /* EINVAL */
            { 23, "文件表溢出" }, /* ENFILE */
            { 24, "打开文件过多" }, /* EMFILE */
            { 25, "不是打字机" }, /* ENOTTY */
            { 26, "文本文件忙" }, /* ETXTBSY */
            { 27, "文件太大" }, /* EFBIG */
            { 28, "设备上没有剩余空间" }, /* ENOSPC */
            { 29, "非法查找" }, /* ESPIPE */
            { 30, "只读文件系统" }, /* EROFS */
            { 31, "链接数过多" }, /* EMLINK */
            { 32, "管道已断开" }, /* EPIPE */
            { 33, "数学参数超出定义域" }, /* EDOM */
            { 34, "数学结果无法表示" }, /* ERANGE */
            { 35, "会发生资源死锁" }, /* EDEADLK */
            { 36, "文件名过长" }, /* ENAMETOOLONG */
            { 37, "没有可用的记录锁" }, /* ENOLCK */
            { 38, "无效的系统调用号" }, /* ENOSYS */
            { 39, "目录非空" }, /* ENOTEMPTY */
            { 40, "遇到过多符号链接" }, /* ELOOP */
            { 42, "没有所需类型的消息" }, /* ENOMSG */
            { 43, "标识符已移除" }, /* EIDRM */
            { 44, "通道号超出范围" }, /* ECHRNG */
            { 45, "第二级未同步" }, /* EL2NSYNC */
            { 46, "第三级已停止" }, /* EL3HLT */
            { 47, "第三级已重置" }, /* EL3RST */
            { 48, "链接号超出范围" }, /* ELNRNG */
            { 49, "协议驱动未连接" }, /* EUNATCH */
            { 50, "没有可用的CSI结构" }, /* ENOCSI */
            { 51, "第二级已停止" }, /* EL2HLT */
            { 52, "无效交换" }, /* EBADE */
            { 53, "无效请求描述符" }, /* EBADR */
            { 54, "交换区已满" }, /* EXFULL */
            { 55, "没有anode" }, /* ENOANO */
            { 56, "无效请求代码" }, /* EBADRQC */
            { 57, "无效插槽" }, /* EBADSLT */
            { 59, "错误的字体文件格式" }, /* EBFONT */
            { 60, "设备不是流" }, /* ENOSTR */
            { 61, "没有可用数据" }, /* ENODATA */
            { 62, "计时器已过期" }, /* ETIME */
            { 63, "流资源不足" }, /* ENOSR */
            { 64, "计算机不在网络上" }, /* ENONET */
            { 65, "软件包未安装" }, /* ENOPKG */
            { 66, "对象是远程的" }, /* EREMOTE */
            { 67, "链接已被切断" }, /* ENOLINK */
            { 68, "广告错误" }, /* EADV */
            { 69, "Srmount错误" }, /* ESRMNT */
            { 70, "发送时通信错误" }, /* ECOMM */
            { 71, "协议错误" }, /* EPROTO */
            { 72, "尝试多跳" }, /* EMULTIHOP */
            { 73, "RFS特定错误" }, /* EDOTDOT */
            { 74, "不是数据消息" }, /* EBADMSG */
            { 75, "值对于定义的数据类型太大" }, /* EOVERFLOW */
            { 76, "名称在网络上不唯一" }, /* ENOTUNIQ */
            { 77, "文件描述符状态错误" }, /* EBADFD */
            { 78, "远程地址已更改" }, /* EREMCHG */
            { 79, "无法访问所需的共享库" }, /* ELIBACC */
            { 80, "访问损坏的共享库" }, /* ELIBBAD */
            { 81, "a.out中的.lib节损坏" }, /* ELIBSCN */
            { 82, "尝试链接过多共享库" }, /* ELIBMAX */
            { 83, "无法直接执行共享库" }, /* ELIBEXEC */
            { 84, "非法字节序列" }, /* EILSEQ */
            { 85, "应重启被中断的系统调用" }, /* ERESTART */
            { 86, "流管道错误" }, /* ESTRPIPE */
            { 87, "用户过多" }, /* EUSERS */
            { 88, "对非套接字执行套接字操作" }, /* ENOTSOCK */
            { 89, "需要目标地址" }, /* EDESTADDRREQ */
            { 90, "消息太长" }, /* EMSGSIZE */
            { 91, "协议类型错误" }, /* EPROTOTYPE */
            { 92, "协议不可用" }, /* ENOPROTOOPT */
            { 93, "协议不支持" }, /* EPROTONOSUPPORT */
            { 94, "套接字类型不支持" }, /* ESOCKTNOSUPPORT */
            { 95, "传输端点上不支持该操作" }, /* EOPNOTSUPP */
            { 96, "协议族不支持" }, /* EPFNOSUPPORT */
            { 97, "协议不支持地址族" }, /* EAFNOSUPPORT */
            { 98, "地址已在使用" }, /* EADDRINUSE */
            { 99, "无法分配请求的地址" }, /* EADDRNOTAVAIL */
            { 100, "网络已关闭" }, /* ENETDOWN */
            { 101, "网络不可达" }, /* ENETUNREACH */
            { 102, "网络因重置而断开连接" }, /* ENETRESET */
            { 103, "软件导致连接中止" }, /* ECONNABORTED */
            { 104, "连接被对等方重置" }, /* ECONNRESET */
            { 105, "没有可用的缓冲区空间" }, /* ENOBUFS */
            { 106, "传输端点已连接" }, /* EISCONN */
            { 107, "传输端点未连接" }, /* ENOTCONN */
            { 108, "传输端点关闭后无法发送" }, /* ESHUTDOWN */
            { 109, "引用过多：无法拼接" }, /* ETOOMANYREFS */
            { 110, "连接超时" }, /* ETIMEDOUT */
            { 111, "连接被拒绝" }, /* ECONNREFUSED */
            { 112, "主机已关闭" }, /* EHOSTDOWN */
            { 113, "没有到主机的路由" }, /* EHOSTUNREACH */
            { 114, "操作已在进行中" }, /* EALREADY */
            { 115, "操作现在进行中" }, /* EINPROGRESS */
            { 116, "陈旧的文件句柄" }, /* ESTALE */
            { 117, "结构需要清理" }, /* EUCLEAN */
            { 118, "不是XENIX命名类型文件" }, /* ENOTNAM */
            { 119, "没有XENIX信号量可用" }, /* ENAVAIL */
            { 120, "是命名类型文件" }, /* EISNAM */
            { 121, "远程I/O错误" }, /* EREMOTEIO */
            { 122, "超出配额" }, /* EDQUOT */
            { 123, "未找到介质" }, /* ENOMEDIUM */
            { 124, "介质类型错误" }, /* EMEDIUMTYPE */
            { 125, "操作已取消" }, /* ECANCELED */
            { 126, "所需密钥不可用" }, /* ENOKEY */
            { 127, "密钥已过期" }, /* EKEYEXPIRED */
            { 128, "密钥已被撤销" }, /* EKEYREVOKED */
            { 129, "密钥被服务拒绝" }, /* EKEYREJECTED */
            { 130, "所有者已死亡" }, /* EOWNERDEAD */
            { 131, "状态不可恢复" }, /* ENOTRECOVERABLE */
            { 132, "由于RF-kill导致操作不可能" }, /* ERFKILL */
            { 133, "内存页有硬件错误" }, /* EHWPOISON */
            { 512, "ERESTARTSYS" },
            { 513, "ERESTARTNOINTR" },
            { 514, "如果没有处理程序则重启.." }, /* ERESTARTNOHAND */
            { 515, "没有ioctl命令" }, /* ENOIOCTLCMD */
            { 516, "通过调用sys_restart_syscall重启" }, /* ERESTART_RESTARTBLOCK */
            { 517, "驱动程序请求探针重试" }, /* EPROBE_DEFER */
            { 518, "open发现了一个过时的目录项" }, /* EOPENSTALE */
            { 519, "参数不支持" }, /* ENOPARAM */
            { 521, "非法NFS文件句柄" }, /* EBADHANDLE */
            { 522, "更新同步不匹配" }, /* ENOTSYNC */
            { 523, "Cookie已过期" }, /* EBADCOOKIE */
            { 524, "操作不被支持" }, /* ENOTSUPP */
            { 525, "缓冲区或请求太小" }, /* ETOOSMALL */
            { 526, "发生不可翻译的错误" }, /* ESERVERFAULT */
            { 527, "服务器不支持的类型" }, /* EBADTYPE */
            { 528, "请求已启动，但在超时前无法完成" }, /* EJUKEBOX */
            { 529, "iocb已排队，将收到完成事件" }, /* EIOCBQUEUED */
            { 530, "与召回状态冲突" }, /* ERECALLCONFLICT */
            { 531, "NFS文件锁回收被拒绝" }, /* ENOGRACE */
        };
    }
}