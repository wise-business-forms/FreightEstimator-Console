namespace FreightEstApp35
{
    /// <summary>
    /// Pure construction of CarrierRateQuote records - no I/O. Keeps each provider's existing
    /// (unchanged) cost-breakdown math in one testable place instead of inline in the HTTP/parsing code.
    /// </summary>
    public static class CarrierRateQuoteBuilder
    {
        public static CarrierRateQuote BuildLtlQuote(
            string carrierName,
            decimal baseCost,
            decimal markupPercent,
            decimal perPackageCharge,
            int numberOfPackages,
            decimal perShipmentCharge,
            int? transitDays = null,
            string currency = "USD",
            bool guardMarkup = true)
        {
            // TransPlace only applies markup when it's positive; TransportationInsight applies
            // it unconditionally. guardMarkup preserves each provider's existing formula exactly.
            decimal totalCharge = baseCost;
            if (!guardMarkup || markupPercent > 0) { totalCharge += (markupPercent / 100m) * baseCost; }
            if (perPackageCharge > 0) { totalCharge += perPackageCharge * numberOfPackages; }
            totalCharge += perShipmentCharge;

            return new CarrierRateQuote
            {
                CarrierName = carrierName,
                BaseCost = baseCost,
                MarkupPercent = markupPercent,
                PerPackageCharge = perPackageCharge,
                PerShipmentCharge = perShipmentCharge,
                TotalCharge = totalCharge,
                TransitDays = transitDays,
                Currency = string.IsNullOrEmpty(currency) ? "USD" : currency
            };
        }

        public static CarrierRateQuote BuildParcelQuote(
            string serviceLevel,
            decimal totalCharge,
            int? transitDays = null,
            string currency = "USD")
        {
            return new CarrierRateQuote
            {
                CarrierName = "UPS",
                ServiceLevel = serviceLevel,
                BaseCost = totalCharge,
                TotalCharge = totalCharge,
                TransitDays = transitDays,
                Currency = string.IsNullOrEmpty(currency) ? "USD" : currency
            };
        }
    }
}
