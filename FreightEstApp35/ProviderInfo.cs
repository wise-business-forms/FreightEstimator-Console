using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace FreightEstApp35
{
    public static class ProviderInfo
    {
        public static Dictionary<string, string> freightAbbreviations = new Dictionary<string, string>();
        public static Dictionary<string, string> freightDescriptions = new Dictionary<string, string>();

        static ProviderInfo()
        {
            WiseTools.logToFile(Config.logFile, "Initializing ProviderInfo", true);
            populateDictionaries();
        }

        private static void populateDictionaries()
        {
            WiseTools.logToFile(Config.logFile, "Starting populateDictionaries", true);

            freightAbbreviations.Clear();
            freightDescriptions.Clear();

            DBUtil db = new DBUtil();
            DataSet ds = db.getFreightProviderInfo();

            if (ds.Tables.Count > 0)
            {
                foreach(DataRow dr in ds.Tables[0].Rows) {
                    freightAbbreviations.Add(dr["FreightProvider"].ToString(), dr["FreightAbbreviation"].ToString());
                    freightDescriptions.Add(dr["FreightProvider"].ToString(), dr["FreightDesc"].ToString());
                }
            }

            WiseTools.logToFile(Config.logFile, "Finished populateDictionaries", true);
            
        }
    }
}
