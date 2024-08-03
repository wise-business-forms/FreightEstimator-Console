using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreightEstApp35
{
    static class WiseTools
    {
        static public string dbString(object toConvert)
        {
            string converted = "";
            if (toConvert != DBNull.Value)
            {
                converted = toConvert.ToString();
            }

            return (converted);
        }

        static public int dbInt(object toConvert)
        {
            int converted = 0;
            if (toConvert != DBNull.Value)
            {
                int.TryParse(toConvert.ToString(), out converted);
            }

            return (converted);
        }


        static public bool dbBool(object toConvert)
        {
            bool converted = false;
            if (toConvert != DBNull.Value)
            {
                bool.TryParse(toConvert.ToString(), out converted);
            }

            return (converted);
        }

        static public DateTime dbDateTime(object toConvert)
        {
            DateTime converted = DateTime.MinValue;
            if (toConvert != DBNull.Value)
            {
                DateTime.TryParse(toConvert.ToString(), out converted);
            }

            return (converted);
        }

        static public void logToFile(string fileName, string logData, bool prependTimestamp)
        {
            if (prependTimestamp)
            {
                logData = DateTime.Now.ToString() + " - " + logData;
            }
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + fileName, true))
            {
                file.WriteLine(logData);
            }
        }
    }
}
