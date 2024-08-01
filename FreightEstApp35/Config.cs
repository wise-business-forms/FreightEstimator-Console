using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace FreightEstApp35
{
    public static class Config
    {
        //static string _ConnString = "Data Source=192.168.4.50;Initial Catalog=UpsRate;uid=sa;pwd=95Montana!;Connection Timeout=300;"; // PRODUCTION
        static string _ConnString = "Server=azuredb01\\azuredb01;Initial Catalog=UpsRate;uid=sa;pwd=95Montana!!!;"; // TEST

        static string _RemoteServerName = ConfigurationSettings.AppSettings.Get("RemoteServerName");
        static string _LogFile = ConfigurationSettings.AppSettings.Get("LogFileName");

        static string _UPSAccessKey = "CC83ED82D080DC80";
        static string _UPSUserName = "WiseWebSupport";
        static string _UPSPassword = "Wise_forms";

        static string _ShipFromShipperNumber = "391287";

        static string _NetworkDomain = "WISENT";

        //static string[] _PlantCodes = { "ALP", "BUT", "FTW", "PDT", "POR" };
        static string[] _PlantCodes = { "ALP", "BUT", "FTW", "POR" };

        static double _MinCWTWeightGround = 200;
        static double _MinCWTPackagesGround = 2;

        static double _MinCWTWeightAir = 100;
        static double _MinCWTPackagesAir = 2;

        static Dictionary<string, string> _PlantNames = new Dictionary<string, string>();

        static string _M33DemoUrl = "http://demo.m33integrated.com/api/";
        static string _M33ProdUrl = "https://blackbeardapp.com/api/";

        static string _M33DemoToken = "696c42d819642885724b60ffcb7d636deadd632e";
        static string _M33ProdToken = "d579620ffba756e5c2ec9f76e3447f98bf85770b";

        static bool _M33DemoMode = false;

        static string _TransPlaceDemoUrl = "https://uattms.transplace.com/xml-api/api/";
        static string _TransPlaceProdUrl = "https://tms.transplace.com/xml-api/api/";

        static string _TransPlaceDemoToken = "8Hhk8etqxs94gEtj0JpVxl90qoINDfMAiemh84XbGNM%3D";
        //static string _TransPlaceProdToken = "jLDnfupyBDGy%2FnF2zYs4g5Zl0%2B7g7TBLPIwBuNfAVhc%3D";
        static string _TransPlaceProdToken = "jLDnfupyBDGy%2FnF2zYs4gxMB78GTlIAytU2dzN4xQLdVWNGGmkNOl%2B9N%2BU6aO4vP";
        
        static bool _TransPlaceDemoMode = false;

        static Config() {
            populatePlantNames();
        }

        private static void populatePlantNames() {
            _PlantNames.Add("ALP", "Alpharetta");
            _PlantNames.Add("BUT", "Butler");
            _PlantNames.Add("FTW", "Ft Wayne");
            //_PlantNames.Add("PDT", "Piedmont");
            _PlantNames.Add("POR", "Portland");
        }

        public static string ConnString
        {
            get { return _ConnString; }
        }
        public static string RemoteServerName
        {
            get { return _RemoteServerName; }
        }
        public static string UPSAccessKey
        {
            get { return _UPSAccessKey; }
        }
        public static string UPSUserName
        {
            get { return _UPSUserName; }
        }
        public static string UPSPassword
        {
            get { return _UPSPassword; }
        }
        public static string ShipFromShipperNumber
        {
            get { return _ShipFromShipperNumber; }
        }
        public static string NetworkDomain
        {
            get { return _NetworkDomain; }
        }
        public static string[] PlantCodes
        {
            get { return _PlantCodes; }
        }
        public static double MinCWTWeightGround
        {
            get { return _MinCWTWeightGround; }
        }
        public static double MinCWTPackagesGround
        {
            get { return _MinCWTPackagesGround; }
        }
        public static double MinCWTWeightAir
        {
            get { return _MinCWTWeightAir; }
        }
        public static double MinCWTPackagesAir
        {
            get { return _MinCWTPackagesAir; }
        }
        public static Dictionary<string, string> PlantNames
        {
            get { return _PlantNames; }
        }
        public static string M33Url
        {
            get 
            {
                if (_M33DemoMode)
                {
                    return _M33DemoUrl;
                }
                else
                {
                    return _M33ProdUrl;
                }
            }
        }
        public static string M33Token
        {
            get
            {
                if (_M33DemoMode)
                {
                    return _M33DemoToken;
                }
                else
                {
                    return _M33ProdToken;
                }
            }
        }
        public static string logFile
        {
            get { return _LogFile; }
        }


        public static string TransPlaceUrl
        {
            get
            {
                if (_TransPlaceDemoMode)
                {
                    return _TransPlaceDemoUrl;
                }
                else
                {
                    return _TransPlaceProdUrl;
                }
            }
        }
        public static string TransPlaceToken
        {
            get
            {
                if (_TransPlaceDemoMode)
                {
                    return _TransPlaceDemoToken;
                }
                else
                {
                    return _TransPlaceProdToken;
                }
            }
        }

    }
}
