using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreightEstApp35
{
    class RateDetail
    {
        public string basicProvider { get; set; }
        public string basicMethod { get; set; }
        public decimal basicRate { get; set; }

        public string plantCode { get; set; }
        public string serviceDesc { get; set; }
        public string note { get; set; }
        public int addressClassification { get; set; }
        public int billingWeight { get; set; }
        public bool isHundredWeight { get; set; }

        public decimal totalCharges { get; set; }
        public decimal serviceCharges { get; set; }
        public decimal transporationCharges { get; set; }
        
        public RateDetail(string rateProvider, string methodDetail, decimal rateValue, string rateNote) 
        {
            basicProvider = rateProvider;
            basicMethod = methodDetail;
            basicRate = rateValue;
            note = rateNote;

            basicRate = Math.Round(100 * basicRate) / 100;
        }

        public RateDetail(string ratePlantCode, string rateServiceDesc, int rateAddressClassification, int rateBillingWeight, bool rateIsHundredWeight,
                          decimal rateTotalCharges, decimal rateServiceCharges, decimal rateTransportationCharges, string rateBasicProvider)
        {
            plantCode = ratePlantCode;
            serviceDesc = rateServiceDesc;
            addressClassification = rateAddressClassification;
            billingWeight = rateBillingWeight;
            isHundredWeight = rateIsHundredWeight;
            totalCharges = rateTotalCharges;
            serviceCharges = rateServiceCharges;
            transporationCharges = rateTransportationCharges;

            totalCharges = Math.Round(100 * totalCharges) / 100;
            serviceCharges = Math.Round(100 * serviceCharges) / 100;
            transporationCharges = Math.Round(100 * transporationCharges) / 100;

            basicProvider = rateBasicProvider;

            basicMethod = serviceDesc;
            basicRate = totalCharges;
        }
        /*
        private string getAbbrev(string fullProvider)
        {
            string abbreviation = "";

            DBUtil db = new DBUtil();
            abbreviation = db.getProviderAbbrev(fullProvider);

            if (abbreviation == "")
            {
                WiseTools.logToFile(Config.logFile, "Provider abbreviation not found for " + fullProvider, true);
            }

            return (abbreviation);
        }


        public string basicProviderAbbrev
        {
            get
            {
                return getAbbrev(basicProvider);
            }
        }*/
    }
}
