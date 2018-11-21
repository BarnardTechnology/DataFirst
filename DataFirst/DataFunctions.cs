using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BarnardTech.DataFirst
{
    class DataFunctions
    {
        public static string GetConnectionString()
        {
#if DEBUG
            return ConfigurationManager.ConnectionStrings["DebugConnection"].ConnectionString;
#else
            return ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
#endif
        }

        public static DataTable SelectCmd(string commandText)
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlDataAdapter dataAdapter = new SqlDataAdapter(commandText, dataConn);
            DataTable dt = new DataTable();
            dataAdapter.Fill(dt);
            return dt;
        }

        internal static string GetTableName(object obj)
        {
            var attributes = obj.GetType().GetCustomAttributes(false);
            if (attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.GetType().Name == "SqlTableName")
                    {
                        SqlTableName desc = attribute as SqlTableName;
                        return desc.Name;
                    }
                }
            }

            return "";
        }

        internal static string GetTableName<T>()
        {
            var attributes = typeof(T).GetCustomAttributes(false);
            if (attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.GetType().Name == "SqlTableName")
                    {
                        SqlTableName desc = attribute as SqlTableName;
                        return desc.Name;
                    }
                }
            }

            return "";
        }

        internal static PropertyInfo GetPrimaryKey(object obj)
        {
            foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                SqlFieldName sField = GetPropertySqlFieldName(prop);
                if (sField != null)
                {
                    if (sField.isPrimaryKey)
                    {
                        return prop;
                    }
                }
            }
            return null;
        }

        internal static PropertyInfo GetAutoNumber(object obj)
        {
            foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                SqlFieldName sField = GetPropertySqlFieldName(prop);
                if (sField != null)
                {
                    if (sField.isAutoNumber)
                    {
                        return prop;
                    }
                }
            }
            return null;
        }

        internal static PropertyInfo GetPrimaryKey<T>()
        {
            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                SqlFieldName sField = GetPropertySqlFieldName(prop);
                if (sField != null)
                {
                    if (sField.isPrimaryKey)
                    {
                        return prop;
                    }
                }
            }
            return null;
        }

        internal static SqlFieldName GetPropertySqlFieldName(PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes(true);
            if (attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.GetType().Name == "SqlFieldName")
                    {
                        return attribute as SqlFieldName;
                    }
                }
            }
            return null;
        }

        /*private static object GetCsvConversion(PropertyInfo prop, object obj)
        {
            var attributes = prop.GetCustomAttributes(true);
            if (attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.GetType().Name == "JsonConverterAttribute")
                    {
                        JsonConverterAttribute converter = attribute as JsonConverterAttribute;
                        if (converter.ConverterType.Name == "CsvConverter")
                        {
                            CsvConverter csv = new CsvConverter();
                            JsonStringWriter jWrite = new JsonStringWriter();
                            csv.WriteJson(jWrite, prop.GetValue(obj), new JsonSerializer());
                            return jWrite.Output;
                        }
                    }
                }
            }

            return prop.GetValue(obj);
        }*/

        public static T GetRecord<T>(object id) where T : DataItem, new()
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());

            string tableName = GetTableName<T>();

            if (tableName == "")
            {
                throw new Exception("No SqlTableName attribute is set for object '" + typeof(T).Name + "'.");
            }

            string whereStr = "";

            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                SqlFieldName sField = GetPropertySqlFieldName(prop);
                if (sField != null)
                {
                    if (sField.isPrimaryKey)
                    {
                        whereStr = " WHERE " + sField.Name + " = @id";
                    }
                }
            }

            if (whereStr == "")
            {
                throw new Exception("Couldn't find a primary key for object with SqlTableName '" + tableName + "'.");
            }

            SqlDataAdapter dataAdapter = new SqlDataAdapter("SELECT * FROM " + tableName + " " + whereStr, dataConn);
            dataAdapter.SelectCommand.Parameters.Add(new SqlParameter("@id", id));
            DataTable dt = new DataTable();

            dataAdapter.Fill(dt);

            if (dt.Rows.Count == 1)
            {
                List<T> lItems = new List<T>();
                foreach (DataRow dRow in dt.Rows)
                {
                    lItems.Add(FromDataRow<T>(dRow));
                }

                return lItems[0];
            }
            else
            {
                return default(T);
            }
        }

        private static T FromDataRow<T>(DataRow dRow) where T : DataItem, new()
        {
            T obj = new T();
            obj._newRecord = false;

            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                SqlFieldName sField = GetPropertySqlFieldName(prop);
                if (sField != null)
                {
                    if (dRow.Table.Columns.Contains(sField.Name))
                    {
                        if (dRow[sField.Name].GetType() == typeof(DBNull))
                        {
                            prop.SetValue(obj, null);
                        }
                        else
                        {
                            if (dRow[sField.Name].GetType() == typeof(string))
                            {
                                if (prop.PropertyType == typeof(Guid))
                                {
                                    prop.SetValue(obj, Guid.Parse(dRow[sField.Name].ToString()));
                                }
                                else
                                {
                                    prop.SetValue(obj, dRow[sField.Name]);
                                }
                            }
                            else
                            {
                                prop.SetValue(obj, dRow[sField.Name]);
                            }
                        }
                    }
                }
            }
            return obj;
        }

        /*public static List<T> GetData<T>(int numRecords, int pageNumber, string whereClause = "", string overrideTableName = "", List<SqlParameter> sqlParameters = null, string orderBy = "ORDER BY id")
        {
            JsonSerializerSettings s = new JsonSerializerSettings();
            s.ContractResolver = new SqlDataTransferContractResolver();
            string jsonStr = GetDataJSON<T>(numRecords, pageNumber, whereClause, overrideTableName, sqlParameters, orderBy);
            return JsonConvert.DeserializeObject<List<T>>(jsonStr, s);
        }*/

        public static List<T> GetData<T>(int numRecords, int pageNumber, string whereClause = "", string overrideTableName = "", List<SqlParameter> sqlParameters = null, string orderBy = "ORDER BY ??") where T : DataItem, new()
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            string tableName = "";

            if (overrideTableName != "")
            {
                tableName = overrideTableName;
            }
            else
            {
                tableName = GetTableName<T>();

                if (tableName == "")
                {
                    throw new Exception("No SqlTableName attribute is set for object '" + typeof(T).Name + "'.");
                }
            }

            if (whereClause != "")
            {
                whereClause = " " + whereClause;
            }

            if (orderBy == "ORDER BY ??")
            {
                // we need to find the primary key field
                PropertyInfo pInfo = GetPrimaryKey<T>();

                if (pInfo != null)
                {
                    SqlFieldName sField = GetPropertySqlFieldName(pInfo);
                    orderBy = "ORDER BY " + sField.Name;
                }
                else
                {
                    throw new Exception("Cannot find primary key to complete ORDER BY statement.");
                }
            }

            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            if (numRecords == 0)
            {
                dataAdapter = new SqlDataAdapter("SELECT * FROM " + tableName + whereClause + " " + orderBy, dataConn);
            }
            else if (pageNumber > 0)
            {
                dataAdapter = new SqlDataAdapter("SELECT * FROM " + tableName + whereClause + " " + orderBy + " OFFSET " + (numRecords * pageNumber) + " ROWS FETCH NEXT " + numRecords + " ROWS ONLY", dataConn);
            }
            else
            {
                dataAdapter = new SqlDataAdapter("SELECT TOP (" + numRecords + ") * FROM " + tableName + whereClause + " " + orderBy, dataConn);
            }

            if (sqlParameters != null)
            {
                foreach (var param in sqlParameters)
                {
                    dataAdapter.SelectCommand.Parameters.Add(param);
                }
            }

            DataTable dt = new DataTable();
            dataAdapter.Fill(dt);

            List<T> lItems = new List<T>();
            foreach (DataRow dRow in dt.Rows)
            {
                lItems.Add(FromDataRow<T>(dRow));
            }

            return lItems;
            //JsonSerializerSettings s = new JsonSerializerSettings();
            //s.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            //return JsonConvert.SerializeObject(dt, s);
        }

        public static void UpdateData(object obj)
        {
            string tableName = GetTableName(obj);
            List<string> fieldNames = new List<string>();
            List<object> values = new List<object>();
            string whereStr = "";
            if (tableName != "")
            {
                foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    object objValue = prop.GetValue(obj);
                    SqlFieldName sField = GetPropertySqlFieldName(prop);
                    if (sField != null)
                    {
                        if (sField.isPrimaryKey)
                        {
                            // TODO: This only really works if we assume an integer value - should add to parameters instead
                            whereStr = " WHERE " + sField.Name + " = " + prop.GetValue(obj);
                        }
                        else if (sField.isReadOnly == false)
                        {
                            fieldNames.Add(sField.Name);
                            values.Add(prop.GetValue(obj));
                        }
                    }
                }
            }

            if (fieldNames.Count > 0)
            {
                string valueStr = "";
                SqlConnection dataConn = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand("UPDATE " + tableName + " SET ", dataConn);
                bool hasValues = false;

                for (int i = 0; i < fieldNames.Count; i++)
                {
                    if (values[i] != null)
                    {
                        hasValues = true;
                        valueStr += fieldNames[i] + " = @" + fieldNames[i] + ", ";
                        cmd.Parameters.Add(new SqlParameter("@" + fieldNames[i], values[i]));
                    }
                }

                if (hasValues)
                {
                    valueStr = valueStr.Substring(0, valueStr.Length - 2);
                    cmd.CommandText += valueStr + whereStr;
                    dataConn.Open();
                    SqlDataReader dRead = cmd.ExecuteReader();
                    dataConn.Close();
                }
            }
        }

        public static object InsertData(object obj)
        {
            string tableName = GetTableName(obj);
            List<string> fieldNames = new List<string>();
            List<object> values = new List<object>();
            string autoNumberField = "id"; // default autonumber field
            PropertyInfo autoNumberProperty = null;

            if (tableName != "")
            {
                foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    object objValue = prop.GetValue(obj);
                    SqlFieldName sField = GetPropertySqlFieldName(prop);
                    if (sField != null)
                    {
                        if (!sField.isAutoNumber && !sField.isReadOnly)
                        {
                            fieldNames.Add(sField.Name);
                            values.Add(prop.GetValue(obj));
                        }

                        if (sField.isAutoNumber)
                        {
                            autoNumberField = sField.Name;
                            autoNumberProperty = prop;
                        }
                    }
                }
            }

            if (fieldNames.Count > 0)
            {
                string fieldStr = "";
                string valueStr = "";
                SqlConnection dataConn = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand("INSERT INTO " + tableName + " ", dataConn);
                bool hasValues = false;

                for (var i = 0; i < fieldNames.Count; i++)
                {
                    if (values[i] != null)
                    {
                        bool skipValue = false;

                        if (values[i].GetType() == typeof(DateTime))
                        {
                            if ((DateTime)values[i] == DateTime.MinValue)
                            {
                                // TODO: If MinValue, assume null. Not fantastic, but will do for now.
                                skipValue = true;
                            }
                        }

                        if (!skipValue)
                        {
                            hasValues = true;
                            fieldStr += fieldNames[i] + ", ";
                            valueStr += "@" + fieldNames[i] + ", ";
                            cmd.Parameters.Add(new SqlParameter("@" + fieldNames[i], values[i]));
                        }
                    }
                }

                if (hasValues)
                {
                    fieldStr = "(" + fieldStr.Substring(0, fieldStr.Length - 2) + ") output INSERTED." + autoNumberField;
                    valueStr = "(" + valueStr.Substring(0, valueStr.Length - 2) + ")";
                    cmd.CommandText += fieldStr + " VALUES " + valueStr;

                    dataConn.Open();
                    object retVal = cmd.ExecuteScalar();
                    dataConn.Close();

                    if (autoNumberProperty != null)
                    {
                        if (autoNumberProperty.PropertyType == typeof(Guid))
                        {
                            Guid retGuid = Guid.Parse(retVal.ToString());
                            autoNumberProperty.SetValue(obj, retGuid);
                            return retGuid;
                        }
                        else
                        {
                            long outId = (long)retVal;
                            autoNumberProperty.SetValue(obj, outId);
                            return outId;
                        }
                    }
                }
            }

            return -1;
        }

        public static void DeleteData(object obj)
        {
            string tableName = GetTableName(obj);
            List<string> fieldNames = new List<string>();
            List<object> values = new List<object>();
            string whereStr = "";

            SqlParameter primaryKey = null;

            if (tableName != "")
            {
                foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    object objValue = prop.GetValue(obj);
                    SqlFieldName sField = GetPropertySqlFieldName(prop);
                    if (sField != null)
                    {
                        if (sField.isPrimaryKey)
                        {
                            whereStr = " WHERE " + sField.Name + " = @primaryKey";
                            primaryKey = new SqlParameter("@primaryKey", prop.GetValue(obj));
                        }
                        else
                        {
                            fieldNames.Add(sField.Name);
                            values.Add(prop.GetValue(obj));
                        }
                    }
                }
            }
            else
            {
                throw new Exception("No table name found for '" + obj.GetType().Name + "'.");
            }

            if (primaryKey == null)
            {
                throw new Exception("No primary key found for '" + obj.GetType().Name + "'.");
            }

            if (whereStr != "")
            {
                SqlConnection dataConn = new SqlConnection(GetConnectionString());
                SqlCommand cmd = new SqlCommand("DELETE FROM " + tableName + " " + whereStr, dataConn);
                cmd.Parameters.Add(primaryKey);

                dataConn.Open();
                SqlDataReader dRead = cmd.ExecuteReader();
                dataConn.Close();
            }
            else
            {
                throw new Exception("No WHERE clause was available for DELETE.");
            }
        }

        public static void ShallowCopy(object fromObj, object toObj)
        {
            foreach (PropertyInfo p in fromObj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                p.SetValue(toObj, p.GetValue(fromObj));
            }

            foreach (FieldInfo f in fromObj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                f.SetValue(toObj, f.GetValue(fromObj));
            }
        }
    }
}