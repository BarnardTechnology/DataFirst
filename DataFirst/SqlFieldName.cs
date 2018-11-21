using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarnardTech.DataFirst
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlFieldName : Attribute
    {
        public string Name;
        public bool isPrimaryKey;
        public bool isAutoNumber;
        public bool isReadOnly;

        public SqlFieldName(string fieldName, bool primaryKey = false, bool autoNumber = false, bool dbReadOnly = false)
        {
            Name = fieldName;
            isPrimaryKey = primaryKey;
            isAutoNumber = autoNumber;
            isReadOnly = dbReadOnly;
        }
    }
}