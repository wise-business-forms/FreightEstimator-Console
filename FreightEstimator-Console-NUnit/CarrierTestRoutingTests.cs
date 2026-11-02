using FreightEstApp35;
using NUnit.Framework;

namespace FreightEstimator_Console_NUnit
{
    public class CarrierTestRoutingTests
    {
        [Test]
        public void ResolveTransPlace_NotATest_ReturnsProduction()
        {
            var (url, token) = CarrierTestRouting.ResolveTransPlace(isTest: false);

            Assert.That(url, Is.EqualTo(Config.TransPlaceProdEndpoint.Url));
            Assert.That(token, Is.EqualTo(Config.TransPlaceProdEndpoint.Token));
        }

        [Test]
        public void ResolveTransPlace_IsATest_ReturnsSandbox()
        {
            var (url, token) = CarrierTestRouting.ResolveTransPlace(isTest: true);

            Assert.That(url, Is.EqualTo(Config.TransPlaceDemoEndpoint.Url));
            Assert.That(token, Is.EqualTo(Config.TransPlaceDemoEndpoint.Token));
        }

        [Test]
        public void ResolveTransportationInsight_NotATest_ReturnsProduction()
        {
            string url = CarrierTestRouting.ResolveTransportationInsight(isTest: false);

            Assert.That(url, Is.EqualTo(Config.TransportationInsightProdUrl));
        }

        [Test]
        public void ResolveTransportationInsight_IsATest_ReturnsSandbox()
        {
            string url = CarrierTestRouting.ResolveTransportationInsight(isTest: true);

            Assert.That(url, Is.EqualTo(Config.TransportationInsightDemoUrl));
        }
    }
}
