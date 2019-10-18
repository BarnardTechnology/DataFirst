using System;
using System.Collections.Generic;
using System.Text;

namespace BarnardTech.DataFirst
{
    public class RelatedDataList<T> : List<T>
        where T : DataItem
    {
        /*
        [Obsolete("Do not use the standard 'Add' method with a RelatedDataList, as it will not auto-populate the item's relationship field.", true)]
        public new void Add(T item)
        {
            base.Add(item);
        }
        */

        public void AddAndSave(T item, DataItem parent)
        {
            var relationship = DataFunctions.GetRelationship(this, parent.GetType(), typeof(T));
            relationship.ForeignProperty.SetValue(item, relationship.LocalProperty.GetValue(parent));
            item.Save();
            base.Add(item);
        }

        public void Save(DataItem parent)
        {
            // TODO: Currently this will save all rows, which is a bit wasteful. DataItem should
            //       be capable of determining if a save is necessary, and only save when required.
            var relationship = DataFunctions.GetRelationship(this, parent.GetType(), typeof(T));
            foreach (T item in this)
            {
                relationship.ForeignProperty.SetValue(item, relationship.LocalProperty.GetValue(parent));
                item.Save();
            }
        }
    }
}
