using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLEventsExecutor
{
    public static class Connections
    {
        public static Dictionary<string, string> ConnectionDict = new Dictionary<string, string>()
        {
            {"SQLDefault", @"Target DB connection string"},
            {"CSVDefault", @"Path to directory with prepared *.CSV files"}
        };
    }
}
