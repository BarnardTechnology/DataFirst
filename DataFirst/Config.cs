using System;
using System.Collections.Generic;
using System.Text;

namespace BarnardTech.DataFirst
{
    public static class Config
    {
        public static string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    throw new Exception("BarnardTech.DataFirst.Config.ConnectionString is not set.");
                }
                else
                {
                    return _connectionString;
                }
            }
            set
            {
                _connectionString = value;
            }
        }
        static string _connectionString = null;
    }
}
