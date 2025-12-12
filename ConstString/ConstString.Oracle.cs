using System.Collections.Generic;

namespace MyTool
{
    public static partial class ConstString
    {
        // Oracle SQLCODE
        public static readonly Dictionary<int, string> OracleSqlCodeMap = new Dictionary<int, string>
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
            { 1407, "Cannot update to NULL" },
        };
    }
}