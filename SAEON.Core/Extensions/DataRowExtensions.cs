using System;
using System.Collections.Generic;
using System.Data;

namespace SAEON.Core
{
    public static class DataRowExtensions
    {
        public static string AsString(this DataRow dataRow, bool oneLine = true)
        {
            List<string> result = new List<string>();
            foreach (DataColumn col in dataRow.Table.Columns)
                result.Add($"{col.ColumnName}: {dataRow[col.ColumnName]}");
            return string.Join(oneLine ? "; " : Environment.NewLine, result);
        }

        public static T GetValue<T>(this DataRow dataRow, string columnName)
        {
            if (dataRow.IsNull(columnName))
                return default;
            else
                return (T)dataRow[columnName];
        }

        public static T SetValue<T>(this DataRow dataRow, string columnName, T value)
        {
            if (value == null)
                dataRow[columnName] = DBNull.Value;
            else
                dataRow[columnName] = value;
            return value;
        }
    }
}
