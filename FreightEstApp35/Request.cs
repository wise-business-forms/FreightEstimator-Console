using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using static FreightEstApp35.RateCalculations;
using System.Text.RegularExpressions;
using System.Threading;

namespace FreightEstApp35
{
    class Request
    {
        public string source { get; set; }
        public string uniqueId { get; set; }
        public Address fromAddress { get; set; }
        public Address toAddress { get; set; }
        public string fromPlant { get; set; }
        public int numPackages { get; set; }
        public int pkgWeight { get; set; }
        public int lastPkgWeight { get; set; }
        public int signatureRequired { get; set; }
        public bool requestUPS { get; set; }
        public bool requestLTL { get; set; }
        public string freightClass { get; set; }
        public string pickupDate { get; set; }
        public bool notifyBeforeDelivery { get; set; }
        public bool liftgatePickup { get; set; }
        public bool liftgateDelivery { get; set; }
        public bool limitedAccessPickup { get; set; }
        public bool limitedAccessDelivery { get; set; }
        public bool residentialPickup { get; set; }
        public bool residentialDelivery { get; set; }
        public bool insidePickup { get; set; }
        public bool insideDelivery { get; set; }
        public bool sortAndSegregate { get; set; }
        public bool stopoffCharge { get; set; }
        public DateTime dateRequested { get; set; }
        public DateTime dateProcessed { get; set; }
        public bool isLoaded { get; set; }
        public bool isValid { get; set; }
        public string accessorials { get; set; }
        
        public int acctNumber { get; set; }

        public string shipWithArray { get; set; }

        /* Used for multi-job shipments (ship-withs) */
        public List<int> packageWeights { get; set; }

        Dictionary<string, string> replacements = new Dictionary<string, string>();

        public Request(bool getNext)
        {
            if (getNext)
            {
                if (loadNextRequest())
                {
                    isLoaded = true;

                    if (validateRequest())
                    {
                        isValid = true;
                    }
                    else
                    {
                        isValid = false;
                    }
                }
                else
                {
                   //No Request was loaded
                    isLoaded = false;
                }
            }
        }

        private bool validateRequest()
        {
            bool success = false;

            try
            {
                if(requestLTL && (toAddress.city == "" || toAddress.state == "" || toAddress.zip == ""))
                {
                    throwValidationError("LTL request require City, State, and Zip.");
                }
            }
            catch (Exception e)
            {

            }

            return (success);
        }

        private bool loadNextRequest()
        {
            bool success = false;
            //LoginId, QtyNumber, ToAddress, ToCity, ToState, ToZip, ToCountry, NumPackages, PkgWeight, LastPkgWeight
            try
            {
                DBUtil db = new DBUtil();
                DataSet ds = db.getNextRequestFromQueue();

                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow dr = ds.Tables[0].Rows[0];

                        source = WiseTools.dbString(dr["LoginId"]);
                        uniqueId = WiseTools.dbString(dr["QtyNumber"]);
                        
                        toAddress = new Address();
                        toAddress.street = WiseTools.dbString(dr["ToAddress"]);
                        toAddress.city = WiseTools.dbString(dr["ToCity"]);
                        toAddress.state = WiseTools.dbString(dr["ToState"]);
                        toAddress.zip = WiseTools.dbString(dr["ToZip"]);
                        toAddress.country = WiseTools.dbString(dr["ToCountry"]);
                        if (toAddress.country == "") { toAddress.country = "US"; }


                        if (toAddress.zip.IndexOf("-") < 0)
                        {
                            string substitutedZip = db.verifyAndCorrectZipCode(toAddress.city, toAddress.state, toAddress.zip);
                            if (substitutedZip != "" && substitutedZip != "BAD_INPUT" && substitutedZip != "NO_ALTERNATIVE")
                            {
                                toAddress.zip = substitutedZip;
                            }
                        }

                        //fromPlant = "ALP";// WiseTools.dbString(dr["FromPlant"]);

                        fromAddress = new Address();
                        fromAddress.street = WiseTools.dbString(dr["FromAddress"]);
                        fromAddress.city = WiseTools.dbString(dr["FromCity"]);
                        fromAddress.state = WiseTools.dbString(dr["FromState"]);
                        fromAddress.zip = WiseTools.dbString(dr["FromZip"]);
                        fromAddress.country = WiseTools.dbString(dr["FromCountry"]);
                        if (fromAddress.country == "") { fromAddress.country = "US"; }

                        switch(fromAddress.state) {
                            case "GA":
                                fromPlant = "ALP";
                                break;
                            case "PA":
                                fromPlant = "BUT";
                                break;
                            case "IN":
                                fromPlant = "FTW";
                                break;
                            case "SC":
                                fromPlant = "PDT";
                                break;
                            case "ME":
                                fromPlant = "POR";
                                break;
                        }

                        numPackages = WiseTools.dbInt(dr["NumPackages"]);
                        pkgWeight = WiseTools.dbInt(dr["PkgWeight"]);
                        lastPkgWeight = WiseTools.dbInt(dr["LastPkgWeight"]);


                        //requestUPS = true;
                        //requestLTL = false;
                        //freightClass = "55";
                        //pickupDate = "6/25/2014";
                        
                        //signatureRequired = WiseTools.dbBool(dr["SignatureRequired"]);
                        signatureRequired = 0;
                        
                        requestUPS = WiseTools.dbBool(dr["RequestUPS"]);
                        requestUPS = true;

                        if (numPackages > 200)
                        {
                            requestUPS = false;
                        }
                        requestLTL = WiseTools.dbBool(dr["RequestLTL"]);

                        freightClass = WiseTools.dbString(dr["FreightClass"]);
                        if (freightClass == "") { freightClass = "55"; }
                        pickupDate = WiseTools.dbString(dr["PickupDate"]);
                        if (pickupDate == "" ) { pickupDate = DateTime.Now.ToShortDateString(); }

                        notifyBeforeDelivery = WiseTools.dbBool(dr["NotifyBeforeDelivery"]);
                        liftgatePickup = WiseTools.dbBool(dr["LiftgatePickup"]);
                        liftgateDelivery = WiseTools.dbBool(dr["LiftgateDelivery"]);
                        limitedAccessPickup = WiseTools.dbBool(dr["LimitedAccessPickup"]);
                        limitedAccessDelivery = WiseTools.dbBool(dr["LimitedAccessDelivery"]);
                        residentialPickup = WiseTools.dbBool(dr["ResidentialPickup"]);
                        residentialDelivery = WiseTools.dbBool(dr["ResidentialDelivery"]);
                        insidePickup = WiseTools.dbBool(dr["InsidePickup"]);
                        insideDelivery = WiseTools.dbBool(dr["InsideDelivery"]);
                        sortAndSegregate = WiseTools.dbBool(dr["SortAndSegregate"]);
                        stopoffCharge = WiseTools.dbBool(dr["StopoffCharge"]);
                        dateRequested = WiseTools.dbDateTime(dr["DateRequested"]);
                        dateProcessed = WiseTools.dbDateTime(dr["DateProcessed"]);

                        acctNumber = WiseTools.dbInt(dr["AcctNumber"]);

                        shipWithArray = WiseTools.dbString(dr["shipWithArray"]);

                        if(shipWithArray != "")
                        {
                            packageWeights = getPackageWeightsFromShipWithArray(shipWithArray);
                        }
                        else
                        {
                            packageWeights = new List<int>();
                        }

                        accessorials = GetLTLAccessorials();
                        
                        success = true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
            }

            return (success);
        }

        private List<int> getPackageWeightsFromShipWithArray(string shipWithArray)
        {
            List<int> pkgWeights = new List<int>();

            string[] jobStrings = shipWithArray.Split(';');
            foreach(string jobString in jobStrings)
            {
                string[] jobDetails = jobString.Split('|');

                int shipWithNumPkgs = int.Parse(jobDetails[0]);
                int shipWithPkgWgt = int.Parse(jobDetails[1]);
                int shipWithLastPkgWgt = int.Parse(jobDetails[2]);

                if (shipWithLastPkgWgt == 0)
                {
                    shipWithLastPkgWgt = shipWithPkgWgt;
                }

                for (int i = 1; i < shipWithNumPkgs; i++ )
                {
                    pkgWeights.Add(shipWithPkgWgt);
                }
                pkgWeights.Add(shipWithLastPkgWgt);
            }

            return pkgWeights;
        }

        private void buildReplacementDictionary()
        {
            replacements.Add("AAA COOPER TRANSPORTATION", "AAA COOPER");
            replacements.Add("SOUTHEASTERN FREIGHT LINES", "SOUTHEASTERN");
            replacements.Add("R & L TRANSFER INC", "R & L");
            replacements.Add("FEDEX FREIGHT ECONOMY", "FEDEX ECONOMY");
            replacements.Add("FEDEX FREIGHT PRIORITY", "FEDEX PRIORITY");
            replacements.Add("Three-Day Select", "3DaySel");
            replacements.Add("Second Day Air A.M.", "2DA AM");
            replacements.Add("Second Day Air", "2DA");
            replacements.Add("Next Day Air Early A.M.", "NDA AM");
            replacements.Add("Next Day Air Saver", "NDA Saver");
            replacements.Add("Next Day Air", "NDA");
        }

        public void saveResults(List<RateDetail> rates, List<RateDetail> ltlRates)
        {

            WiseTools.logToFile(Config.logFile, "Beginning saveResults", true);

            List<string[]> ratesToSave = new List<string[]>();
            //buildReplacementDictionary();
            
            //WiseTools.logToFile(Config.logFile, "Replacement Dictionary has been built", true);

            foreach (RateDetail rate in rates)
            {
                string[] rateInfo = new string[6];
                rateInfo[0] = rate.basicProvider;
                rateInfo[1] = rate.basicMethod;
                rateInfo[2] = rate.basicRate.ToString();
                rateInfo[3] = rate.serviceDesc.ToString();
                rateInfo[4] = rate.addressClassification.ToString(); // If value is 2, Residential is TRUE
                rateInfo[5] = ""; // no note on UPS rates
                ratesToSave.Add(rateInfo);
            }


            //WiseTools.logToFile(Config.logFile, "UPS rates added to ratesToSave", true);

            
            foreach (RateDetail rate in ltlRates)
            {
                string[] rateInfo = new string[6];
                rateInfo[0] = rate.basicProvider;
                rateInfo[1] = rate.basicMethod;
                rateInfo[2] = rate.basicRate.ToString();
                //rateInfo[3] = rate.serviceDesc.ToString();
                rateInfo[4] = rate.addressClassification.ToString(); // If value is 2, Residential is TRUE
                if (rate.note == null)
                {
                    rateInfo[5] = "";
                }
                else
                {
                    rateInfo[5] = rate.note;
                }
                ratesToSave.Add(rateInfo);
            }

            //WiseTools.logToFile(Config.logFile, "LTL rates added to ratesToSave", true);

            foreach (string[] rateInfo in ratesToSave)
            {
                if (rateInfo[5] != "LTL")
                {
                    string basicProvider = rateInfo[1];
                    rateInfo[1] = abbreviateMethod(basicProvider);
                    rateInfo[3] = getMethodDesc(basicProvider);
                }
            }

            List<string[]> ratesToSaveAfterExclusions = new List<string[]>();
            foreach (string[] rateInfo in ratesToSave)
            {
                if(rateInfo[1] != "XX")
                {
                    ratesToSaveAfterExclusions.Add(rateInfo);
                }
            }

            WiseTools.logToFile(Config.logFile, "ratesToSave processed - about to initialize new DBUtil", true);

            DBUtil db = new DBUtil();

            //WiseTools.logToFile(Config.logFile, "DBUtil initialized - about to call saveResults", true);
            db.saveResults(source, uniqueId, ratesToSaveAfterExclusions, toAddress);

            WiseTools.logToFile(Config.logFile, "Completed saveResults", true);

            Console.WriteLine("Results saved to DB -- source: " + source + " id: " + uniqueId + " - " + DateTime.Now.ToShortTimeString());

        }

        private string GetLTLAccessorials()
        {
            string fullList = "";
            
            if (notifyBeforeDelivery)
            {
                fullList += "NOTIFY-BEFORE-DELIVERY;";
            }
            if (liftgatePickup)
            {
                fullList += "LIFTGATE-PICKUP;";
            }
            if (liftgateDelivery)
            {
                fullList += "LIFTGATE-DELIVERY;";
            }
            if (limitedAccessPickup)
            {
                fullList += "LIMITED-ACCESS-PICKUP;";
            }
            if (limitedAccessDelivery)
            {
                fullList += "LIMITED-ACCESS-DELIVERY;";
            }
            if (residentialPickup)
            {
                fullList += "RESIDENTIAL-PICKUP;";
            }
            if (residentialDelivery)
            {
                fullList += "RESIDENTIAL-DELIVERY;";
            }
            if (insidePickup)
            {
                fullList += "INSIDE-PICKUP;";
            }
            if (insideDelivery)
            {
                fullList += "INSIDE-DELIVERY;";
            }
            if (sortAndSegregate)
            {
                fullList += "SORT-AND-SEGREGATE;";
            }
            if (stopoffCharge)
            {
                fullList += "STOPOFF-CHARGE;";
            }

            if (fullList.Length > 0)
            {
                fullList = fullList.Substring(0, fullList.Length - 1);
            }

            return fullList;
        }

        private string abbreviateMethod(string fullMethod)
        {
            //WiseTools.logToFile(Config.logFile, "Starting abbreviateMethod", true);
            string shortMethod = "";

            if(!ProviderInfo.freightAbbreviations.TryGetValue(fullMethod, out shortMethod))
            {
                // Translate the service name into UI format.
                switch (fullMethod)
                {
                    case "UPSGround":
                        shortMethod = "Ground";
                        break;
                    case "UPSNextDayAir":
                        shortMethod = "Next Day Air";
                        break;
                    case "UPSNextDayAirEarlyAM":
                        shortMethod = "Next Day Air Early A.M.";
                        break;
                    case "UPSNextDayAirSaver":
                        shortMethod = "Next Day Air Saver";
                        break;
                    case "UPSSecondDayAir":
                        shortMethod = "Second Day Air";
                        break;
                    case "UPS3DaySelect":
                        shortMethod = "Three - Day Select";
                        break;
                    case "UPSGroundFreight":
                        shortMethod = "UPS Ground Freight";
                        break;
                    default:
                        shortMethod = fullMethod;
                        break;
                }
            }
            
            if (shortMethod == "")
            {
                WiseTools.logToFile(Config.logFile, "Provider abbreviation not found for " + fullMethod, true);
            }
            
            //WiseTools.logToFile(Config.logFile, "Leaving abbreviateMethod", true);

            return (shortMethod);
        }

        private string getMethodDesc(string fullMethod)
        {
            //WiseTools.logToFile(Config.logFile, "Starting getMethodDesc", true);
            string methodDesc = "";

            if(!ProviderInfo.freightDescriptions.TryGetValue(fullMethod, out methodDesc))
            {
                methodDesc = "";
            }

            if (methodDesc == "")
            {
                WiseTools.logToFile(Config.logFile, "Provider description not found for " + fullMethod, true);
            }

            //WiseTools.logToFile(Config.logFile, "Leaving getMethodDesc", true);

            return (methodDesc);
        }

        private string abbreviateMethod_DBversion(string fullMethod)
        {
            WiseTools.logToFile(Config.logFile, "Starting abbreviateMethod_DBversion", true);
            string shortMethod = "";
            /*
            foreach (string key in replacements.Keys)
            {
                shortMethod = shortMethod.Replace(key, replacements[key]);
            }
            */
            DBUtil db = new DBUtil();
            shortMethod = db.getProviderAbbrev(fullMethod);

            if (shortMethod == "")
            {
                WiseTools.logToFile(Config.logFile, "Provider abbreviation not found for " + fullMethod, true);
            }

            WiseTools.logToFile(Config.logFile, "Leaving abbreviateMethod_DBversion", true);

            return (shortMethod);
        }

        private string getMethodDesc_DBversion(string fullMethod)
        {
            WiseTools.logToFile(Config.logFile, "Starting getMethodDesc_DBversion", true);

            string methodDesc = "";

            DBUtil db = new DBUtil();
            methodDesc = db.getProviderDesc(fullMethod);

            if (methodDesc == "")
            {
                WiseTools.logToFile(Config.logFile, "Provider description not found for " + fullMethod, true);
            }

            WiseTools.logToFile(Config.logFile, "Leaving getMethodDesc_DBversion", true);

            return (methodDesc);
        }


        private void throwValidationError(string errorDetails)
        {
            //WRITE ERROR TO REQUEST
            Console.WriteLine(errorDetails);
        }

        internal void writeError(string errorCode, string errorMessage)
        {
            DBUtil db = new DBUtil();
            db.saveErrorMessage(source, uniqueId, errorCode, errorMessage);

            Console.WriteLine("Error saved to DB -- source: " + source + " id: " + uniqueId + " - " + DateTime.Now.ToShortTimeString());
            Console.WriteLine("Error code: " + errorCode);
            Console.WriteLine("Error msg: " + errorMessage);
        }
    }
}
