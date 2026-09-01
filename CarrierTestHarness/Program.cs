using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using FreightEstApp35;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CarrierTestHarness
{
    /// <summary>
    /// Standalone tool - separate process from the always-running production request-processing
    /// service - that triggers one shipment request on demand over HTTP (for use with Postman)
    /// and reads back the logged CarrierApiCall/CarrierRateQuote rows to prove persistence
    /// actually happened. Every request through it is a Test Call (IsTest = true).
    /// See docs/adr/0002-ups-test-calls-hit-production.md.
    /// </summary>
    class Program
    {
        const string Prefix = "http://localhost:5085/";

        static void Main(string[] args)
        {
            Config.SetProdDebug();

            var listener = new HttpListener();
            listener.Prefixes.Add(Prefix);
            listener.Start();

            Console.WriteLine("Carrier Test Harness listening on " + Prefix + " (localhost only)");
            Console.WriteLine("POST a JSON body to " + Prefix + "test-request");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                try
                {
                    HandleRequest(context);
                }
                catch (Exception ex)
                {
                    WriteJson(context.Response, 500, new { error = ex.Message });
                }
            }
        }

        static void HandleRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod != "POST" || context.Request.Url.AbsolutePath.Trim('/') != "test-request")
            {
                WriteJson(context.Response, 404, new { error = "POST /test-request with a JSON body" });
                return;
            }

            JObject payload;
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8))
            {
                payload = JObject.Parse(reader.ReadToEnd());
            }

            string correlationId = payload.Value<string>("correlationId");
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = "harness-test-" + Guid.NewGuid().ToString("N");
            }

            JToken toAddressToken = payload["toAddress"];
            Address toAddress = new Address
            {
                street = toAddressToken?.Value<string>("street") ?? "",
                city = toAddressToken?.Value<string>("city") ?? "",
                state = toAddressToken?.Value<string>("state") ?? "",
                zip = toAddressToken?.Value<string>("zip") ?? "",
                country = toAddressToken?.Value<string>("country") ?? "US"
            };

            bool requestUPS = payload.Value<bool?>("requestUPS") ?? true;
            bool requestLTL = payload.Value<bool?>("requestLTL") ?? true;

            UpsComm rates = new UpsComm
            {
                CorrelationId = correlationId,
                IsTest = true,
                toValidate = toAddress,
                plantCode = payload.Value<string>("fromPlant") ?? "ALP",
                ltlClass = payload.Value<string>("freightClass") ?? "55",
                deliveryConfCode = 0,
                pickupDate = payload.Value<DateTime?>("pickupDate") ?? DateTime.Now,
                numPackages = payload.Value<int?>("numPackages") ?? 1,
                pkgWeight = payload.Value<int?>("pkgWeight") ?? 10,
                lastPkgWeight = payload.Value<int?>("lastPkgWeight") ?? 0,
                packageWeights = new List<int>(),
                accessorials = payload.Value<string>("accessorials") ?? "",
                acctNumber = payload.Value<int?>("accountNumber") ?? 0
            };

            List<Address> candidates = rates.validateAddress();
            rates.toRate = candidates.Count == 1 ? candidates[0] : toAddress;

            var upsRateResults = new List<object>();
            var ltlRateResults = new List<object>();
            string processingError = null;

            try
            {
                if (requestUPS)
                {
                    foreach (RateDetail r in rates.getRates()) { upsRateResults.Add(RateToDto(r)); }
                }

                if (requestLTL)
                {
                    // Mirrors Program.processNextRequest()'s bucketing, which files UPS ground
                    // freight under the "LTL requested" flag even though it's a Parcel call -
                    // plus getLTLRates() (TransPlace), which the production pipeline never
                    // actually calls today but this feature's logging still needs to be testable.
                    foreach (RateDetail r in rates.getGroundFreightRates()) { ltlRateResults.Add(RateToDto(r)); }
                    foreach (RateDetail r in rates.getLTLRates(rates)) { ltlRateResults.Add(RateToDto(r)); }
                    foreach (RateDetail r in rates.getLTLRates_TransportationInsight(rates)) { ltlRateResults.Add(RateToDto(r)); }
                }
            }
            catch (Exception ex)
            {
                processingError = ex.Message;
            }

            List<CarrierApiCall> loggedCalls = CarrierApiLogger.GetCallsByCorrelationId(correlationId);

            WriteJson(context.Response, 200, new
            {
                correlationId,
                processingError,
                upsRates = upsRateResults,
                ltlRates = ltlRateResults,
                loggedCalls = loggedCalls.ConvertAll(c => (object)new
                {
                    c.Id,
                    c.ApiProvider,
                    c.ShippingMode,
                    c.Operation,
                    c.PlantCode,
                    c.IsSuccess,
                    c.IsTest,
                    c.HttpStatusCode,
                    c.ErrorMessage,
                    c.DurationMs,
                    c.RequestUrl,
                    Quotes = c.Quotes.ConvertAll(q => (object)new
                    {
                        q.CarrierName,
                        q.ServiceLevel,
                        q.BaseCost,
                        q.MarkupPercent,
                        q.PerPackageCharge,
                        q.PerShipmentCharge,
                        q.TotalCharge,
                        q.Currency,
                        q.TransitDays
                    })
                })
            });
        }

        static object RateToDto(RateDetail r)
        {
            return new
            {
                r.basicProvider,
                r.basicMethod,
                r.basicRate,
                r.totalCharges,
                r.plantCode,
                r.note
            };
        }

        static void WriteJson(HttpListenerResponse response, int statusCode, object body)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body, Formatting.Indented));

            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.OutputStream.Close();
        }
    }
}
