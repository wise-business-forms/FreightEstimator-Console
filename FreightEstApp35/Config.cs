using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace FreightEstApp35
{
    public static class Config
    {
        [Obsolete]
        static string _RemoteServerName = ConfigurationManager.AppSettings.Get("RemoteServerName");
        static string _LogFile = "FreightEstApp35.log";

        #region Environment specific settings

        static string _PROD_SQLProviderAbbriviations = "SELECT FreightAbbreviation FROM " + Config.RemoteServerName + ".CostPlus.dbo.FreightProviderAbbreviations";

        // UPS endpoints. UPS has no usable sandbox for the REST Rating API, so Test Calls use these too (ADR 0002).
        static string _UPSAuthorizationURL = "https://onlinetools.ups.com/security/v1/oauth/authorize";
        static string _UPSGenerateTokenURL = "https://onlinetools.ups.com/security/v1/oauth/token";
        static string _UPSAddressValidationURL = "https://onlinetools.ups.com/api/addressvalidation/v1/3"; // {version}/{requestOption}
        static string _UPSShopRatesURL = "https://onlinetools.ups.com/api/rating/v2403/"; // {version}/{requestoption}

        // UPS certification (non-production) endpoints, unusable for the REST Rating API (ADR 0002)
        //static string _TEST_UPSAuthorizationURL = "https://wwwcie.ups.com/security/v1/oauth/authorize"; // TEST
        //static string _TEST_UPSGenerateTokenURL = "https://wwwcie.ups.com/security/v1/oauth/token"; // TEST
        //static string _TEST_UPSAddressValidationURL = "https://wwwcie.ups.com/api/addressvalidation/v1/3"; // TEST  {version}/{requestOption}
        //static string _TEST_UPSShopRatesURL = "https://wwwcie.ups.com/api/rating/v2403/"; // TEST {version}/{requestoption}        
        #endregion

        static string _UPSAccessKey = ConfigurationManager.AppSettings.Get("UPS_AccessKey");
        static string _UPSUserName = ConfigurationManager.AppSettings.Get("UPS_UserName");
        static string _UPSPassword = ConfigurationManager.AppSettings.Get("UPS_Password");
        static string _UPSClientId = ConfigurationManager.AppSettings.Get("UPS_ClientId");
        static string _UPSClientSecret = ConfigurationManager.AppSettings.Get("UPS_ClientSecret");

        static string _SQLProviderAbbriviations = String.Empty;
        static string _ConnString = String.Empty;
        static EnvironmentResolution _Resolution;
        static string _UPSShipFromName = "Wise Alpharetta";
        static string _UPSShipFromAddress = "1000 Union Center Drive";
        static string _UPSShipFromCity = "Alpharetta";
        static string _UPSShipFromState = "GA";
        static string _UPSShipFromZip = "30004";

        static double _MinCWTWeightGround = 200;
        static double _MinCWTPackagesGround = 2;

        static double _MinCWTWeightAir = 100;
        static double _MinCWTPackagesAir = 2;

        #region Plant shipping information
        static string _ShipFromShipperNumber = "391287";

        static string _NetworkDomain = "WISENT";

        //static string[] _PlantCodes = { "ALP", "BUT", "FTW", "PDT", "POR" };
        static string[] _PlantCodes = { "ALP", "BUT", "FTW", "POR" };

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

        static string _M33DemoToken = ConfigurationManager.AppSettings.Get("M33_DemoToken");
        static string _M33ProdToken = ConfigurationManager.AppSettings.Get("M33_ProdToken");

        static bool _M33DemoMode = false;

        static string _TransPlaceDemoUrl = "https://uattms.transplace.com/xml-api/api/";
        static string _TransPlaceProdUrl = "https://tms.transplace.com/xml-api/api/";

        static string _TransPlaceDemoToken = ConfigurationManager.AppSettings.Get("TransPlace_DemoToken");
        static string _TransPlaceProdToken = ConfigurationManager.AppSettings.Get("TransPlace_ProdToken");

        // PRODUCTION
        static string _TransportationInsightDemoUrl = "https://t-insightws.mercurygate.net/MercuryGate/common/remoteService.jsp";
        static string _TransportationInsightProdUrl = "https://t-insightws.mercurygate.net/MercuryGate/common/remoteService.jsp";
        static string _TransprtationInsightApiUsername = ConfigurationManager.AppSettings.Get("TI_ApiUsername");
        static string _TransprtationInsightApiPassword = ConfigurationManager.AppSettings.Get("TI_ApiPassword");

        // DEFVELOPMENT - restore these (and use them from the demo/test path) once WBF has real
        // TransportationInsight test credentials; today _TransportationInsightDemoUrl above is
        // identical to the prod URL, so a Test Call authenticates with prod credentials regardless.
        //static string _TransportationInsightDemoUrl = "https://t-insightws.mercurygate.net/MercuryGate/common/remoteService.jsp";
        #endregion

        static Config() {
            populatePlantNames();
        }

        /// <summary>
        /// Classifies this run as a Production Host or Test Mode from the machine name and the configured
        /// ProductionHosts list, then sets the connection string and request-queue query. If the configured
        /// servers do not fit the mode, prints why and exits before any connection is opened.
        /// </summary>
        public static void ResolveEnvironment()
        {
            _Resolution = EnvironmentEvaluator.Evaluate(
                Environment.MachineName,
                EnvironmentEvaluator.ParseHostList(ConfigurationManager.AppSettings.Get("ProductionHosts")),
                ConfigurationManager.AppSettings.Get("PROD_Server"),
                _RemoteServerName);

            if (_Resolution.IsRefused)
            {
                Console.Error.WriteLine("REFUSING TO START: " + _Resolution.Refusal);
                Environment.Exit(2);
            }

            IReadOnlyList<string> missingSecrets = SecretSettings.FindMissing(ConfigurationManager.AppSettings.Get, _Resolution.Mode);
            if (missingSecrets.Count > 0)
            {
                Console.Error.WriteLine("REFUSING TO START: missing required secret setting(s): " + string.Join(", ", missingSecrets)
                    + ". Set them in secrets.config beside the exe or in the deployed config (see secrets.config.example).");
                Environment.Exit(2);
            }

            _ConnString = ConnectionStrings.Build(
                _Resolution.Mode,
                ConfigurationManager.AppSettings.Get("PROD_Server"),
                ConfigurationManager.AppSettings.Get("PROD_User"),
                ConfigurationManager.AppSettings.Get("PROD_Password"));
            _SQLProviderAbbriviations = _PROD_SQLProviderAbbriviations;
        }

        public static EnvironmentResolution Resolution
        {
            get { return _Resolution; }
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


        // Raw prod/demo values - which one to use for a given call is a per-request decision
        // (see CarrierTestRouting), not a static app-wide toggle.
        // Url and Token always travel together for a given environment, so they're bundled
        // into one value rather than four separate properties a caller has to re-pair.
        public static (string Url, string Token) TransPlaceProdEndpoint { get { return (_TransPlaceProdUrl, _TransPlaceProdToken); } }
        public static (string Url, string Token) TransPlaceDemoEndpoint { get { return (_TransPlaceDemoUrl, _TransPlaceDemoToken); } }
        public static string TransportationInsightProdUrl { get { return _TransportationInsightProdUrl; } }
        public static string TransportationInsightDemoUrl { get { return _TransportationInsightDemoUrl; } }

        public static string TransprtationInsightApiUsername
        {
            get
            {
                return _TransprtationInsightApiUsername;
            }
        }

        public static string TransprtationInsightApiPassword
        {
            get
            {
                return _TransprtationInsightApiPassword;
            }
        }
    }
}
