namespace FreightEstApp35
{
    /// <summary>
    /// Pure per-request routing decision - no I/O. UPS has no available sandbox for the REST
    /// Rating API this app uses (see docs/adr/0002-ups-test-calls-hit-production.md), so only
    /// the LTL providers get a resolver here; UPS Test Calls always use production.
    /// </summary>
    public static class CarrierTestRouting
    {
        public static (string Url, string Token) ResolveTransPlace(bool isTest)
        {
            return isTest ? Config.TransPlaceDemoEndpoint : Config.TransPlaceProdEndpoint;
        }

        public static string ResolveTransportationInsight(bool isTest)
        {
            return isTest ? Config.TransportationInsightDemoUrl : Config.TransportationInsightProdUrl;
        }
    }
}
