using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BarnardTech.DataFirst
{
    class DataFunctions
    {
        internal static string GetConnectionString()
        {
            return Config.ConnectionString;
        }

        public static DataSet ExecuteDataSet(string commandText)
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlDataAdapter dataAdapter = new SqlDataAdapter(commandText, dataConn);
            DataSet dt = new DataSet();
            dataAdapter.Fill(dt);
            return dt;
        }

        public static async Task<DataSet> ExecuteDataSetAsync(string commandText)
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlDataAdapter dataAdapter = new SqlDataAdapter(commandText, dataConn);
            DataSet dt = new DataSet();
            await Task.Run(() => dataAdapter.Fill(dt));
            return dt;
        }

        public static DataTable ExecuteQuery(string commandText)
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlDataAdapter dataAdapter = new SqlDataAdapter(commandText, dataConn);
            DataTable dt = new DataTable();
            dataAdapter.Fill(dt);
            return dt;
        }

        public static DataTable ExecuteQuery(string commandText, List<SqlParameter> parameters)
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlDataAdapter dataAdapter = new SqlDataAdapter(commandText, dataConn);
            foreach (var param in parameters)
            {
                dataAdapter.SelectCommand.Parameters.Add(param);
            }
            DataTable dt = new DataTable();
            dataAdapter.Fill(dt);
            return dt;
        }

        public static int ExecuteNonQuery(string commandText)
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlCommand sCmd = new SqlCommand(commandText, dataConn);
            dataConn.Open();
            int rows = sCmd.ExecuteNonQuery();
            dataConn.Close();
            return rows;
        }

        public static int ExecuteNonQuery(string commandText, List<SqlParameter> parameters)
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlCommand sCmd = new SqlCommand(commandText, dataConn);
            foreach (var param in parameters)
            {
                sCmd.Parameters.Add(param);
            }
            dataConn.Open();
            int rows = sCmd.ExecuteNonQuery();
            dataConn.Close();
            return rows;
        }

        public static string GetTableName(object obj, bool forRead)
        {
            var attributes = obj.GetType().GetCustomAttributes(false);
            if (attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.GetType().Name == "SqlTableName")
                    {
                        SqlTableName desc = attribute as SqlTableName;
                        if (forRead)
                        {
                            if (!String.IsNullOrEmpty(desc.ReadTableOverride))
                            {
                                return desc.ReadTableOverride;
                            }
                        }
                        return desc.Name;
                    }
                }
            }

            return "";
        }

        internal static string GetTableName<T>(bool forRead)
        {
            var attributes = typeof(T).GetCustomAttributes(false);
            if (attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.GetType().Name == "SqlTableName")
                    {
                        SqlTableName desc = attribute as SqlTableName;
                        if (forRead)
                        {
                            if (!String.IsNullOrEmpty(desc.ReadTableOverride))
                            {
                                return desc.ReadTableOverride;
                            }
                        }
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

        internal static List<Tuple<PropertyInfo, SqlRelatedTable>> GetRelatedProperties(object obj)
        {
            return GetRelatedProperties(obj.GetType());
        }

        internal static List<Tuple<PropertyInfo, SqlRelatedTable>> GetRelatedProperties(Type objType)
        {
            List<Tuple<PropertyInfo, SqlRelatedTable>> properties = new List<Tuple<PropertyInfo, SqlRelatedTable>>();
            foreach (PropertyInfo prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                SqlRelatedTable sField = GetPropertySqlRelatedTable(prop);
                if (sField != null)
                {
                    properties.Add(new Tuple<PropertyInfo, SqlRelatedTable>(prop, sField));
                }
            }
            return properties;
        }

        public class TypeRelationship
        {
            public PropertyInfo LocalProperty;
            public SqlRelatedTable LocalRelationship;
            public PropertyInfo ForeignProperty;
            public SqlRelatedTable ForeignRelationship;
        }

        internal static TypeRelationship GetRelationship(object localInnerObject, Type localType, Type foreignType)
        {
            return GetRelationship(localInnerObject.GetType(), localType, foreignType);
        }

        internal static TypeRelationship GetRelationship(Type localInnerType, Type localType, Type foreignType)
        {
            var foreignRelationships = DataFunctions.GetRelatedProperties(foreignType);
            foreach (var fR in foreignRelationships)
            {
                if (fR.Item1.DeclaringType == foreignType)
                {
                    var localRelationships = DataFunctions.GetRelatedProperties(localType);
                    foreach (var lR in localRelationships)
                    {
                        if (lR.Item2.RelationshipName == fR.Item2.RelationshipName)
                        {
                            // we found a matching relationship
                            // TODO: Should also check type of relationship... eg, MANY_TO_ONE should match to ONE_TO_MANY, etc
                            return new TypeRelationship()
                            {
                                LocalProperty = lR.Item1,
                                LocalRelationship = lR.Item2,
                                ForeignProperty = fR.Item1,
                                ForeignRelationship = fR.Item2
                            };
                        }
                    }
                }
            }

            throw new Exception("Cannot find data relationship.");
        }

        internal static List<PropertyInfo> GetRelationshipDataLists(Type objType)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();
            foreach (PropertyInfo prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                //SqlRelationshipLookup sField = GetPropertyRelationshipLookup(prop);
                //if (sField != null)
                //{
                //    properties.Add(prop);
                //}
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(RelatedDataList<>))
                {
                    properties.Add(prop);
                }
            }
            return properties;
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

        internal static SqlRelatedTable GetPropertySqlRelatedTable(PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes(true);
            if (attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.GetType().Name == "SqlRelatedTable")
                    {
                        return attribute as SqlRelatedTable;
                    }
                }
            }
            return null;
        }

        /*internal static SqlRelationshipLookup GetPropertyRelationshipLookup(PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes(true);
            if (attributes.Length > 0)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute.GetType().Name == "SqlRelationshipLookup")
                    {
                        return attribute as SqlRelationshipLookup;
                    }
                }
            }
            return null;
        }*/

        private static void SetCsvConversion(PropertyInfo prop, object obj, object value)
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
                            prop.SetValue(obj, new List<string>(value.ToString().Split(',')));
                            return;
                        }
                    }
                }
            }

            prop.SetValue(obj, value);
        }

        private static object GetCsvConversion(PropertyInfo prop, object obj)
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
        }

        public static T GetRecord<T>(object id) where T : DataItem, new()
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());

            string tableName = GetTableName<T>(true);

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

        private static List<string> GetDataFields<T>() where T : DataItem
        {
            List<string> fields = new List<string>();
            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                SqlFieldName sField = GetPropertySqlFieldName(prop);
                if (sField != null)
                {
                    fields.Add(sField.Name);
                }
            }
            return fields;
        }

        public static T FromDataRow<T>(DataRow dRow) where T : DataItem, new()
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
                                    SetCsvConversion(prop, obj, dRow[sField.Name]);
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
            obj.DataFilled();
            return obj;
        }

        private static object FromDataRow(Type ofType, DataRow dRow)
        {
            DataItem obj = Activator.CreateInstance(ofType) as DataItem;
            obj._newRecord = false;

            foreach (PropertyInfo prop in ofType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
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
            obj.DataFilled();
            return obj;
        }

        /*public static List<T> GetData<T>(int numRecords, int pageNumber, string whereClause = "", string overrideTableName = "", List<SqlParameter> sqlParameters = null, string orderBy = "ORDER BY id")
        {
            JsonSerializerSettings s = new JsonSerializerSettings();
            s.ContractResolver = new SqlDataTransferContractResolver();
            string jsonStr = GetDataJSON<T>(numRecords, pageNumber, whereClause, overrideTableName, sqlParameters, orderBy);
            return JsonConvert.DeserializeObject<List<T>>(jsonStr, s);
        }*/

        public static List<T> GetDataSP<T>(string storedProcedure, List<SqlParameter> parameters, int tableIndex = 0) where T : DataItem, new()
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlDataAdapter dataAdapter;

            SqlCommand sCommand = new SqlCommand(storedProcedure, dataConn);
            sCommand.CommandType = CommandType.StoredProcedure;

            foreach (SqlParameter param in parameters)
            {
                sCommand.Parameters.Add(param);
            }

            dataAdapter = new SqlDataAdapter(sCommand);

            DataSet ds = new DataSet();
            dataAdapter.Fill(ds);

            List<T> lItems = new List<T>();
            foreach (DataRow dRow in ds.Tables[tableIndex].Rows)
            {
                lItems.Add(FromDataRow<T>(dRow));
            }

            return lItems;
        }

        public static List<T> GetDataCmd<T>(string sqlCommand, List<SqlParameter> sqlParameters = null) where T : DataItem, new()
        {
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            SqlDataAdapter dataAdapter = new SqlDataAdapter(sqlCommand, dataConn);

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
        }

        public static List<T> GetData<T>(int numRecords, int pageNumber, string whereClause = "", string overrideTableName = "", List<SqlParameter> sqlParameters = null, string orderBy = "ORDER BY ??", bool distinct = false, Dictionary<string, string> selectOverrides = null) where T : DataItem, new()
        {
            if (selectOverrides == null)
            {
                selectOverrides = new Dictionary<string, string>();
            }

            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            string tableName = "";

            if (overrideTableName != "")
            {
                tableName = overrideTableName;
            }
            else
            {
                tableName = GetTableName<T>(true);

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

            string selectDirective;
            if (distinct)
            {
                selectDirective = "SELECT DISTINCT ";
            }
            else
            {
                selectDirective = "SELECT ";
            }

            string fields = "";
            foreach (string fieldName in GetDataFields<T>())
            {
                if (selectOverrides.ContainsKey(fieldName))
                {
                    if (!string.IsNullOrEmpty(selectOverrides[fieldName]))
                    {
                        fields += selectOverrides[fieldName] + ", ";
                    }
                }
                else
                {
                    fields += tableName + "." + fieldName + ", ";
                }
            }

            if (fields.Length > 1)
            {
                fields = fields.Substring(0, fields.Length - 2);
            }
            else
            {
                fields = "*";
            }

            if (numRecords == 0)
            {
                dataAdapter = new SqlDataAdapter(selectDirective + fields + " FROM " + tableName + whereClause + " " + orderBy, dataConn);
            }
            else if (pageNumber > 0)
            {
                dataAdapter = new SqlDataAdapter(selectDirective + fields + " FROM " + tableName + whereClause + " " + orderBy + " OFFSET " + (numRecords * pageNumber) + " ROWS FETCH NEXT " + numRecords + " ROWS ONLY", dataConn);
            }
            else
            {
                dataAdapter = new SqlDataAdapter(selectDirective + "TOP (" + numRecords + ") " + fields + " FROM " + tableName + whereClause + " " + orderBy, dataConn);
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

        public static object ConvertList(List<object> value, Type type)
        {
            var containedType = type.GenericTypeArguments.First();
            var list = (IList)Activator.CreateInstance(type);
            var cList = value.Select(item => Convert.ChangeType(item, containedType)).ToList();
            for (int i = 0; i < cList.Count; i++)
            {
                list.Add(cList[i]);
            }
            return list;
        }

        public static List<object> GetData(Type ofType, int numRecords, int pageNumber, string whereClause = "", string overrideTableName = "", List<SqlParameter> sqlParameters = null, string orderBy = "ORDER BY ??")
        {
            object tempObj = Activator.CreateInstance(ofType);
            SqlConnection dataConn = new SqlConnection(GetConnectionString());
            string tableName = "";

            if (overrideTableName != "")
            {
                tableName = overrideTableName;
            }
            else
            {
                tableName = GetTableName(tempObj, true);

                if (tableName == "")
                {
                    throw new Exception("No SqlTableName attribute is set for object '" + ofType.Name + "'.");
                }
            }

            if (whereClause != "")
            {
                whereClause = " " + whereClause;
            }

            if (orderBy == "ORDER BY ??")
            {
                // we need to find the primary key field
                PropertyInfo pInfo = GetPrimaryKey(tempObj);

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

            List<object> lItems = new List<object>();
            foreach (DataRow dRow in dt.Rows)
            {
                lItems.Add(FromDataRow(ofType, dRow));
            }

            return lItems;
            //JsonSerializerSettings s = new JsonSerializerSettings();
            //s.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            //return JsonConvert.SerializeObject(dt, s);
        }

        public static void UpdateData(object obj)
        {
            string tableName = GetTableName(obj, false);
            List<string> fieldNames = new List<string>();
            List<object> values = new List<object>();
            string whereStr = "";
            SqlParameter whereValue = null;
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
                            whereStr = " WHERE " + sField.Name + " = @insert_id__";
                            whereValue = new SqlParameter("@insert_id__", prop.GetValue(obj));
                        }
                        else if (sField.isReadOnly == false)
                        {
                            fieldNames.Add(sField.Name);
                            values.Add(GetCsvConversion(prop, obj));
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
                        bool skipValue = false;
                        if (values[i] is DateTime)
                        {
                            // nulls get converted to DateTime.MinValue when reading, which then isn't compatible with
                            // SQL when putting back - so don't update this field.
                            DateTime dTime = (DateTime)values[i];
                            if (dTime == DateTime.MinValue)
                            {
                                skipValue = true;
                            }
                        }
                        if (!skipValue)
                        {
                            hasValues = true;
                            valueStr += fieldNames[i] + " = @" + fieldNames[i] + ", ";
                            cmd.Parameters.Add(new SqlParameter("@" + fieldNames[i], values[i]));
                        }
                    }
                }

                if (hasValues)
                {
                    if (whereValue != null)
                    {
                        cmd.Parameters.Add(whereValue);
                    }
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
            string tableName = GetTableName(obj, false);
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
                            values.Add(GetCsvConversion(prop, obj));
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
                    fieldStr = "(" + fieldStr.Substring(0, fieldStr.Length - 2) + ")";
                    valueStr = "(" + valueStr.Substring(0, valueStr.Length - 2) + ")";
                    cmd.CommandText += fieldStr + " VALUES " + valueStr + "; SELECT @@IDENTITY;";

                    dataConn.Open();
                    object retVal;
                    try
                    {
                        retVal = cmd.ExecuteScalar();
                    }
                    catch (Exception ex)
                    {
                        Exception outEx = new Exception("Error executing statement: " + cmd.CommandText, ex);
                        throw outEx;
                    }
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
                            long outId = Convert.ToInt64(retVal);
                            autoNumberProperty.SetValue(obj, Convert.ChangeType(outId, autoNumberProperty.PropertyType));
                            return outId;
                        }
                    }
                }
            }

            return -1;
        }

        public static void DeleteData(object obj)
        {
            string tableName = GetTableName(obj, false);
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
                            values.Add(GetCsvConversion(prop, obj));
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
                if (p.CanWrite)
                {
                    p.SetValue(toObj, p.GetValue(fromObj));
                }
            }

            foreach (FieldInfo f in fromObj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                f.SetValue(toObj, f.GetValue(fromObj));
            }
        }
    }
}