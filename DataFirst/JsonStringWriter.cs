using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace BarnardTech.DataFirst
{
    public class JsonStringWriter : JsonWriter
    {
        public string Output;

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override void WriteValue(object value)
        {
            Output += value.ToString();
        }

        public override void WriteValue(string value)
        {
            Output += value;
        }
    }
}
