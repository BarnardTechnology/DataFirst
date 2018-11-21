using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarnardTech.DataFirst
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SqlTableName : Attribute
    {
        public string Name;

        public SqlTableName(string tableName)
        {
            Name = tableName;
        }
    }
}