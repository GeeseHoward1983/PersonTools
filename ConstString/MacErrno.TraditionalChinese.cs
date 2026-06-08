namespace PersonalTools.ConstString
{
    internal static partial class MacErrno
    {
        // Mac (Darwin/XNU) errno 错误码 - 繁体中文
        private static readonly Dictionary<long, string> MacErrnoMapTraditionalChinese = new()
        {
            { 0, "成功" },
            { 1, "操作不被允許" }, /* EPERM */
            { 2, "沒有這個文件或目錄" }, /* ENOENT */
            { 3, "沒有這個進程" }, /* ESRCH */
            { 4, "系統調用被中斷" }, /* EINTR */
            { 5, "輸入/輸出錯誤" }, /* EIO */
            { 6, "設備未配置" }, /* ENXIO */
            { 7, "參數列表過長" }, /* E2BIG */
            { 8, "執行格式錯誤" }, /* ENOEXEC */
            { 9, "錯誤的文件描述符" }, /* EBADF */
            { 10, "沒有子進程" }, /* ECHILD */
            { 11, "已避免資源死鎖" }, /* EDEADLK */
            { 12, "無法分配內存" }, /* ENOMEM */
            { 13, "權限被拒絕" }, /* EACCES */
            { 14, "錯誤地址" }, /* EFAULT */
            { 15, "需要塊設備" }, /* ENOTBLK */
            { 16, "資源忙" }, /* EBUSY */
            { 17, "文件已存在" }, /* EEXIST */
            { 18, "跨設備鏈接" }, /* EXDEV */
            { 19, "設備不支持該操作" }, /* ENODEV */
            { 20, "不是目錄" }, /* ENOTDIR */
            { 21, "是一個目錄" }, /* EISDIR */
            { 22, "無效參數" }, /* EINVAL */
            { 23, "系統打開文件過多" }, /* ENFILE */
            { 24, "打開文件過多" }, /* EMFILE */
            { 25, "對設備不適用的ioctl操作" }, /* ENOTTY */
            { 26, "文本文件忙" }, /* ETXTBSY */
            { 27, "文件太大" }, /* EFBIG */
            { 28, "設備上沒有剩餘空間" }, /* ENOSPC */
            { 29, "非法查找" }, /* ESPIPE */
            { 30, "只讀文件系統" }, /* EROFS */
            { 31, "鏈接數過多" }, /* EMLINK */
            { 32, "管道已斷開" }, /* EPIPE */
            { 33, "數學參數超出定義域" }, /* EDOM */
            { 34, "結果太大" }, /* ERANGE */
            { 35, "資源暫時不可用" }, /* EAGAIN, EWOULDBLOCK */
            { 36, "操作現在進行中" }, /* EINPROGRESS */
            { 37, "操作已在進行中" }, /* EALREADY */
            { 38, "對非套接字執行套接字操作" }, /* ENOTSOCK */
            { 39, "需要目標地址" }, /* EDESTADDRREQ */
            { 40, "消息太長" }, /* EMSGSIZE */
            { 41, "協議類型對套接字錯誤" }, /* EPROTOTYPE */
            { 42, "協議不可用" }, /* ENOPROTOOPT */
            { 43, "協議不支持" }, /* EPROTONOSUPPORT */
            { 44, "套接字類型不支持" }, /* ESOCKTNOSUPPORT */
            { 45, "操作不被支持" }, /* ENOTSUP */
            { 46, "協議族不支持" }, /* EPFNOSUPPORT */
            { 47, "協議族不支持該地址族" }, /* EAFNOSUPPORT */
            { 48, "地址已在使用" }, /* EADDRINUSE */
            { 49, "無法分配請求的地址" }, /* EADDRNOTAVAIL */
            { 50, "網絡已關閉" }, /* ENETDOWN */
            { 51, "網絡不可達" }, /* ENETUNREACH */
            { 52, "網絡因重置而斷開連接" }, /* ENETRESET */
            { 53, "軟件導致連接中止" }, /* ECONNABORTED */
            { 54, "連接被對等方重置" }, /* ECONNRESET */
            { 55, "沒有可用的緩衝區空間" }, /* ENOBUFS */
            { 56, "套接字已連接" }, /* EISCONN */
            { 57, "套接字未連接" }, /* ENOTCONN */
            { 58, "套接字關閉後無法發送" }, /* ESHUTDOWN */
            { 59, "引用過多：無法拼接" }, /* ETOOMANYREFS */
            { 60, "操作超時" }, /* ETIMEDOUT */
            { 61, "連接被拒絕" }, /* ECONNREFUSED */
            { 62, "符號鏈接層級過多" }, /* ELOOP */
            { 63, "文件名過長" }, /* ENAMETOOLONG */
            { 64, "主機已關閉" }, /* EHOSTDOWN */
            { 65, "沒有到主機的路由" }, /* EHOSTUNREACH */
            { 66, "目錄非空" }, /* ENOTEMPTY */
            { 67, "進程過多" }, /* EPROCLIM */
            { 68, "用戶過多" }, /* EUSERS */
            { 69, "超出磁盤配額" }, /* EDQUOT */
            { 70, "陳舊的NFS文件句柄" }, /* ESTALE */
            { 71, "路徑中遠程層級過多" }, /* EREMOTE */
            { 72, "RPC結構損壞" }, /* EBADRPC */
            { 73, "RPC版本錯誤" }, /* ERPCMISMATCH */
            { 74, "RPC程序不可用" }, /* EPROGUNAVAIL */
            { 75, "程序版本錯誤" }, /* EPROGMISMATCH */
            { 76, "程序的過程錯誤" }, /* EPROCUNAVAIL */
            { 77, "沒有可用的鎖" }, /* ENOLCK */
            { 78, "功能未實現" }, /* ENOSYS */
            { 79, "不適當的文件類型或格式" }, /* EFTYPE */
            { 80, "認證錯誤" }, /* EAUTH */
            { 81, "需要認證器" }, /* ENEEDAUTH */
            { 82, "設備電源已關閉" }, /* EPWROFF */
            { 83, "設備錯誤" }, /* EDEVERR */
            { 84, "值太大，無法存入數據類型" }, /* EOVERFLOW */
            { 85, "錯誤的可執行文件（或共享庫）" }, /* EBADEXEC */
            { 86, "可執行文件中的CPU類型錯誤" }, /* EBADARCH */
            { 87, "共享庫版本不匹配" }, /* ESHLIBVERS */
            { 88, "格式錯誤的Mach-o文件" }, /* EBADMACHO */
            { 89, "操作已取消" }, /* ECANCELED */
            { 90, "標識符已移除" }, /* EIDRM */
            { 91, "沒有所需類型的消息" }, /* ENOMSG */
            { 92, "非法字節序列" }, /* EILSEQ */
            { 93, "未找到屬性" }, /* ENOATTR */
            { 94, "錯誤的消息" }, /* EBADMSG */
            { 95, "EMULTIHOP（保留）" }, /* EMULTIHOP */
            { 96, "STREAM上沒有可用消息" }, /* ENODATA */
            { 97, "ENOLINK（保留）" }, /* ENOLINK */
            { 98, "STREAM資源不足" }, /* ENOSR */
            { 99, "不是STREAM" }, /* ENOSTR */
            { 100, "協議錯誤" }, /* EPROTO */
            { 101, "STREAM ioctl超時" }, /* ETIME */
            { 102, "套接字上不支持該操作" }, /* EOPNOTSUPP */
            { 103, "未找到策略" }, /* ENOPOLICY */
            { 104, "狀態不可恢復" }, /* ENOTRECOVERABLE */
            { 105, "前一個所有者已死亡" }, /* EOWNERDEAD */
            { 106, "接口輸出隊列已滿" }, /* EQFULL */
        };
    }
}
