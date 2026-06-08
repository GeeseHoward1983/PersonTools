namespace PersonalTools.ConstString
{
    internal static partial class MacErrno
    {
        // Mac (Darwin/XNU) errno 错误码 - 简体中文
        private static readonly Dictionary<long, string> MacErrnoMapSimplifiedChinese = new()
        {
            { 0, "成功" },
            { 1, "操作不被允许" }, /* EPERM */
            { 2, "没有这个文件或目录" }, /* ENOENT */
            { 3, "没有这个进程" }, /* ESRCH */
            { 4, "系统调用被中断" }, /* EINTR */
            { 5, "输入/输出错误" }, /* EIO */
            { 6, "设备未配置" }, /* ENXIO */
            { 7, "参数列表过长" }, /* E2BIG */
            { 8, "执行格式错误" }, /* ENOEXEC */
            { 9, "错误的文件描述符" }, /* EBADF */
            { 10, "没有子进程" }, /* ECHILD */
            { 11, "已避免资源死锁" }, /* EDEADLK */
            { 12, "无法分配内存" }, /* ENOMEM */
            { 13, "权限被拒绝" }, /* EACCES */
            { 14, "错误地址" }, /* EFAULT */
            { 15, "需要块设备" }, /* ENOTBLK */
            { 16, "资源忙" }, /* EBUSY */
            { 17, "文件已存在" }, /* EEXIST */
            { 18, "跨设备链接" }, /* EXDEV */
            { 19, "设备不支持该操作" }, /* ENODEV */
            { 20, "不是目录" }, /* ENOTDIR */
            { 21, "是一个目录" }, /* EISDIR */
            { 22, "无效参数" }, /* EINVAL */
            { 23, "系统打开文件过多" }, /* ENFILE */
            { 24, "打开文件过多" }, /* EMFILE */
            { 25, "对设备不适用的ioctl操作" }, /* ENOTTY */
            { 26, "文本文件忙" }, /* ETXTBSY */
            { 27, "文件太大" }, /* EFBIG */
            { 28, "设备上没有剩余空间" }, /* ENOSPC */
            { 29, "非法查找" }, /* ESPIPE */
            { 30, "只读文件系统" }, /* EROFS */
            { 31, "链接数过多" }, /* EMLINK */
            { 32, "管道已断开" }, /* EPIPE */
            { 33, "数学参数超出定义域" }, /* EDOM */
            { 34, "结果太大" }, /* ERANGE */
            { 35, "资源暂时不可用" }, /* EAGAIN, EWOULDBLOCK */
            { 36, "操作现在进行中" }, /* EINPROGRESS */
            { 37, "操作已在进行中" }, /* EALREADY */
            { 38, "对非套接字执行套接字操作" }, /* ENOTSOCK */
            { 39, "需要目标地址" }, /* EDESTADDRREQ */
            { 40, "消息太长" }, /* EMSGSIZE */
            { 41, "协议类型对套接字错误" }, /* EPROTOTYPE */
            { 42, "协议不可用" }, /* ENOPROTOOPT */
            { 43, "协议不支持" }, /* EPROTONOSUPPORT */
            { 44, "套接字类型不支持" }, /* ESOCKTNOSUPPORT */
            { 45, "操作不被支持" }, /* ENOTSUP */
            { 46, "协议族不支持" }, /* EPFNOSUPPORT */
            { 47, "协议族不支持该地址族" }, /* EAFNOSUPPORT */
            { 48, "地址已在使用" }, /* EADDRINUSE */
            { 49, "无法分配请求的地址" }, /* EADDRNOTAVAIL */
            { 50, "网络已关闭" }, /* ENETDOWN */
            { 51, "网络不可达" }, /* ENETUNREACH */
            { 52, "网络因重置而断开连接" }, /* ENETRESET */
            { 53, "软件导致连接中止" }, /* ECONNABORTED */
            { 54, "连接被对等方重置" }, /* ECONNRESET */
            { 55, "没有可用的缓冲区空间" }, /* ENOBUFS */
            { 56, "套接字已连接" }, /* EISCONN */
            { 57, "套接字未连接" }, /* ENOTCONN */
            { 58, "套接字关闭后无法发送" }, /* ESHUTDOWN */
            { 59, "引用过多：无法拼接" }, /* ETOOMANYREFS */
            { 60, "操作超时" }, /* ETIMEDOUT */
            { 61, "连接被拒绝" }, /* ECONNREFUSED */
            { 62, "符号链接层级过多" }, /* ELOOP */
            { 63, "文件名过长" }, /* ENAMETOOLONG */
            { 64, "主机已关闭" }, /* EHOSTDOWN */
            { 65, "没有到主机的路由" }, /* EHOSTUNREACH */
            { 66, "目录非空" }, /* ENOTEMPTY */
            { 67, "进程过多" }, /* EPROCLIM */
            { 68, "用户过多" }, /* EUSERS */
            { 69, "超出磁盘配额" }, /* EDQUOT */
            { 70, "陈旧的NFS文件句柄" }, /* ESTALE */
            { 71, "路径中远程层级过多" }, /* EREMOTE */
            { 72, "RPC结构损坏" }, /* EBADRPC */
            { 73, "RPC版本错误" }, /* ERPCMISMATCH */
            { 74, "RPC程序不可用" }, /* EPROGUNAVAIL */
            { 75, "程序版本错误" }, /* EPROGMISMATCH */
            { 76, "程序的过程错误" }, /* EPROCUNAVAIL */
            { 77, "没有可用的锁" }, /* ENOLCK */
            { 78, "功能未实现" }, /* ENOSYS */
            { 79, "不适当的文件类型或格式" }, /* EFTYPE */
            { 80, "认证错误" }, /* EAUTH */
            { 81, "需要认证器" }, /* ENEEDAUTH */
            { 82, "设备电源已关闭" }, /* EPWROFF */
            { 83, "设备错误" }, /* EDEVERR */
            { 84, "值太大，无法存入数据类型" }, /* EOVERFLOW */
            { 85, "错误的可执行文件（或共享库）" }, /* EBADEXEC */
            { 86, "可执行文件中的CPU类型错误" }, /* EBADARCH */
            { 87, "共享库版本不匹配" }, /* ESHLIBVERS */
            { 88, "格式错误的Mach-o文件" }, /* EBADMACHO */
            { 89, "操作已取消" }, /* ECANCELED */
            { 90, "标识符已移除" }, /* EIDRM */
            { 91, "没有所需类型的消息" }, /* ENOMSG */
            { 92, "非法字节序列" }, /* EILSEQ */
            { 93, "未找到属性" }, /* ENOATTR */
            { 94, "错误的消息" }, /* EBADMSG */
            { 95, "EMULTIHOP（保留）" }, /* EMULTIHOP */
            { 96, "STREAM上没有可用消息" }, /* ENODATA */
            { 97, "ENOLINK（保留）" }, /* ENOLINK */
            { 98, "STREAM资源不足" }, /* ENOSR */
            { 99, "不是STREAM" }, /* ENOSTR */
            { 100, "协议错误" }, /* EPROTO */
            { 101, "STREAM ioctl超时" }, /* ETIME */
            { 102, "套接字上不支持该操作" }, /* EOPNOTSUPP */
            { 103, "未找到策略" }, /* ENOPOLICY */
            { 104, "状态不可恢复" }, /* ENOTRECOVERABLE */
            { 105, "前一个所有者已死亡" }, /* EOWNERDEAD */
            { 106, "接口输出队列已满" }, /* EQFULL */
        };
    }
}
