using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EClaimsWeb.Helpers
{
    public class ListtoDataTableConverter
    {
        public DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);
            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            string columnName = string.Empty;
            foreach (PropertyInfo prop in Props)
            {
                var columnAttr = prop.GetCustomAttribute<ColumnAttribute>();
                var DisplayAttribute = prop.GetCustomAttributes<DisplayAttribute>().FirstOrDefault();
                columnName = DisplayAttribute != null ? DisplayAttribute.Name : prop.Name;
                //var attributes = prop.CustomAttributes;

                //Setting column names as Property names
                dataTable.Columns.Add(columnName);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }
    }
}
