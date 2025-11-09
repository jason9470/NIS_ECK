using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Reflection;

namespace NIS.Models {
    public static class ModelsBasic {
        /// <summary>
        /// 參考網址
        /// http://stackoverflow.com/questions/6038255/asp-net-mvc-helpers-merging-two-object-htmlattributes-together
        /// object to Dictionary
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary(this object data) {
            if (data == null) return null; // Or throw an ArgumentNullException if you want

            BindingFlags publicAttributes = BindingFlags.Public | BindingFlags.Instance;
            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            foreach (PropertyInfo property in
                     data.GetType().GetProperties(publicAttributes)) {
                if (property.CanRead) {
                    dictionary.Add(property.Name, property.GetValue(data, null));
                }
            }
            return dictionary;
        }
    }

    /// <summary>DataTable To List</summary>
    public static class DataTableExtensions {
        public static IList<T> ToList<T>(this DataTable table) where T : new() {
            IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            IList<T> result = new List<T>();

            //取得DataTable所有的row data
            foreach (var row in table.Rows) {
                var item = MappingItem<T>((DataRow)row, properties);
                result.Add(item);
            }
            return result;
        }

        private static T MappingItem<T>(DataRow row, IList<PropertyInfo> properties) where T : new() {
            T item = new T();
            foreach (var property in properties) {
                if (row.Table.Columns.Contains(property.Name)) {
                    //針對欄位的型態去轉換
                    if (property.PropertyType == typeof(DateTime)) {
                        DateTime dt = new DateTime();
                        if (DateTime.TryParse(row[property.Name].ToString(), out dt)) {
                            property.SetValue(item, dt, null);
                        } else {
                            property.SetValue(item, null, null);
                        }
                    } else if (property.PropertyType == typeof(decimal)) {
                        decimal val = new decimal();
                        decimal.TryParse(row[property.Name].ToString(), out val);
                        property.SetValue(item, val, null);
                    } else if (property.PropertyType == typeof(double)) {
                        double val = new double();
                        double.TryParse(row[property.Name].ToString(), out val);
                        property.SetValue(item, val, null);
                    } else if (property.PropertyType == typeof(int)) {
                        int val = new int();
                        int.TryParse(row[property.Name].ToString(), out val);
                        property.SetValue(item, val, null);
                    } else {
                        if (row[property.Name] != DBNull.Value) {
                            property.SetValue(item, row[property.Name], null);
                        }
                    }
                }
            }
            return item;
        }
    }
}