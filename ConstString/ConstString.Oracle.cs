using MyTool.Enums;

namespace MyTool
{
    public static partial class ConstString
    {
        // Oracle SQLCODE - 英文
        private static readonly Dictionary<long, string> OracleSqlCodeMapEnglish = new()
        {
            { 1, "Unique constraint violated" },
            { 100, "No data found" },
            { 1012, "Not logged on" },
            { 1400, "Cannot insert NULL" },
            { 1403, "No data found" },
            { 1422, "Exact fetch returns more than requested number of rows" },
            { 1722, "Invalid number" },
            { 2292, "Integrity constraint violated - child record found" },
            { 12899, "Value too large for column" },
            { 2291, "Integrity constraint violated - parent key not found" },
            { 2290, "Check constraint violated" },
            { 2449, "Unique/primary keys in table referenced by foreign keys" },
            { 12541, "TNS:no listener" },
            { 12154, "TNS:could not resolve the connect identifier specified" },
            { 12514, "TNS:listener does not currently know of service requested in connect descriptor" },
            { 1017, "Invalid username/password; logon denied" },
            { 942, "Table or view does not exist" },
            { 936, "Missing expression" },
            { 933, "SQL command not properly ended" },
            { 923, "FROM keyword not found where expected" },
            { 921, "Unexpected end of SQL command" },
            { 918, "Column ambiguously defined" },
            { 911, "Invalid character" },
            { 904, "Invalid identifier" },
            { 1861, "Literal does not match format string" },
            { 1843, "Not a valid month" },
            { 1830, "Date format picture ends before converting entire input string" },
            { 1461, "Can bind a LONG value only for insert into a LONG column" },
            { 1407, "Cannot update to NULL" }
        };

        // Oracle SQLCODE - 简体中文
        private static readonly Dictionary<long, string> OracleSqlCodeMapSimplifiedChinese = new()
        {
            { 1, "唯一约束违反" },
            { 100, "未找到数据" },
            { 1012, "未登录" },
            { 1400, "无法插入NULL" },
            { 1403, "未找到数据" },
            { 1422, "精确提取返回的行数超过请求的行数" },
            { 1722, "无效数字" },
            { 2292, "完整性约束违反 - 找到子记录" },
            { 12899, "值对于列来说太大" },
            { 2291, "完整性约束违反 - 未找到父键" },
            { 2290, "检查约束违反" },
            { 2449, "表中外键引用的唯一/主键" },
            { 12541, "TNS:无监听器" },
            { 12154, "TNS:无法解析指定的连接标识符" },
            { 12514, "TNS:监听器当前不知道连接描述符中请求的服务" },
            { 1017, "用户名/密码无效；登录被拒绝" },
            { 942, "表或视图不存在" },
            { 936, "缺少表达式" },
            { 933, "SQL命令未正确结束" },
            { 923, "在预期位置未找到FROM关键字" },
            { 921, "SQL命令意外结束" },
            { 918, "列定义模糊" },
            { 911, "无效字符" },
            { 904, "无效标识符" },
            { 1861, "文字与格式字符串不匹配" },
            { 1843, "不是有效的月份" },
            { 1830, "日期格式图片在转换整个输入字符串之前结束" },
            { 1461, "只能将LONG值绑定到LONG列的插入操作中" },
            { 1407, "无法更新为NULL" }
        };

        // Oracle SQLCODE - 繁体中文
        private static readonly Dictionary<long, string> OracleSqlCodeMapTraditionalChinese = new()
        {
            { 1, "唯一約束違反" },
            { 100, "未找到數據" },
            { 1012, "未登錄" },
            { 1400, "無法插入NULL" },
            { 1403, "未找到數據" },
            { 1422, "精確提取返回的行數超過請求的行數" },
            { 1722, "無效數字" },
            { 2292, "完整性約束違反 - 找到子記錄" },
            { 12899, "值對於列來說太大" },
            { 2291, "完整性約束違反 - 未找到父鍵" },
            { 2290, "檢查約束違反" },
            { 2449, "表中外鍵引用的唯一/主鍵" },
            { 12541, "TNS:無監聽器" },
            { 12154, "TNS:無法解析指定的連接標識符" },
            { 12514, "TNS:監聽器當前不知道連接描述符中請求的服務" },
            { 1017, "用戶名/密碼無效；登錄被拒絕" },
            { 942, "表或視圖不存在" },
            { 936, "缺少表達式" },
            { 933, "SQL命令未正確結束" },
            { 923, "在預期位置未找到FROM關鍵字" },
            { 921, "SQL命令意外結束" },
            { 918, "列定義模糊" },
            { 911, "無效字符" },
            { 904, "無效標識符" },
            { 1861, "文字與格式字符串不匹配" },
            { 1843, "不是有效的月份" },
            { 1830, "日期格式圖片在轉換整個輸入字符串之前結束" },
            { 1461, "只能將LONG值綁定到LONG列的插入操作中" },
            { 1407, "無法更新為NULL" }
        };

        // Oracle SQLCODE
        public static Dictionary<long, string> OracleSqlCodeMap
        {
            get
            {
                return GlobalState.CurrentLanguageType switch
                {
                    LanguageType.SimplifiedChinese => OracleSqlCodeMapSimplifiedChinese,
                    LanguageType.TraditionalChinese => OracleSqlCodeMapTraditionalChinese,
                    _ => OracleSqlCodeMapEnglish
                };
            }
        }
    }
}