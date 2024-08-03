using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml.Linq;
using System.IO;
using System.Net;
using FreightEstApp35.UpsAddressValidationWebReference;
using FreightEstApp35.UpsRateWebReference;

namespace FreightEstApp35
{
    class UpsComm
    {
        public Address toValidate { get; set; }
        public Address toRate { get; set; }
        public Address fromRate { get; set; }

        /* Used for single job shipment */
        public int numPackages { get; set; }
        public int pkgWeight { get; set; }
        public int lastPkgWeight { get; set; }

        /* Used for multi-job shipments (ship-withs) */
        public List<int> packageWeights { get; set; }

        public int deliveryConfCode { get; set; }
        public string plantCode { get; set; }
        public List<Address> addressCandidates { get; set; }
        public DateTime pickupDate { get; set; }
        public string ltlClass { get; set; }
        public string accessorials { get; set; }

        public int acctNumber { get; set; }

        public UpsComm()
        {
        }

        public List<Address> validateAddress()
        {
            StringBuilder sbResults = new StringBuilder();
            addressCandidates = new List<Address>();
            

            Console.WriteLine(sbResults.ToString());
            return (addressCandidates);
        }

        public List<RateDetail> getRates()
        {
            List<RateDetail> rates = new List<RateDetail>();

            #region Build out shipment information.
            Shipment shipment = new Shipment();
            shipment.PlantId = this.plantCode;
            // Ship To info
            shipment.Address = toValidate.street;
            shipment.City = toValidate.city;
            shipment.State_selection = toValidate.state;
            shipment.Zip = toValidate.zip;
            shipment.Country_selection = toValidate.country;
            shipment.AcctNum = acctNumber.ToString();
            shipment.package_weight = this.pkgWeight;
            // Package info
            shipment.number_of_packages = this.numPackages;
            shipment.package_weight = this.pkgWeight;
            shipment.last_package_weight = this.lastPkgWeight;

            #endregion

            ShopRateResponse shopRateResponse = new ShopRateResponse();
            UPSRequest uPSRequest = new UPSRequest(shipment, new Plant(shipment.PlantId), UPSRequest.RequestOption.Shop);
            uPSRequest.Response();
            shopRateResponse.UPSServices = uPSRequest.UPSServices;

            foreach (UPSService service in uPSRequest.UPSServices)
            {
                RateDetail rateDetail = new RateDetail(shipment.PlantId, service.ServiceName, 1, int.Parse(shipment.billing_weight.ToString()), false, Decimal.Parse(service.Rate), 0, 0, "UPS");
                rates.Add(rateDetail);
            }

            return (rates);
        }
        
        public List<RateDetail> getLTLRates()
        {
            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();

            Console.WriteLine(sbResults.ToString());
            return (rates);
        }

        private void handleError(string source, string details)
        {
            Console.WriteLine("=================================");
            Console.WriteLine(DateTime.Now.ToLongTimeString());
            Console.WriteLine("Error from " + source + ":");
            Console.WriteLine(details);
            Console.WriteLine("=================================");
        }

        public List<RateDetail> getGroundFreightRates()
        {

            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();


            return (rates);
        }
        
    }
}
