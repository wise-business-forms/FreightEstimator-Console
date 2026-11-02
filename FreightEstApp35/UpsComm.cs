using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml.Linq;
using System.IO;
using System.Net;
using Microsoft.Data.SqlClient;

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

        public string CorrelationId { get; set; }
        public bool IsTest { get; set; }

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
            shipment.CorrelationId = this.CorrelationId;
            shipment.IsTest = this.IsTest;
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

            #region Return results
            if (shipment.number_of_packages != 0)  // Apparently this is a thing. Orders in the queue may not have packages but have weight.

            {
                ShopRateResponse shopRateResponse = new ShopRateResponse();
                UPSRequest uPSRequest = new UPSRequest(shipment, new Plant(shipment.PlantId), UPSRequest.RequestOption.Shop);
                uPSRequest.Response();
                shopRateResponse.UPSServices = uPSRequest.UPSServices;

                DBUtil dBUtil = new DBUtil();
                var charges = dBUtil.getPlantCharges("UPS", 0);
                List<CarrierRateQuote> quotes = new List<CarrierRateQuote>();
                if (uPSRequest.UPSServices != null)
                {
                    foreach (UPSService service in uPSRequest.UPSServices)
                    {
                        service.PublishedRate = RateCalculations.CalculateRate(shipment.AcctNum, shipment.PlantId, service.ServiceName, service.PublishedRate, service.NegotiatedRate, shipment.number_of_packages, shipment.package_weight.ToString(), shipment.last_package_weight.ToString()); // Should use CWT not ServiceName for cleanliness.
                        try
                        {
                            decimal totalCharges = Decimal.Parse(service.PublishedRate.Replace("$", ""));

                            RateDetail rateDetail = new RateDetail("UPS", service.ServiceName, 0, "", shipment.PlantId);
                            rateDetail.totalCharges = totalCharges;
                            rateDetail.serviceDesc = service.ServiceName;

                        rates.Add(rateDetail);

                            quotes.Add(CarrierRateQuoteBuilder.BuildParcelQuote(service.ServiceName, totalCharges));
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }
                CarrierApiLogger.LogQuotes(uPSRequest.LastCarrierApiCallId, quotes);
            }
            #endregion
            return (rates);
        }
        
        public List<RateDetail> getLTLRates(UpsComm myUPS)
        {
            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();
            ShopRateResponse response = new ShopRateResponse();
            response.UPSServices = new UPSService[0];

            #region Build out shipment information.
            Shipment shipment = new Shipment();
            shipment.CorrelationId = this.CorrelationId;
            shipment.IsTest = this.IsTest;
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
            shipment.pick_up_date = System.DateTime.Now;

            // Calulate Billing weight
            if (shipment.package_weight != shipment.last_package_weight)
            {
                int _fullPackages = shipment.number_of_packages - 1;
                shipment.billing_weight = (shipment.package_weight * _fullPackages) + shipment.last_package_weight;
            }
            else { shipment.billing_weight = shipment.number_of_packages * shipment.package_weight; }

            shipment.freight_class_selected = 55;

            // process the old accessorials format to the new model -  Though we do not use below. :)
            foreach(string accessorial in myUPS.accessorials.Split(';'))
            {
                switch (accessorial)
                {
                    case "NOTIFY-BEFORE-DELIVERY":
                        shipment.notify_before_delivery = true;
                        break;
                    case "LIFTGATE-PICKUP":
                        shipment.liftgate_pickup = true;
                        break;
                    case "LIFTGATE-DELIVERY":
                        shipment.liftgate_delivery = true;
                        break;
                    case "LIMITED-ACCESS-PICKUP":
                        shipment.limited_access_pickup = true;
                        break;
                    case "LIMITED-ACCESS-DELIVERY;":
                        shipment.limited_access_delivery = true;
                        break;
                    case "RESIDENTIAL-PICKUP;":
                        shipment.residential_pickup = true;
                        break;
                    case "RESIDENTIAL-DELIVERY;":
                        shipment.residential_delivery = true;
                        break;
                    case "INSIDE-PICKUP;":
                        shipment.inside_pickup = true;
                        break;
                    case "INSIDE-DELIVERY;":
                        shipment.inside_delivery = true;
                        break;
                    case "SORT-AND-SEGREGATE;":
                        shipment.sort_and_segregate = true;
                        break;
                    case "STOPOFF-CHARGE;":
                        shipment.stopoff_charge = true;
                        break;
                }
            }

            #endregion
            try
            {
                #region -- Define Per Package Charge and Per Shipment charge dictionaries for LTL (chare per plant) --
                Dictionary<string, double> dPerPackageChargeLTL = new Dictionary<string, double>();
                Dictionary<string, double> dPerShipmentChargeLTL = new Dictionary<string, double>();
                Dictionary<string, double> dUpchargeLTL = new Dictionary<string, double>();

                SqlConnection sqlConnection = new SqlConnection(Config.ConnString);
                sqlConnection.Open();

                SqlCommand cmdCharges = sqlConnection.CreateCommand();
                cmdCharges.CommandText = "GetPlantCharges";
                cmdCharges.CommandType = System.Data.CommandType.StoredProcedure;
                cmdCharges.Parameters.Add("@Carrier", System.Data.SqlDbType.VarChar, 50).Value = "M33";
                cmdCharges.Parameters.Add("@AcctNumber", System.Data.SqlDbType.Int).Value = shipment.AcctNum;

                SqlDataReader drCharges = cmdCharges.ExecuteReader();

                while (drCharges.Read())
                {
                    dPerPackageChargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                    dPerShipmentChargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));
                    dUpchargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
                }
                #endregion

                (string url, string token) = CarrierTestRouting.ResolveTransPlace(shipment.IsTest);
                string pickupDate = shipment.pick_up_date.ToShortDateString();
                string ltlClass = shipment.freight_class_selected.ToString();

                #region -- Define dtLTLServices to hold rate data --
                DataSet dsLTLServices = new DataSet();
                DataTable dtLTLServices = dsLTLServices.Tables.Add();

                dtLTLServices.Columns.Add("Plant", typeof(string));
                dtLTLServices.Columns.Add("Service", typeof(string));
                dtLTLServices.Columns.Add("Rate", typeof(double));
                dtLTLServices.Columns.Add("TransitDays", typeof(int));
                dtLTLServices.Columns.Add("Direct", typeof(string));
                #endregion

                string[] plantCodes = { "" };

                if (shipment.PlantId == "ALL")
                {
                    plantCodes = Config.PlantCodes;
                }
                else
                {
                    plantCodes[0] = shipment.PlantId;
                }

                List<UPSService> ltlServices = new List<UPSService>();

                foreach (string plantCode in plantCodes)
                {
                    string requestUrl = url + "rate/quote?loginToken=" + token;

                    #region -- Load Plant Ship From Address --
                    string shipFromCity = "";
                    string shipFromState = "";
                    string shipFromZip = "";
                    string shipFromCountry = "";

                    SqlConnection conn = new SqlConnection(Config.ConnString);
                    conn.Open();

                    SqlCommand sqlCommand = conn.CreateCommand();
                    sqlCommand.Connection = conn;
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.CommandText = "SELECT city, State, Zip, Country FROM Plants WHERE Plantcode = '" + plantCode + "'";

                    SqlDataReader drResults = sqlCommand.ExecuteReader();

                    if (drResults.Read())
                    {
                        shipFromCity = drResults["City"].ToString();
                        shipFromState = drResults["State"].ToString();
                        shipFromZip = drResults["Zip"].ToString();
                        shipFromCountry = drResults["Country"].ToString();
                    }
                    else
                    {
                        sbResults.Append("Unable to lookup address info for Plant " + plantCode + "'");
                    }

                    conn.Close();

                    #endregion

                    #region Build XML Request
                    StringBuilder postData = new StringBuilder("<?xml version=\"1.0\"?>");
                    postData.Append("<quote>");
                    postData.Append("<requestedMode>LTL</requestedMode>");
                    postData.Append("<requestedPickupDate>" + shipment.pick_up_date.ToString("yyyy-MM-dd") + "</requestedPickupDate>");
                    postData.Append("<shipper>");
                    postData.Append("<city>" + shipFromCity + "</city>");
                    postData.Append("<region>" + shipFromState + "</region>");
                    postData.Append("<country>" + shipFromCountry + "</country>");
                    postData.Append("<postalCode>" + shipFromZip + "</postalCode>");
                    postData.Append("</shipper>");
                    postData.Append("<consignee>");
                    postData.Append("<city>" + shipment.City + "</city>");
                    postData.Append("<region>" + shipment.State_selection + "</region>");
                    postData.Append("<country>" + shipment.Country_selection + "</country>");
                    postData.Append("<postalCode>" + shipment.Zip + "</postalCode>");
                    postData.Append("</consignee>");
                    postData.Append("<lineItems>");
                    postData.Append("<lineItem>");
                    postData.Append("<freightClass>" + ltlClass + "</freightClass>");
                    postData.Append("<weight>" + shipment.billing_weight + "</weight>");
                    postData.Append("<weightUnit>LB</weightUnit>");
                    postData.Append("</lineItem>");
                    postData.Append("</lineItems>");

                    //string accessorials = GetLTLAccessorials(shipment);
                    string accessorials = myUPS.accessorials;
                    if (accessorials.Length > 0)
                    {
                        postData.Append("<accessorials>");
                        string[] accessorialArray = accessorials.Split(';');
                        for (int i = 0; i < accessorialArray.Count(); i++)
                        {
                            postData.Append("<accessorial><type>" + accessorialArray[i] + "</type></accessorial>");
                        }
                        postData.Append("</accessorials>");
                    }
                    //postData += "<accessorials>";
                    //postData += "<accessorial>";
                    //postData += "<type>LIFTGATE-PICKUP</type>";
                    //postData += "</accessorial>";
                    //postData += "</accessorials>";
                    postData.Append("</quote>");
                    #endregion

                    DateTime requestedAtUtc = DateTime.UtcNow;
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    string responseFromServer = null;
                    int? statusCode = null;
                    bool callSucceeded = false;
                    string callErrorMessage = null;

                    try
                    {
                        WebRequest request = WebRequest.Create(requestUrl);
                        request.Method = "POST";
                        request.ContentType = "text/xml";

                        byte[] byteArray = Encoding.UTF8.GetBytes(postData.ToString());
                        request.ContentLength = byteArray.Length;

                        Stream dataStream = request.GetRequestStream();
                        dataStream.Write(byteArray, 0, byteArray.Length);
                        dataStream.Close();

                        using (WebResponse webResponse = request.GetResponse())
                        {
                            statusCode = (int)((HttpWebResponse)webResponse).StatusCode;

                            using (var reader = new StreamReader(webResponse.GetResponseStream()))
                            {
                                responseFromServer = reader.ReadToEnd();
                            }
                        }

                        callSucceeded = true;
                    }
                    catch (WebException wex)
                    {
                        using (HttpWebResponse errorResponse = wex.Response as HttpWebResponse)
                        {
                            if (errorResponse != null)
                            {
                                statusCode = (int)errorResponse.StatusCode;
                                using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                                {
                                    responseFromServer = reader.ReadToEnd();
                                }
                            }
                        }
                        callErrorMessage = wex.Message;
                    }
                    catch (Exception ex)
                    {
                        callErrorMessage = ex.Message;
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }

                    List<CarrierRateQuote> plantQuotes = new List<CarrierRateQuote>();

                    if (callSucceeded)
                    {
                        try
                        {
                            #region Parse response
                            XDocument xmlDoc = XDocument.Parse(responseFromServer);

                            foreach (var rate in xmlDoc.Descendants("rate"))
                            {
                                string carrier = rate.Element("carrier").Element("name").Value.Trim();
                                string direct = rate.Element("direct").Value.Trim();
                                int transitDays = Convert.ToInt16(rate.Element("transitDays").Value.Trim());
                                double cost = double.Parse(rate.Element("cost").Element("totalAmount").Value.Trim());
                                double totalCharges = 0;

                                #region -- Define variables for markup calculations --
                                double markupPercentage = dUpchargeLTL[plantCode];
                                double perPackageCharge = dPerPackageChargeLTL[plantCode];
                                double perShipmentCharge = dPerShipmentChargeLTL[plantCode];
                                #endregion

                                totalCharges = cost;
                                if (markupPercentage > 0) { totalCharges += ((markupPercentage / 100) * cost); }
                                if (perPackageCharge > 0) { totalCharges += (perPackageCharge * shipment.number_of_packages); }
                                totalCharges += perShipmentCharge;

                                //if ((carrier != "LTL BENCHMARK") || (Session["DefaultPlant"].ToString() == "POR"))
                                if (carrier != "LTL BENCHMARK")
                                {
                                    UPSService service = new UPSService();
                                    service.PlantCode = plantCode;
                                    service.ServiceName = carrier;
                                    service.PublishedRate = cost.ToString();
                                    service.TotalCost = totalCharges.ToString();
                                    service.TransitDays = transitDays.ToString();
                                    service.Direct = direct;

                                    ltlServices.Add(service);

                                }
                                else if (plantCode == "POR")
                                {
                                    UPSService service = new UPSService
                                    {
                                        PlantCode = plantCode,
                                        ServiceName = carrier,
                                        PublishedRate = cost.ToString(),
                                        TransitDays = transitDays.ToString(),
                                        Direct = direct
                                    };

                                    ltlServices.Add(service);
                                }

                                DBUtil dBUtil = new DBUtil();
                                var charges = dBUtil.getPlantCharges("UPS", 0);
                                //List<Plant> plants = Plant.Plants();
                                //foreach(var surcharge in )
                                response.UPSServices = ltlServices.ToArray();

                                plantQuotes.Add(CarrierRateQuoteBuilder.BuildLtlQuote(
                                    carrierName: carrier,
                                    baseCost: (decimal)cost,
                                    markupPercent: (decimal)markupPercentage,
                                    perPackageCharge: (decimal)perPackageCharge,
                                    numberOfPackages: shipment.number_of_packages,
                                    perShipmentCharge: (decimal)perShipmentCharge,
                                    transitDays: transitDays));
                            }
                            #endregion
                        }
                        catch (Exception parseEx)
                        {
                            // A parse failure is still a failed call worth logging - don't let it
                            // propagate and abort logging for the remaining plants in this loop.
                            callSucceeded = false;
                            callErrorMessage = "Parse error: " + parseEx.Message;
                            plantQuotes.Clear();
                        }
                    }

                    CarrierApiLogger.LogApiCall(new CarrierApiCall
                    {
                        CorrelationId = shipment.CorrelationId,
                        IsTest = shipment.IsTest,
                        ApiProvider = "TransPlace",
                        ShippingMode = "LTL",
                        Operation = "Quote",
                        PlantCode = plantCode,
                        AccountNumber = shipment.AcctNum,
                        ShipToAddress = shipment.Address,
                        ShipToCity = shipment.City,
                        ShipToState = shipment.State_selection,
                        ShipToZip = shipment.Zip,
                        ShipToCountry = shipment.Country_selection,
                        RequestUrl = requestUrl,
                        HttpStatusCode = statusCode,
                        IsSuccess = callSucceeded,
                        ErrorMessage = callErrorMessage,
                        RequestBody = postData.ToString(),
                        ResponseBody = responseFromServer,
                        RequestedAtUtc = requestedAtUtc,
                        RespondedAtUtc = DateTime.UtcNow,
                        DurationMs = (int)stopwatch.ElapsedMilliseconds,
                        Quotes = plantQuotes
                    });

                    if (!callSucceeded)
                    {
                        // Move on to the next plant code instead of aborting the whole request.
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO
            }
            Console.WriteLine(sbResults.ToString());

            List<RateDetail> r = new List<RateDetail>();
            foreach(var s in response.UPSServices)
            {
                rates.Add(new RateDetail("LTL", s.ServiceName, Decimal.Parse(s.TotalCost), "LTL", shipment.PlantId));
            }
                return rates;
        }


        public List<RateDetail> getLTLRates_TransportationInsight(UpsComm myUPS)
        {
            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();
            ShopRateResponse response = new ShopRateResponse();
            response.UPSServices = new UPSService[0];

            string _request = String.Empty;
            string _response = String.Empty;

            #region Build out shipment information.
            Shipment shipment = new Shipment();
            shipment.CorrelationId = this.CorrelationId;
            shipment.IsTest = this.IsTest;
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
            shipment.pick_up_date = System.DateTime.Now;

            // Calulate Billing weight
            if (shipment.package_weight != shipment.last_package_weight)
            {
                int _fullPackages = shipment.number_of_packages - 1;
                shipment.billing_weight = (shipment.package_weight * _fullPackages) + shipment.last_package_weight;
            }
            else { shipment.billing_weight = shipment.number_of_packages * shipment.package_weight; }

            shipment.freight_class_selected = 55;

            // process the old accessorials format to the new model -  Though we do not use below. :)
            foreach (string accessorial in myUPS.accessorials.Split(';'))
            {
                switch (accessorial)
                {
                    case "NOTIFY-BEFORE-DELIVERY":
                        shipment.notify_before_delivery = true;
                        break;
                    case "LIFTGATE-PICKUP":
                        shipment.liftgate_pickup = true;
                        break;
                    case "LIFTGATE-DELIVERY":
                        shipment.liftgate_delivery = true;
                        break;
                    case "LIMITED-ACCESS-PICKUP":
                        shipment.limited_access_pickup = true;
                        break;
                    case "LIMITED-ACCESS-DELIVERY":
                        shipment.limited_access_delivery = true;
                        break;
                    case "RESIDENTIAL-PICKUP":
                        shipment.residential_pickup = true;
                        break;
                    case "RESIDENTIAL-DELIVERY":
                        shipment.residential_delivery = true;
                        break;
                    case "INSIDE-PICKUP":
                        shipment.inside_pickup = true;
                        break;
                    case "INSIDE-DELIVERY":
                        shipment.inside_delivery = true;
                        break;
                    case "SORT-AND-SEGREGATE":
                        shipment.sort_and_segregate = true;
                        break;
                    case "STOPOFF-CHARGE":
                        shipment.stopoff_charge = true;
                        break;
                }
            }

            #endregion
            try
            {
                #region -- Define Per Package Charge and Per Shipment charge dictionaries for LTL (chare per plant) --
                Dictionary<string, double> dPerPackageChargeLTL = new Dictionary<string, double>();
                Dictionary<string, double> dPerShipmentChargeLTL = new Dictionary<string, double>();
                Dictionary<string, double> dUpchargeLTL = new Dictionary<string, double>();

                SqlConnection sqlConnection = new SqlConnection(Config.ConnString);
                sqlConnection.Open();

                SqlCommand cmdCharges = sqlConnection.CreateCommand();
                cmdCharges.CommandText = "GetPlantCharges";
                cmdCharges.CommandType = System.Data.CommandType.StoredProcedure;
                cmdCharges.Parameters.Add("@Carrier", System.Data.SqlDbType.VarChar, 50).Value = "M33";
                cmdCharges.Parameters.Add("@AcctNumber", System.Data.SqlDbType.Int).Value = shipment.AcctNum;

                SqlDataReader drCharges = cmdCharges.ExecuteReader();

                while (drCharges.Read())
                {
                    dPerPackageChargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                    dPerShipmentChargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));
                    dUpchargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
                }
                #endregion

                string pickupDate = shipment.pick_up_date.ToShortDateString();
                string ltlClass = shipment.freight_class_selected.ToString();

                #region -- Define dtLTLServices to hold rate data --
                DataSet dsLTLServices = new DataSet();
                DataTable dtLTLServices = dsLTLServices.Tables.Add();

                dtLTLServices.Columns.Add("Plant", typeof(string));
                dtLTLServices.Columns.Add("Service", typeof(string));
                dtLTLServices.Columns.Add("Rate", typeof(double));
                dtLTLServices.Columns.Add("TransitDays", typeof(int));
                dtLTLServices.Columns.Add("Direct", typeof(string));
                #endregion

                string[] plantCodes = { "" };

                if (shipment.PlantId == "ALL")
                {
                    plantCodes = Config.PlantCodes;
                }
                else
                {
                    plantCodes[0] = shipment.PlantId;
                }

                List<UPSService> ltlServices = new List<UPSService>();

                foreach (string plantCode in plantCodes)
                {


                    #region -- Load Plant Ship From Address --
                    string shipFromCity = "";
                    string shipFromState = "";
                    string shipFromZip = "";
                    string shipFromCountry = "";

                    SqlConnection conn = new SqlConnection(Config.ConnString);
                    conn.Open();

                    SqlCommand sqlCommand = conn.CreateCommand();
                    sqlCommand.Connection = conn;
                    sqlCommand.CommandType = CommandType.Text;
                    sqlCommand.CommandText = "SELECT city, State, Zip, Country FROM Plants WHERE Plantcode = '" + plantCode + "'";

                    SqlDataReader drResults = sqlCommand.ExecuteReader();

                    if (drResults.Read())
                    {
                        shipFromCity = drResults["City"].ToString();
                        shipFromState = drResults["State"].ToString();
                        shipFromZip = drResults["Zip"].ToString();
                        shipFromCountry = drResults["Country"].ToString();
                    }
                    else
                    {
                        sbResults.Append("Unable to lookup address info for Plant " + plantCode + "'");
                    }

                    conn.Close();

                    #endregion

                    #region Build XML Request
                    if (shipment.Country_selection == "US")
                    {
                        shipment.Country_selection = "USA";
                    }

                    if(shipFromCountry == "US")
                    {
                        shipFromCountry = "USA";
                    }

                    StringBuilder postData = new StringBuilder("<?xml version=\"1.0\"?>");
                    postData.Append("<service-request xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">");
                    postData.Append("<service-id>XMLRating</service-id>");
                    postData.Append("<request-id>123456789</request-id>");
                    postData.Append("<data><RateRequest><RatingLevel isCompanyAccountNumber=\"true\">WISE03RATE</RatingLevel>");
                    postData.Append("<Constraints>");
                    postData.Append("<PaymentTerms>Prepaid</PaymentTerms>");
                    // Add Accessorials
                    string accessorials = GetAccessorialsXML(shipment);
                    if(accessorials.Length > 0)
                    {
                        postData.Append("<ServiceFlags>");
                        postData.Append(accessorials);
                        postData.Append("</ServiceFlags>");
                    }
                    else
                    {
                        postData.Append("<ServiceFlags />");
                    }
                    // End Accessorials                    
                    postData.Append("</Constraints>");
                    postData.Append("<Items>");
                    for (int i = 0; i < shipment.number_of_packages; i++)
                    {
                        postData.Append("<Item sequence=\"1\" freightClass=\"");
                        postData.Append("55");
                        postData.Append("\">");
                        postData.Append("<Weight units=\"lb\">");
                        postData.Append(shipment.package_weight.ToString());
                        postData.Append("</Weight>");
                        postData.Append("<Dimensions length=\"5.0\" width=\"5.0\" height=\"5.0\" units=\"in\" /></Item>");
                    }
                    postData.Append("</Items>");

                    postData.Append("<Events><Event sequence=\"1\" type=\"Pickup\" date=\"").Append(System.DateTime.Now.ToString("MM/dd/yyyy HH:mm")).Append("\">");
                    postData.Append("<Location><City>").Append(shipFromCity).Append("</City><State>").Append(shipFromState).Append("</State><Zip>").Append(shipFromZip).Append("</Zip><Country>").Append(shipFromCountry).Append("</Country></Location>");
                    postData.Append("</Event>");
                    postData.Append("<Event sequence=\"2\" type=\"Drop\" date=\"").Append(System.DateTime.Now.AddDays(1).ToString("MM/dd/yyyy HH:mm")).Append("\">");
                    postData.Append("<Location><City>").Append(shipment.Corrected_City).Append("</City><State>").Append(shipment.State_selection).Append("</State><Zip>").Append(shipment.Zip).Append("</Zip><Country>").Append(shipment.Country_selection).Append("</Country></Location>");
                    postData.Append("</Event></Events></RateRequest></data></service-request>");

                    #endregion

                    #region Add UserID/Pass & encode.
                    StringBuilder postDataCred = new StringBuilder();
                    postDataCred.Append("userid=").Append($"{Uri.EscapeDataString(Config.TransprtationInsightApiUsername)}");
                    postDataCred.Append("&password=").Append($"{Uri.EscapeDataString(Config.TransprtationInsightApiPassword)}");
                    postDataCred.Append("&request=").Append($"{Uri.UnescapeDataString(postData.ToString())}");
                    #endregion


                    _request = postData.ToString();

                    string transportationInsightUrl = CarrierTestRouting.ResolveTransportationInsight(shipment.IsTest);

                    DateTime requestedAtUtc = DateTime.UtcNow;
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    string responseFromServer = null;
                    int? statusCode = null;
                    bool callSucceeded = false;
                    string callErrorMessage = null;

                    try
                    {
                        WebRequest request = WebRequest.Create(transportationInsightUrl);
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Method = "POST";

                        byte[] byteArray = Encoding.UTF8.GetBytes(postDataCred.ToString());
                        request.ContentLength = byteArray.Length;

                        Stream dataStream = request.GetRequestStream();
                        dataStream.Write(byteArray, 0, byteArray.Length);
                        dataStream.Close();

                        using (WebResponse webResponse = request.GetResponse())
                        {
                            statusCode = (int)((HttpWebResponse)webResponse).StatusCode;

                            using (var reader = new StreamReader(webResponse.GetResponseStream()))
                            {
                                responseFromServer = reader.ReadToEnd();
                            }
                        }

                        callSucceeded = true;
                    }
                    catch (WebException wex)
                    {
                        using (HttpWebResponse errorResponse = wex.Response as HttpWebResponse)
                        {
                            if (errorResponse != null)
                            {
                                statusCode = (int)errorResponse.StatusCode;
                                using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                                {
                                    responseFromServer = reader.ReadToEnd();
                                }
                            }
                        }
                        callErrorMessage = wex.Message;
                    }
                    catch (Exception ex)
                    {
                        callErrorMessage = ex.Message;
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }

                    List<CarrierRateQuote> plantQuotes = new List<CarrierRateQuote>();
                    string decodedString = null;

                    if (callSucceeded)
                    {
                        try
                        {
                            #region Parse response
                            // Decode response.
                            XDocument xmlDoc = XDocument.Parse(responseFromServer);
                            string encodedData = xmlDoc.Descendants("data").FirstOrDefault()?.Value;
                            byte[] data = Convert.FromBase64String(encodedData);
                            decodedString = System.Text.Encoding.UTF8.GetString(data);
                            _response = decodedString;

                            // Parse the result.
                            XDocument decodedXmlDoc = XDocument.Parse(decodedString);
                            var priceSheets = decodedXmlDoc.Descendants("PriceSheet").Select(sheet => new
                            {
                                CarrierName = sheet.Element("CarrierName")?.Value,
                                Rate = sheet.Element("CarrierName")?.Value,
                                TotalCost = sheet.Element("Total")?.Value,
                                TransitDays = sheet.Element("ServiceDays")?.Value,
                                Direct = string.Empty // Information not supplied.
                            });

                            float PlantUpcharges = GetPlantUpcharges(shipment, "TI");
                            foreach (var priceSheet in priceSheets)
                            {
                                float _totalCost = ((PlantUpcharges / 100) * float.Parse(priceSheet.TotalCost)) + float.Parse(priceSheet.TotalCost);

                                UPSService service = new UPSService();
                                service.PlantCode = plantCode;
                                service.ServiceName = priceSheet.CarrierName;
                                service.PublishedRate = priceSheet.Rate;
                                service.TotalCost = _totalCost.ToString("C");
                                service.TransitDays = priceSheet.TransitDays;
                                service.Direct = priceSheet.Direct;

                                ltlServices.Add(service);

                                int? transitDays = null;
                                if (int.TryParse(priceSheet.TransitDays, out int parsedTransitDays)) { transitDays = parsedTransitDays; }

                                plantQuotes.Add(CarrierRateQuoteBuilder.BuildLtlQuote(
                                    carrierName: priceSheet.CarrierName,
                                    baseCost: (decimal)float.Parse(priceSheet.TotalCost),
                                    markupPercent: (decimal)PlantUpcharges,
                                    perPackageCharge: 0m,
                                    numberOfPackages: shipment.number_of_packages,
                                    perShipmentCharge: 0m,
                                    transitDays: transitDays,
                                    guardMarkup: false));
                            }
                            response.UPSServices = ltlServices.ToArray();
                            #endregion
                        }
                        catch (Exception parseEx)
                        {
                            // A parse failure is still a failed call worth logging - don't let it
                            // propagate and abort logging for the remaining plants in this loop.
                            callSucceeded = false;
                            callErrorMessage = "Parse error: " + parseEx.Message;
                            plantQuotes.Clear();
                        }
                    }

                    CarrierApiLogger.LogApiCall(new CarrierApiCall
                    {
                        CorrelationId = shipment.CorrelationId,
                        IsTest = shipment.IsTest,
                        ApiProvider = "TransportationInsight",
                        ShippingMode = "LTL",
                        Operation = "Quote",
                        PlantCode = plantCode,
                        AccountNumber = shipment.AcctNum,
                        ShipToAddress = shipment.Address,
                        ShipToCity = shipment.City,
                        ShipToState = shipment.State_selection,
                        ShipToZip = shipment.Zip,
                        ShipToCountry = shipment.Country_selection,
                        RequestUrl = transportationInsightUrl,
                        HttpStatusCode = statusCode,
                        IsSuccess = callSucceeded,
                        ErrorMessage = callErrorMessage,
                        RequestBody = _request,
                        ResponseBody = decodedString ?? responseFromServer,
                        RequestedAtUtc = requestedAtUtc,
                        RespondedAtUtc = DateTime.UtcNow,
                        DurationMs = (int)stopwatch.ElapsedMilliseconds,
                        Quotes = plantQuotes
                    });

                    if (!callSucceeded)
                    {
                        // Move on to the next plant code instead of aborting the whole request.
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO
            }
            Console.WriteLine(sbResults.ToString());

            List<RateDetail> r = new List<RateDetail>();
            foreach (var s in response.UPSServices)
            {
                rates.Add(new RateDetail("LTL", s.ServiceName, Decimal.Parse(s.TotalCost.Replace("$","")), "LTL", shipment.PlantId));
            }
            return rates;
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

            #region Build out shipment information.
            Shipment shipment = new Shipment();
            shipment.CorrelationId = this.CorrelationId;
            shipment.IsTest = this.IsTest;
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

            #region Return results
            ShopRateResponse shopRateResponse = new ShopRateResponse();
            UPSRequest uPSRequest = new UPSRequest(shipment, new Plant(shipment.PlantId), UPSRequest.RequestOption.Rate);
            uPSRequest.Response();
            shopRateResponse.UPSServices = uPSRequest.UPSServices;

            if (uPSRequest.UPSServices != null)
            {
                List<CarrierRateQuote> quotes = new List<CarrierRateQuote>();
                foreach (UPSService service in uPSRequest.UPSServices)
                {
                    service.PublishedRate = RateCalculations.CalculateRate(shipment.AcctNum, shipment.PlantId, "UPSGroundFreight", service.PublishedRate, service.NegotiatedRate, shipment.number_of_packages, shipment.package_weight.ToString(), shipment.last_package_weight.ToString()); // Should use CWT not ServiceName for cleanliness.
                    service.PublishedRate = service.PublishedRate.Replace("$", "");
                    decimal totalCharges = Decimal.Parse(service.PublishedRate);
                    RateDetail rateDetail = new RateDetail(shipment.PlantId, "UPSGroundFreight", 1, int.Parse(shipment.billing_weight.ToString()), false, totalCharges, 0, 0, "UPS");
                    rates.Add(rateDetail);

                    quotes.Add(CarrierRateQuoteBuilder.BuildParcelQuote("UPSGroundFreight", totalCharges));
                }
                CarrierApiLogger.LogQuotes(uPSRequest.LastCarrierApiCallId, quotes);
            }
            #endregion
            return (rates);
        }

        private string GetAccessorialsXML(Shipment shipment)
        {
            StringBuilder accessorials = new StringBuilder();

            // NPDEL	Notification Prior to Delivery
            if (shipment.notify_before_delivery) { accessorials.Append("<ServiceFlag code=\"NPDEL\" />"); }
            // LIFT	Liftgate Service
            if (shipment.liftgate_pickup) { accessorials.Append("<ServiceFlag code=\"LIFT\" />"); }
            if (shipment.liftgate_delivery) { accessorials.Append("<ServiceFlag code=\"LIFT\" />"); }
            // LAPD	Limited Access Delivery
            if (shipment.limited_access_pickup) { accessorials.Append("<ServiceFlag code=\"LAPD\" />"); }
            if (shipment.limited_access_delivery) { accessorials.Append("<ServiceFlag code=\"LAPD\" />"); }
            // RESD	Residential Delivery
            if (shipment.residential_pickup) { accessorials.Append("<ServiceFlag code=\"RESD\" />"); }
            if (shipment.residential_delivery) { accessorials.Append("<ServiceFlag code=\"RESD\" />"); }
            // IDEL	Inside Delivery
            if (shipment.inside_pickup) { accessorials.Append("<ServiceFlag code=\"IDEL\" />"); }
            if (shipment.inside_delivery) { accessorials.Append("<ServiceFlag code=\"IDEL\" />"); }
            // ???
            if (shipment.sort_and_segregate) { accessorials.Append("<ServiceFlag code=\"SORT\" />"); }
            // ???
            if (shipment.stopoff_charge) { accessorials.Append("<ServiceFlag code=\"STOPOFF\" />"); } // ? Not sure this is correct based on https://qa-api-docs.mercurygate.net/documentation/standard-data.html

            return accessorials.ToString();
        }

        private string GetLTLAccessorials(Shipment shipment)
        {
            StringBuilder fullList = new StringBuilder();

            if (shipment.notify_before_delivery)
            {
                fullList.Append("NOTIFY-BEFORE-DELIVERY;");
            }
            if (shipment.liftgate_pickup)
            {
                fullList.Append("LIFTGATE-PICKUP;");
            }
            if (shipment.liftgate_delivery)
            {
                fullList.Append("LIFTGATE-DELIVERY;");
            }
            if (shipment.limited_access_pickup)
            {
                fullList.Append("LIMITED-ACCESS-PICKUP;");
            }
            if (shipment.limited_access_delivery)
            {
                fullList.Append("LIMITED-ACCESS-DELIVERY;");
            }
            if (shipment.residential_pickup)
            {
                fullList.Append("RESIDENTIAL-PICKUP;");
            }
            if (shipment.residential_delivery)
            {
                fullList.Append("RESIDENTIAL-DELIVERY;");
            }
            if (shipment.inside_pickup)
            {
                fullList.Append("INSIDE-PICKUP;");
            }
            if (shipment.inside_delivery)
            {
                fullList.Append("INSIDE-DELIVERY;");
            }
            if (shipment.sort_and_segregate)
            {
                fullList.Append("SORT-AND-SEGREGATE;");
            }
            if (shipment.stopoff_charge)
            {
                fullList.Append("STOPOFF-CHARGE;");
            }
            return fullList.ToString();
        }

        private float GetPlantUpcharges(Shipment shipment, String carrier)
        {
            Shipment _shipment = shipment;
            int accountNumber = 0;
            if (Int32.TryParse(shipment.AcctNum, out accountNumber))
            {
                accountNumber = Int32.Parse(shipment.AcctNum);
            }
            Dictionary<string, string> PerPackageCharge = new Dictionary<string, string>();
            SqlConnection sqlConnection = new SqlConnection(Config.ConnString);
            sqlConnection.Open();

            SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = "GetPlantCharges";
            sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
            sqlCommand.Parameters.Add("@Carrier", System.Data.SqlDbType.VarChar, 50).Value = carrier.ToString();
            sqlCommand.Parameters.Add("AcctNumber", System.Data.SqlDbType.Int).Value = accountNumber;

            using (SqlDataReader reader = sqlCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (shipment.PlantId == reader["PlantCode"].ToString())
                    {
                        float pkgcharges = shipment.number_of_packages * float.Parse(reader["PerPackageCharge"].ToString());
                        float shipmentCharge = float.Parse(reader["PerShipmentCharge"].ToString());
                        float serviceCharge = float.Parse(reader["Ground"].ToString());
                        return pkgcharges + shipmentCharge + serviceCharge;
                    }
                }
            }
            return 0;
        }
    }

}
