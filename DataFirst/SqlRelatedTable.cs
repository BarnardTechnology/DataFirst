using System;
using System.Collections.Generic;
using System.Text;

namespace BarnardTech.DataFirst
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SqlRelatedTable : Attribute
    {
        public enum DataRelationship
        {
            ONE_TO_MANY, ONE_TO_ONE, MANY_TO_ONE
        }

        public string RelationshipName;
        public string TableName;
        public Type RelatedClass;
        public DataRelationship Relationship;

        public SqlRelatedTable(string relationshipName, Type relatedClass, DataRelationship relationship = DataRelationship.ONE_TO_MANY)
        {
            RelationshipName = relationshipName;
            RelatedClass = relatedClass;
            Relationship = relationship;
        }
    }
}
