namespace PersonalTools.ConstString
{
    internal static partial class MacErrno
    {
        // Mac (Darwin/XNU) errno 错误码 - 繁体中文
        private static readonly Dictionary<long, string> MacErrnoMapTraditionalChinese = new()
        {
            { 0, "成功" },
            { 1, "操作不被允許" }, /* EPERM */
            { 2, "沒有這個檔案或目錄" }, /* ENOENT */
            { 3, "沒有這個處理程序" }, /* ESRCH */
            { 4, "系統呼叫被中斷" }, /* EINTR */
            { 5, "輸入/輸出錯誤" }, /* EIO */
            { 6, "裝置未配置" }, /* ENXIO */
            { 7, "參數列表過長" }, /* E2BIG */
            { 8, "執行格式錯誤" }, /* ENOEXEC */
            { 9, "錯誤的檔案描述符" }, /* EBADF */
            { 10, "沒有子處理程序" }, /* ECHILD */
            { 11, "已避免資源死鎖" }, /* EDEADLK */
            { 12, "無法分配記憶體" }, /* ENOMEM */
            { 13, "權限被拒絕" }, /* EACCES */
            { 14, "錯誤位址" }, /* EFAULT */
            { 15, "需要塊裝置" }, /* ENOTBLK */
            { 16, "資源忙" }, /* EBUSY */
            { 17, "檔案已存在" }, /* EEXIST */
            { 18, "跨裝置連結" }, /* EXDEV */
            { 19, "裝置不支援該操作" }, /* ENODEV */
            { 20, "不是目錄" }, /* ENOTDIR */
            { 21, "是一個目錄" }, /* EISDIR */
            { 22, "無效參數" }, /* EINVAL */
            { 23, "系統開啟檔案過多" }, /* ENFILE */
            { 24, "開啟檔案過多" }, /* EMFILE */
            { 25, "對裝置不適用的ioctl操作" }, /* ENOTTY */
            { 26, "文字檔案忙" }, /* ETXTBSY */
            { 27, "檔案太大" }, /* EFBIG */
            { 28, "裝置上沒有剩餘空間" }, /* ENOSPC */
            { 29, "非法查找" }, /* ESPIPE */
            { 30, "唯讀檔案系統" }, /* EROFS */
            { 31, "連結數過多" }, /* EMLINK */
            { 32, "管道已斷開" }, /* EPIPE */
            { 33, "數學參數超出定義域" }, /* EDOM */
            { 34, "結果太大" }, /* ERANGE */
            { 35, "資源暫時不可用" }, /* EAGAIN, EWOULDBLOCK */
            { 36, "操作現在進行中" }, /* EINPROGRESS */
            { 37, "操作已在進行中" }, /* EALREADY */
            { 38, "對非通訊端執行通訊端操作" }, /* ENOTSOCK */
            { 39, "需要目標位址" }, /* EDESTADDRREQ */
            { 40, "訊息太長" }, /* EMSGSIZE */
            { 41, "協議類型對通訊端錯誤" }, /* EPROTOTYPE */
            { 42, "協議不可用" }, /* ENOPROTOOPT */
            { 43, "協議不支援" }, /* EPROTONOSUPPORT */
            { 44, "通訊端類型不支援" }, /* ESOCKTNOSUPPORT */
            { 45, "操作不被支援" }, /* ENOTSUP */
            { 46, "協議族不支援" }, /* EPFNOSUPPORT */
            { 47, "協議族不支援該位址族" }, /* EAFNOSUPPORT */
            { 48, "位址已在使用" }, /* EADDRINUSE */
            { 49, "無法分配請求的位址" }, /* EADDRNOTAVAIL */
            { 50, "網路已關閉" }, /* ENETDOWN */
            { 51, "網路不可達" }, /* ENETUNREACH */
            { 52, "網路因重設而斷開連線" }, /* ENETRESET */
            { 53, "軟體導致連線中止" }, /* ECONNABORTED */
            { 54, "連線被對等方重設" }, /* ECONNRESET */
            { 55, "沒有可用的緩衝區空間" }, /* ENOBUFS */
            { 56, "通訊端已連線" }, /* EISCONN */
            { 57, "通訊端未連線" }, /* ENOTCONN */
            { 58, "通訊端關閉後無法發送" }, /* ESHUTDOWN */
            { 59, "引用過多：無法拼接" }, /* ETOOMANYREFS */
            { 60, "操作超時" }, /* ETIMEDOUT */
            { 61, "連線被拒絕" }, /* ECONNREFUSED */
            { 62, "符號連結層級過多" }, /* ELOOP */
            { 63, "檔案名過長" }, /* ENAMETOOLONG */
            { 64, "主機已關閉" }, /* EHOSTDOWN */
            { 65, "沒有到主機的路由" }, /* EHOSTUNREACH */
            { 66, "目錄非空" }, /* ENOTEMPTY */
            { 67, "處理程序過多" }, /* EPROCLIM */
            { 68, "使用者過多" }, /* EUSERS */
            { 69, "超出磁碟配額" }, /* EDQUOT */
            { 70, "陳舊的NFS檔案控制代碼" }, /* ESTALE */
            { 71, "路徑中遠端層級過多" }, /* EREMOTE */
            { 72, "RPC結構損壞" }, /* EBADRPC */
            { 73, "RPC版本錯誤" }, /* ERPCMISMATCH */
            { 74, "RPC程式不可用" }, /* EPROGUNAVAIL */
            { 75, "程式版本錯誤" }, /* EPROGMISMATCH */
            { 76, "程式的程序錯誤" }, /* EPROCUNAVAIL */
            { 77, "沒有可用的鎖" }, /* ENOLCK */
            { 78, "功能未實現" }, /* ENOSYS */
            { 79, "不適當的檔案類型或格式" }, /* EFTYPE */
            { 80, "認證錯誤" }, /* EAUTH */
            { 81, "需要認證器" }, /* ENEEDAUTH */
            { 82, "裝置電源已關閉" }, /* EPWROFF */
            { 83, "裝置錯誤" }, /* EDEVERR */
            { 84, "值太大，無法存入資料類型" }, /* EOVERFLOW */
            { 85, "錯誤的可執行檔案（或共用程式庫）" }, /* EBADEXEC */
            { 86, "可執行檔案中的CPU類型錯誤" }, /* EBADARCH */
            { 87, "共用程式庫版本不匹配" }, /* ESHLIBVERS */
            { 88, "格式錯誤的Mach-o檔案" }, /* EBADMACHO */
            { 89, "操作已取消" }, /* ECANCELED */
            { 90, "標識符已移除" }, /* EIDRM */
            { 91, "沒有所需類型的訊息" }, /* ENOMSG */
            { 92, "非法位元組序列" }, /* EILSEQ */
            { 93, "未找到屬性" }, /* ENOATTR */
            { 94, "錯誤的訊息" }, /* EBADMSG */
            { 95, "EMULTIHOP（保留）" }, /* EMULTIHOP */
            { 96, "STREAM上沒有可用訊息" }, /* ENODATA */
            { 97, "ENOLINK（保留）" }, /* ENOLINK */
            { 98, "STREAM資源不足" }, /* ENOSR */
            { 99, "不是STREAM" }, /* ENOSTR */
            { 100, "協議錯誤" }, /* EPROTO */
            { 101, "STREAM ioctl超時" }, /* ETIME */
            { 102, "通訊端上不支援該操作" }, /* EOPNOTSUPP */
            { 103, "未找到策略" }, /* ENOPOLICY */
            { 104, "狀態不可恢復" }, /* ENOTRECOVERABLE */
            { 105, "前一個所有者已死亡" }, /* EOWNERDEAD */
            { 106, "介面輸出佇列已滿" }, /* EQFULL */
        };
    }
}
