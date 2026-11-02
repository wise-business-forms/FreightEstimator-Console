using FreightEstApp35;
using NUnit.Framework;

namespace FreightEstimator_Console_NUnit
{
    public class CarrierRateQuoteBuilderTests
    {
        [Test]
        public void BuildLtlQuote_WithMarkupAndSurcharges_ComputesTotalCharge()
        {
            // TransPlace-style: cost + markup% of cost + (per-package * qty) + per-shipment
            var quote = CarrierRateQuoteBuilder.BuildLtlQuote(
                carrierName: "Old Dominion",
                baseCost: 100m,
                markupPercent: 10m,
                perPackageCharge: 2m,
                numberOfPackages: 5,
                perShipmentCharge: 15m,
                transitDays: 3);

            // 100 + (10% of 100 = 10) + (2 * 5 = 10) + 15 = 135
            Assert.That(quote.TotalCharge, Is.EqualTo(135m));
            Assert.That(quote.CarrierName, Is.EqualTo("Old Dominion"));
            Assert.That(quote.BaseCost, Is.EqualTo(100m));
            Assert.That(quote.MarkupPercent, Is.EqualTo(10m));
            Assert.That(quote.PerPackageCharge, Is.EqualTo(2m));
            Assert.That(quote.PerShipmentCharge, Is.EqualTo(15m));
            Assert.That(quote.TransitDays, Is.EqualTo(3));
        }

        [Test]
        public void BuildLtlQuote_WithNoMarkupOrSurcharges_TotalChargeEqualsBaseCost()
        {
            var quote = CarrierRateQuoteBuilder.BuildLtlQuote(
                carrierName: "LTL BENCHMARK",
                baseCost: 250.50m,
                markupPercent: 0m,
                perPackageCharge: 0m,
                numberOfPackages: 4,
                perShipmentCharge: 0m);

            Assert.That(quote.TotalCharge, Is.EqualTo(250.50m));
        }

        [Test]
        public void BuildLtlQuote_TransportationInsightStyle_AppliesUpchargeAsMarkupPercent()
        {
            // TransportationInsight's existing formula treats its per-plant upcharge figure
            // as a markup percentage, with no separate per-package/per-shipment charges.
            var quote = CarrierRateQuoteBuilder.BuildLtlQuote(
                carrierName: "Some TI Carrier",
                baseCost: 200m,
                markupPercent: 5m,
                perPackageCharge: 0m,
                numberOfPackages: 10,
                perShipmentCharge: 0m);

            // 200 + (5% of 200 = 10) = 210
            Assert.That(quote.TotalCharge, Is.EqualTo(210m));
        }

        [Test]
        public void BuildLtlQuote_DefaultsCurrencyToUsd()
        {
            var quote = CarrierRateQuoteBuilder.BuildLtlQuote(
                carrierName: "TransPlace Carrier",
                baseCost: 50m,
                markupPercent: 0m,
                perPackageCharge: 0m,
                numberOfPackages: 1,
                perShipmentCharge: 0m);

            Assert.That(quote.Currency, Is.EqualTo("USD"));
        }

        [Test]
        public void BuildLtlQuote_HonorsExplicitCurrency()
        {
            var quote = CarrierRateQuoteBuilder.BuildLtlQuote(
                carrierName: "TransPlace Carrier",
                baseCost: 50m,
                markupPercent: 0m,
                perPackageCharge: 0m,
                numberOfPackages: 1,
                perShipmentCharge: 0m,
                currency: "CAD");

            Assert.That(quote.Currency, Is.EqualTo("CAD"));
        }

        [Test]
        public void BuildLtlQuote_TransportationInsightStyle_AppliesNegativeMarkupUnconditionally()
        {
            // TransportationInsight's real formula (((PlantUpcharges/100)*Total) + Total) applies
            // even when the upcharge is negative - unlike TransPlace, which only applies markup > 0.
            var quote = CarrierRateQuoteBuilder.BuildLtlQuote(
                carrierName: "Some TI Carrier",
                baseCost: 200m,
                markupPercent: -5m,
                perPackageCharge: 0m,
                numberOfPackages: 10,
                perShipmentCharge: 0m,
                guardMarkup: false);

            // 200 + (-5% of 200 = -10) = 190
            Assert.That(quote.TotalCharge, Is.EqualTo(190m));
        }

        [Test]
        public void BuildLtlQuote_TransPlaceStyle_IgnoresNegativeMarkup()
        {
            // TransPlace's real formula only adds markup when it's > 0, so a negative markup
            // (guardMarkup defaults to true) must not reduce the total.
            var quote = CarrierRateQuoteBuilder.BuildLtlQuote(
                carrierName: "Old Dominion",
                baseCost: 200m,
                markupPercent: -5m,
                perPackageCharge: 0m,
                numberOfPackages: 10,
                perShipmentCharge: 0m);

            Assert.That(quote.TotalCharge, Is.EqualTo(200m));
        }

        [Test]
        public void BuildParcelQuote_NoMarkupApplied_BaseCostEqualsTotalCharge()
        {
            var quote = CarrierRateQuoteBuilder.BuildParcelQuote(
                serviceLevel: "UPS Ground",
                totalCharge: 42.75m,
                transitDays: 2);

            Assert.That(quote.BaseCost, Is.EqualTo(42.75m));
            Assert.That(quote.TotalCharge, Is.EqualTo(42.75m));
            Assert.That(quote.MarkupPercent, Is.Null);
            Assert.That(quote.PerPackageCharge, Is.Null);
            Assert.That(quote.PerShipmentCharge, Is.Null);
            Assert.That(quote.CarrierName, Is.EqualTo("UPS"));
            Assert.That(quote.ServiceLevel, Is.EqualTo("UPS Ground"));
            Assert.That(quote.Currency, Is.EqualTo("USD"));
        }
    }
}
