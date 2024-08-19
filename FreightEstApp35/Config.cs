using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace FreightEstApp35
{
    public static class Config
    {
        // Toggleing between Prod and Test is based on host machine name.
        static string HOST_PRODUCTION_WEB = "AZWEB10"; // If this is the host name it will run production settings.
        static string _ENVIRONMENT = String.Empty;
        
        static string _RemoteServerName = "SANDBOX";
        static string _LogFile = "FreightEstApp35.log";

        static string _PROD_ConnString = "Data Source=192.168.4.50;Initial Catalog=UpsRate;uid=sa;pwd=95Montana!;Connection Timeout=300;"; // PRODUCTION
        static string _TEST_ConnString = "Server=azuredb01\\azuredb01;Initial Catalog=UpsRate;uid=sa;pwd=95Montana!!!;"; // TEST

        static string _PROD_SQLProviderAbbriviations = "SELECT @Abbrev = FreightAbbreviation FROM " + Config.RemoteServerName + ".CostPlus.dbo.FreightProviderAbbreviations WHERE FreightProvider = @FreightProvider";
        static string _TEST_SQLProviderAbbriviations = "SELECT @Abbrev = FreightAbbreviation FROM SUWDB03.UPSRate.dbo.FreightProviderAbbreviations WHERE FreightProvider = @FreightProvider";

        // UPS PRODUCTION ENDPOINTS
        static string _PROD_UPSAuthorizationURL = "https://onlinetools.ups.com/security/v1/oauth/authorize"; // PRODUCTION
        static string _PROD_UPSGenerateTokenURL = "https://onlinetools.ups.com/security/v1/oauth/token"; // PRODUCTION
        static string _PROD_UPSAddressValidationURL = "https://onlinetools.ups.com/api/addressvalidation/v1/3"; //  PRODUCTION  {version}/{requestOption}
        static string _PROD_UPSShopRatesURL = "https://onlinetools.ups.com/api/rating/v2403/"; // PRODUCTION {version}/{requestoption}        

        // UPS TEST ENDPOINTS
        static string _TEST_UPSAuthorizationURL = "https://wwwcie.ups.com/security/v1/oauth/authorize"; // TEST
        static string _TEST_UPSGenerateTokenURL = "https://wwwcie.ups.com/security/v1/oauth/token"; // TEST
        static string _TEST_UPSAddressValidationURL = "https://wwwcie.ups.com/api/addressvalidation/v1/3"; // TEST  {version}/{requestOption}
        static string _TEST_UPSShopRatesURL = "https://wwwcie.ups.com/api/rating/v2403/"; // TEST {version}/{requestoption}        

        static string _UPSAccessKey = "CC83ED82D080DC80";
        static string _UPSUserName = "WiseWebSupport";
        static string _UPSPassword = "Wise_forms";
        static string _UPSClientId = "SzWbVRiGAPPbC0NqV9GGXdE8kUE0EnexGrxsl94sj0HGTdAX";
        static string _UPSClientSecret = "zcnbBCf3qPGLleJv1aBqOH8SbAbFssLoE1vAAUGbrnXK2GJAQJUTAskarDv70Ddw";

        static string _SQLProviderAbbriviations = String.Empty;
        static string _ConnString = String.Empty;
        static string _UPSAuthorizationURL = String.Empty;
        static string _UPSGenerateTokenURL = String.Empty;
        static string _UPSAddressValidationURL = String.Empty;
        static string _UPSShopRatesURL = String.Empty;
        static string _UPSShipFromName = "Wise Alpharetta";
        static string _UPSShipFromAddress = "1000 Union Center Drive";
        static string _UPSShipFromCity = "Alpharetta";
        static string _UPSShipFromState = "GA";
        static string _UPSShipFromZip = "30004";

        #region Plant shipping information
        static string _ShipFromShipperNumber = "391287";

        static string _NetworkDomain = "WISENT";

        //static string[] _PlantCodes = { "ALP", "BUT", "FTW", "PDT", "POR" };
        static string[] _PlantCodes = { "ALP", "BUT", "FTW", "POR" };

        static double _MinCWTWeightGround = 200;
        static double _MinCWTPackagesGround = 2;

        static double _MinCWTWeightAir = 100;
        static double _MinCWTPackagesAir = 2;

        static Dictionary<string, string> _PlantNames = new Dictionary<string, string>();
        private static void populatePlantNames()
        {
            _PlantNames.Add("ALP", "Alpharetta");
            _PlantNames.Add("BUT", "Butler");
            _PlantNames.Add("FTW", "Ft Wayne");
            //_PlantNames.Add("PDT", "Piedmont");
            _PlantNames.Add("POR", "Portland");
        }
        #endregion

        #region LTL Endpoints

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
        #endregion
        
        static Config() {
            populatePlantNames();
        }

        /// <summary>
        /// Manages the DEBUG / PRODUCTION toggle based on host server name.
        /// If the host name is unknown it will default to DEBUG.
        /// </summary>
        public static void SetProdDebug()
        {
            string hostServerName = Environment.MachineName;
            if(hostServerName == HOST_PRODUCTION_WEB)
            {
                _ENVIRONMENT = "PRODUCTION";
                _UPSAuthorizationURL = _PROD_UPSAuthorizationURL;
                _UPSGenerateTokenURL = _PROD_UPSGenerateTokenURL;
                _UPSAddressValidationURL = _PROD_UPSAddressValidationURL;
                _UPSShopRatesURL = _PROD_UPSShopRatesURL;
                _ConnString = _PROD_ConnString;
                _SQLProviderAbbriviations = _PROD_SQLProviderAbbriviations;
            }
            else
            {
                _ENVIRONMENT = "DEBUG";
                _UPSAuthorizationURL = _TEST_UPSAuthorizationURL;
                _UPSGenerateTokenURL = _TEST_UPSGenerateTokenURL;
                _UPSAddressValidationURL = _TEST_UPSAddressValidationURL;
                _UPSShopRatesURL = _TEST_UPSShopRatesURL;
                _ConnString = _TEST_ConnString;
                _SQLProviderAbbriviations = _TEST_SQLProviderAbbriviations;
            }
        }


        public static string ENVIRONMENT
        {
            get
            {
                return _ENVIRONMENT;
            }
        }
        public static string ConnString
        {
            get { return _ConnString; }
        }
        public static string RemoteServerName
        {
            get { return _RemoteServerName; }
        }
        public static string SQLProviderAbbriviations
        {
            get { return _SQLProviderAbbriviations; }
        }

        #region UPS 
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
        public static string UPSClientId
        {
            get { return _UPSClientId; }
        }
        public static string UPSClientSecret
        {
            get { return _UPSClientSecret; }
        }
        public static string UPSAuthorizationURL
        {
            get { return _UPSAuthorizationURL; }
        }
        public static string UPSGenerateTokenURL
        {
            get { return _UPSGenerateTokenURL; }
        }
        public static string UPSAddressValidationURL
        {
            get { return _UPSAddressValidationURL; }
        }
        public static string UPSShopRatesURL
        {
            get { return _UPSShopRatesURL; }
        }
        public static string UPSShipFromName
        {
            get { return _UPSShipFromName; }
        }
        public static string UPSShipFromAddress
        {
            get { return _UPSShipFromAddress; }
        }
        public static string UPSShipFromCity
        {
            get { return _UPSShipFromCity; }
        }
        public static string UPSShipFromState
        {
            get { return _UPSShipFromState; }
        }
        public static string UPSShipFromZip
        {
            get { return _UPSShipFromZip; }
        }
        #endregion

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
