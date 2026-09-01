using System;
using System.Collections.Generic;

namespace FreightEstApp35
{
    public class CarrierApiCall
    {
        public long Id { get; set; }
        public string CorrelationId { get; set; }
        public string Application { get; set; } = "FreightEstApp35";
        public string ApiProvider { get; set; }
        public string ShippingMode { get; set; }
        public string Operation { get; set; }
        public string PlantCode { get; set; }
        public string AccountNumber { get; set; }
        public string ShipToAddress { get; set; }
        public string ShipToCity { get; set; }
        public string ShipToState { get; set; }
        public string ShipToZip { get; set; }
        public string ShipToCountry { get; set; }
        public string RequestUrl { get; set; }
        public int? HttpStatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? RespondedAtUtc { get; set; }
        public int? DurationMs { get; set; }
        public List<CarrierRateQuote> Quotes { get; set; } = new List<CarrierRateQuote>();
    }
}
