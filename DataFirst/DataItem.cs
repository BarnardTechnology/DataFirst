using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarnardTech.DataFirst
{
    public class DataItem
    {
        /// <summary>
        /// Lets us know if this is a brand new record, or one loaded from the database. We
        /// need to know this when saving, to decide whether we're inserting or updating.
        /// </summary>
        internal bool _newRecord = true;

        public DataItem()
        {
            _newRecord = true;
        }

        public DataItem(object Id)
        {
            var property = DataFunctions.GetPrimaryKey(this);

            if (property != null)
            {
                SqlFieldName sField = DataFunctions.GetPropertySqlFieldName(property);
                //DataFunctions.GetRecord<>
                object item = typeof(DataFunctions)
                    .GetMethod("GetRecord")
                    .MakeGenericMethod(this.GetType())
                    .Invoke(null, new object[] { Id });

                if (item == null)
                {
                    throw new Exception("Item cannot be found.");
                }

                DataFunctions.ShallowCopy(item, this);

                this._newRecord = false;
            }
        }

        public void Save()
        {
            if (_newRecord)
            {
                DataFunctions.InsertData(this);
                _newRecord = false;
            }
            else
            {
                DataFunctions.UpdateData(this);
            }
        }

        public void Delete()
        {
            if (!_newRecord)
            {
                DataFunctions.DeleteData(this);
            }
        }

        public static T GetItem<T>(object id) where T : DataItem, new()
        {
            return DataFunctions.GetRecord<T>(id);
        }

        public static List<T> GetItems<T>(int numRecords, int pageNumber, string whereClause = "", List<SqlParameter> sqlParameters = null) where T : DataItem, new()
        {
            return DataFunctions.GetData<T>(numRecords, pageNumber, whereClause, "", sqlParameters);
        }
    }
}