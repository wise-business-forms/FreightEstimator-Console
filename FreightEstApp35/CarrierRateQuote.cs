namespace FreightEstApp35
{
    public class CarrierRateQuote
    {
        public long Id { get; set; }
        public long CarrierApiCallId { get; set; }
        public string CarrierName { get; set; }
        public string ServiceLevel { get; set; }
        public decimal? BaseCost { get; set; }
        public decimal? MarkupPercent { get; set; }
        public decimal? PerPackageCharge { get; set; }
        public decimal? PerShipmentCharge { get; set; }
        public decimal TotalCharge { get; set; }
        public string Currency { get; set; } = "USD";
        public int? TransitDays { get; set; }
    }
}
