using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace BarnardTech.DataFirst
{
    public class CsvConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<string>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }
            return new List<string>(reader.Value.ToString().Split(','));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(string.Join(",", (List<string>)value));
        }
    }
}
