namespace PersonalTools.ConstString
{
    internal static partial class MacErrno
    {
        // Mac (Darwin/XNU) errno 错误码 - English（取自 Darwin sys/errno.h 标准描述）
        private static readonly Dictionary<long, string> MacErrnoMapEnglish = new()
        {
            { 0, "Success" },
            { 1, "Operation not permitted" }, /* EPERM */
            { 2, "No such file or directory" }, /* ENOENT */
            { 3, "No such process" }, /* ESRCH */
            { 4, "Interrupted system call" }, /* EINTR */
            { 5, "Input/output error" }, /* EIO */
            { 6, "Device not configured" }, /* ENXIO */
            { 7, "Argument list too long" }, /* E2BIG */
            { 8, "Exec format error" }, /* ENOEXEC */
            { 9, "Bad file descriptor" }, /* EBADF */
            { 10, "No child processes" }, /* ECHILD */
            { 11, "Resource deadlock avoided" }, /* EDEADLK */
            { 12, "Cannot allocate memory" }, /* ENOMEM */
            { 13, "Permission denied" }, /* EACCES */
            { 14, "Bad address" }, /* EFAULT */
            { 15, "Block device required" }, /* ENOTBLK */
            { 16, "Resource busy" }, /* EBUSY */
            { 17, "File exists" }, /* EEXIST */
            { 18, "Cross-device link" }, /* EXDEV */
            { 19, "Operation not supported by device" }, /* ENODEV */
            { 20, "Not a directory" }, /* ENOTDIR */
            { 21, "Is a directory" }, /* EISDIR */
            { 22, "Invalid argument" }, /* EINVAL */
            { 23, "Too many open files in system" }, /* ENFILE */
            { 24, "Too many open files" }, /* EMFILE */
            { 25, "Inappropriate ioctl for device" }, /* ENOTTY */
            { 26, "Text file busy" }, /* ETXTBSY */
            { 27, "File too large" }, /* EFBIG */
            { 28, "No space left on device" }, /* ENOSPC */
            { 29, "Illegal seek" }, /* ESPIPE */
            { 30, "Read-only file system" }, /* EROFS */
            { 31, "Too many links" }, /* EMLINK */
            { 32, "Broken pipe" }, /* EPIPE */
            { 33, "Numerical argument out of domain" }, /* EDOM */
            { 34, "Result too large" }, /* ERANGE */
            { 35, "Resource temporarily unavailable" }, /* EAGAIN, EWOULDBLOCK */
            { 36, "Operation now in progress" }, /* EINPROGRESS */
            { 37, "Operation already in progress" }, /* EALREADY */
            { 38, "Socket operation on non-socket" }, /* ENOTSOCK */
            { 39, "Destination address required" }, /* EDESTADDRREQ */
            { 40, "Message too long" }, /* EMSGSIZE */
            { 41, "Protocol wrong type for socket" }, /* EPROTOTYPE */
            { 42, "Protocol not available" }, /* ENOPROTOOPT */
            { 43, "Protocol not supported" }, /* EPROTONOSUPPORT */
            { 44, "Socket type not supported" }, /* ESOCKTNOSUPPORT */
            { 45, "Operation not supported" }, /* ENOTSUP */
            { 46, "Protocol family not supported" }, /* EPFNOSUPPORT */
            { 47, "Address family not supported by protocol family" }, /* EAFNOSUPPORT */
            { 48, "Address already in use" }, /* EADDRINUSE */
            { 49, "Can't assign requested address" }, /* EADDRNOTAVAIL */
            { 50, "Network is down" }, /* ENETDOWN */
            { 51, "Network is unreachable" }, /* ENETUNREACH */
            { 52, "Network dropped connection on reset" }, /* ENETRESET */
            { 53, "Software caused connection abort" }, /* ECONNABORTED */
            { 54, "Connection reset by peer" }, /* ECONNRESET */
            { 55, "No buffer space available" }, /* ENOBUFS */
            { 56, "Socket is already connected" }, /* EISCONN */
            { 57, "Socket is not connected" }, /* ENOTCONN */
            { 58, "Can't send after socket shutdown" }, /* ESHUTDOWN */
            { 59, "Too many references: can't splice" }, /* ETOOMANYREFS */
            { 60, "Operation timed out" }, /* ETIMEDOUT */
            { 61, "Connection refused" }, /* ECONNREFUSED */
            { 62, "Too many levels of symbolic links" }, /* ELOOP */
            { 63, "File name too long" }, /* ENAMETOOLONG */
            { 64, "Host is down" }, /* EHOSTDOWN */
            { 65, "No route to host" }, /* EHOSTUNREACH */
            { 66, "Directory not empty" }, /* ENOTEMPTY */
            { 67, "Too many processes" }, /* EPROCLIM */
            { 68, "Too many users" }, /* EUSERS */
            { 69, "Disc quota exceeded" }, /* EDQUOT */
            { 70, "Stale NFS file handle" }, /* ESTALE */
            { 71, "Too many levels of remote in path" }, /* EREMOTE */
            { 72, "RPC struct is bad" }, /* EBADRPC */
            { 73, "RPC version wrong" }, /* ERPCMISMATCH */
            { 74, "RPC prog. not avail" }, /* EPROGUNAVAIL */
            { 75, "Program version wrong" }, /* EPROGMISMATCH */
            { 76, "Bad procedure for program" }, /* EPROCUNAVAIL */
            { 77, "No locks available" }, /* ENOLCK */
            { 78, "Function not implemented" }, /* ENOSYS */
            { 79, "Inappropriate file type or format" }, /* EFTYPE */
            { 80, "Authentication error" }, /* EAUTH */
            { 81, "Need authenticator" }, /* ENEEDAUTH */
            { 82, "Device power is off" }, /* EPWROFF */
            { 83, "Device error" }, /* EDEVERR */
            { 84, "Value too large to be stored in data type" }, /* EOVERFLOW */
            { 85, "Bad executable (or shared library)" }, /* EBADEXEC */
            { 86, "Bad CPU type in executable" }, /* EBADARCH */
            { 87, "Shared library version mismatch" }, /* ESHLIBVERS */
            { 88, "Malformed Mach-o file" }, /* EBADMACHO */
            { 89, "Operation canceled" }, /* ECANCELED */
            { 90, "Identifier removed" }, /* EIDRM */
            { 91, "No message of desired type" }, /* ENOMSG */
            { 92, "Illegal byte sequence" }, /* EILSEQ */
            { 93, "Attribute not found" }, /* ENOATTR */
            { 94, "Bad message" }, /* EBADMSG */
            { 95, "EMULTIHOP (Reserved)" }, /* EMULTIHOP */
            { 96, "No message available on STREAM" }, /* ENODATA */
            { 97, "ENOLINK (Reserved)" }, /* ENOLINK */
            { 98, "No STREAM resources" }, /* ENOSR */
            { 99, "Not a STREAM" }, /* ENOSTR */
            { 100, "Protocol error" }, /* EPROTO */
            { 101, "STREAM ioctl timeout" }, /* ETIME */
            { 102, "Operation not supported on socket" }, /* EOPNOTSUPP */
            { 103, "Policy not found" }, /* ENOPOLICY */
            { 104, "State not recoverable" }, /* ENOTRECOVERABLE */
            { 105, "Previous owner died" }, /* EOWNERDEAD */
            { 106, "Interface output queue is full" }, /* EQFULL */
        };
    }
}
