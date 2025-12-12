using System.Collections.Generic;

namespace MyTool
{
    public static partial class ConstString
    {
        // ODBC 错误码
        public static readonly Dictionary<int, string> OdbcErrorsMap = new Dictionary<int, string>
        {
            { 0, "SQL_SUCCESS" },
            { 1, "SQL_SUCCESS_WITH_INFO" },
            { -1, "SQL_ERROR" },
            { -2, "SQL_INVALID_HANDLE" },
            { 100, "SQL_NO_DATA" },
            { 101, "SQL_STILL_EXECUTING" },
            { 102, "SQL_NEED_DATA" },
            { 110, "SQL_PARAM_DATA_AVAILABLE" },
            { 200, "General warning" },
            { 201, "Cursor operation conflict" },
            { 202, "Dynamic cursor not supported" },
            { 203, "Invalid cursor name" },
            { 204, "Connection not found" },
            { 205, "Invalid connection string attribute" },
            { 206, "Invalid transaction state" },
            { 207, "Invalid transaction termination" },
            { 208, "Transaction rollback" },
            { 209, "Transaction resolution unknown" },
            { 210, "Authorization violation" },
            { 211, "Invalid authorization specification" },
            { 212, "Invalid catalog name" },
            { 213, "Invalid schema name" },
            { 214, "Invalid role specification" },
            { 215, "Invalid transaction initiation" },
            { 216, "Invalid locator specification" },
            { 217, "Invalid grantor" },
            { 218, "Invalid grant operation" },
            { 219, "Invalid revoke operation" },
            { 220, "Invalid privilege specification" },
            { 221, "Invalid target type specification" },
            { 222, "Invalid role name" },
            { 223, "Invalid transform group name specification" },
            { 224, "Invalid transform group specification" },
            { 225, "Invalid SQL invocation name" },
            { 226, "Invalid SQL routine name" },
            { 227, "Invalid SQL routine specification" },
            { 228, "SQL routine body contains invalid SQL statement" },
            { 229, "Invalid SQL statement in SQL routine body" },
            { 230, "Invalid SQL statement repetition in SQL routine body" },
            { 231, "Invalid SQL statement sequence in SQL routine body" },
            { 232, "Invalid SQL routine body" },
            { 233, "Invalid SQL routine body definition" },
            { 234, "Invalid SQL routine body specification" },
            { 235, "Invalid SQL routine body declaration" },
            { 236, "Invalid SQL routine body implementation" },
            { 237, "Invalid SQL routine body implementation specification" },
            { 238, "Invalid SQL routine body implementation definition" },
            { 239, "Invalid SQL routine body implementation declaration" },
            { 240, "Invalid SQL routine body implementation specification definition" },
            { 241, "Invalid SQL routine body implementation specification definition declaration" },
            { 242, "Invalid SQL routine body implementation specification definition declaration specification" },
            { 243, "Invalid SQL routine body implementation specification definition declaration specification definition" },
            { 244, "Invalid SQL routine body implementation specification definition declaration specification definition declaration" },
            { 245, "Invalid SQL routine body implementation specification definition declaration specification definition declaration specification" }
        };
    }
}