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

        /*
        private Dictionary<PropertyInfo, object> _storedValues = new Dictionary<PropertyInfo, object>();

        private void getStoredValues()
        {
            PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                _storedValues.Add(property, property.GetValue(this));
            }
        }

        private bool hasChanges()
        {
            // Get all properties
            PropertyInfo[] tempProperties = this.GetType().GetProperties().ToArray();

            // Filter properties by only getting what has changed
            PropertyInfo[] properties = tempProperties.Where(p => !Equals(p.GetValue(this), _storedValues[p])).ToArray();

            return properties.Length > 0;
        }
        */

        public virtual void DataFilled() { }

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

            // look for relationships
            var relationships = DataFunctions.GetRelationshipDataLists(this.GetType());
            foreach (var r in relationships)
            {
                DataFunctions.TypeRelationship tR = DataFunctions.GetRelationship(r.PropertyType, this.GetType(), r.PropertyType.GetGenericArguments()[0]);

                if (tR.ForeignRelationship.Relationship == SqlRelatedTable.DataRelationship.MANY_TO_ONE && tR.LocalRelationship.Relationship == SqlRelatedTable.DataRelationship.ONE_TO_MANY)
                {
                    // we found the related property
                    SqlFieldName fieldName = DataFunctions.GetPropertySqlFieldName(tR.ForeignProperty);

                    // construct our generic type
                    List<object> retList = DataFunctions.GetData(r.PropertyType.GetGenericArguments()[0],
                        0,
                        0,
                        "WHERE " + fieldName.Name + " = @fieldid", "",
                        new List<SqlParameter>()
                        {
                                                    new SqlParameter("@fieldid", tR.LocalProperty.GetValue(this))
                        });

                    var converted = DataFunctions.ConvertList(retList, r.PropertyType);
                    r.SetValue(this, converted);
                    break;
                }
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

            foreach (var dataListProperty in DataFunctions.GetRelationshipDataLists(this.GetType()))
            {
                dynamic dataList = dataListProperty.GetValue(this);
                if (dataList != null)
                {
                    dataList.Save(this);
                }
            }

            //getStoredValues();
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

        public static List<T> GetItems<T>(string whereClause = "", List<SqlParameter> sqlParameters = null, string orderBy = "ORDER BY ??", bool distinct = false) where T : DataItem, new()
        {
            return GetItems<T>(0, 0, whereClause, sqlParameters, orderBy, "", distinct);
        }

        public static List<T> GetItems<T>(int numRecords, int pageNumber, string whereClause = "", List<SqlParameter> sqlParameters = null, string orderBy = "ORDER BY ??", string overrideTablename = "", bool distinct = false, Dictionary<string, string> selectOverrides = null) where T : DataItem, new()
        {
            return DataFunctions.GetData<T>(numRecords, pageNumber, whereClause, overrideTablename, sqlParameters, orderBy, distinct, selectOverrides);
        }

        /// <summary>
        /// Resets the _newRecord marker, so that DataFirst sees this item as a new record that hasn't yet been saved.
        /// After resetting, calling Save() on this DataItem will initiate an INSERT instead of an UPDATE.
        /// </summary>
        public void ResetNewRecordMarker()
        {
            _newRecord = false;
        }
    }
}