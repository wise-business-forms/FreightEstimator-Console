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
using System.Data.SqlClient;

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

            #region Return results
            if (shipment.number_of_packages != 0)  // Apparently this is a thing. Orders in the queue may not have packages but have weight.

            {
                ShopRateResponse shopRateResponse = new ShopRateResponse();
                UPSRequest uPSRequest = new UPSRequest(shipment, new Plant(shipment.PlantId), UPSRequest.RequestOption.Shop);
                uPSRequest.Response();
                shopRateResponse.UPSServices = uPSRequest.UPSServices;

                DBUtil dBUtil = new DBUtil();
                var charges = dBUtil.getPlantCharges("UPS", 0);
                if (uPSRequest.UPSServices != null)
                {
                    foreach (UPSService service in uPSRequest.UPSServices)
                    {
                        service.Rate = RateCalculations.CalculateRate(shipment.AcctNum, shipment.PlantId, service.ServiceName, service.Rate, service.CWTRate, shipment.number_of_packages, shipment.package_weight.ToString(), shipment.last_package_weight.ToString()); // Should use CWT not ServiceName for cleanliness.
                        try
                        {
                            RateDetail rateDetail = new RateDetail(shipment.PlantId, service.ServiceName, 1, int.Parse(shipment.billing_weight.ToString()), true, Decimal.Parse(service.Rate.Replace("$", "")), 0, 0, "UPS");


                            //RateDetail rateDetail = new RateDetail(shipment.PlantId, service.ServiceName, 1, int.Parse(shipment.billing_weight.ToString()), false, Decimal.Parse(service.Rate), 0, 0, "UPS");
                            foreach (DataRow row in charges.Tables[0].Rows)
                            {
                                if (row["PlantCode"].ToString() == shipment.PlantId)
                                {
                                    // Column names do not match service names.....
                                    var serviceName = string.Empty;
                                    switch (service.ServiceName)
                                    {
                                        case "UPSNextDayAir":
                                            serviceName = "NextDayAir";
                                            break;
                                        case "UPS2ndDayAir":
                                            serviceName = "SecondDayAir";
                                            break;
                                        case "UPSGround":
                                            serviceName = "Ground";
                                            break;
                                        case "UPS3DaySelect":
                                            serviceName = "ThreeDaySelect";
                                            break;
                                        case "NextDayAirSaver":
                                            serviceName = "NextDayAirSaver";
                                            break;
                                        case "NextDayAirEarlyAM":
                                            serviceName = "NextDayAirEarlyAM";
                                            break;
                                        case "SecondDayAirAM":
                                            serviceName = "SecondDayAirAM";
                                            break;
                                        case "UPSSaver":
                                            serviceName = "Saver";
                                            break;
                                    }
                                    rateDetail.serviceDesc = serviceName;
                                    rateDetail.totalCharges += Decimal.Parse(row[serviceName].ToString());
                                };

                            }
                            rates.Add(rateDetail);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                }
            }
            #endregion
            return (rates);
        }
        
        public List<RateDetail> getLTLRates(UpsComm myUPS)
        {
            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();
            ShopRateResponse response = new ShopRateResponse();

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

                string url = Config.TransPlaceUrl;
                string token = Config.TransPlaceToken;
                string fullPostData = "";
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

                string combinedResponses = "";
                List<UPSService> ltlServices = new List<UPSService>();

                foreach (string plantCode in plantCodes)
                {
                    WebRequest request = WebRequest.Create(url + "rate/quote?loginToken=" + token);
                    request.Method = "POST";

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

                    fullPostData += postData;

                    byte[] byteArray = Encoding.UTF8.GetBytes(postData.ToString());
                    // Set the ContentType property of the WebRequest.
                    request.ContentType = "text/xml";
                    // Set the ContentLength property of the WebRequest.
                    request.ContentLength = byteArray.Length;
                    // Get the request stream.
                    Stream dataStream = request.GetRequestStream();
                    // Write the data to the request stream.
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    // Close the Stream object.
                    dataStream.Close();

                    StringBuilder results = new StringBuilder(postData.ToString());
                    results.Append("\n\n\n");
                    results.Append("<br/>" + url + "rate/quote?loginToken=" + token);
                    results.Append("\n\n\n");
                    // Get the response.
                    WebResponse WebResponse = request.GetResponse();
                    // Display the status.
                    //Console.WriteLine(((HttpWebResponse)WebResponse).StatusDescription);
                    // Get the stream containing content returned by the server.
                    dataStream = WebResponse.GetResponseStream();
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();
                    // Display the content.
                    results.Append(responseFromServer);
                    results.Append("\n\n\n");
                    // Clean up the streams.
                    reader.Close();
                    dataStream.Close();
                    WebResponse.Close();

                    combinedResponses += responseFromServer;

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
                        double markupPercentage = 0;
                        double perPackageCharge = dPerPackageChargeLTL[plantCode];
                        double perShipmentCharge = dPerShipmentChargeLTL[plantCode];
                        #endregion

                        results.Append("Cost is " + cost.ToString() + "\n");
                        results.Append("Markup percentage is " + markupPercentage.ToString() + "\n");
                        results.Append("Number of Packages is " + shipment.number_of_packages + "\n");
                        results.Append("Per package charge is " + perPackageCharge.ToString() + "\n");
                        results.Append("Per shipment charge is " + perShipmentCharge.ToString() + "\n");

                        totalCharges = cost;
                        if (dUpchargeLTL[plantCode] > 0) { totalCharges += ((dUpchargeLTL[plantCode] / 100) * cost); }
                        if (perPackageCharge > 0) { totalCharges += (perPackageCharge * shipment.number_of_packages); }
                        totalCharges += perShipmentCharge;

                        results.Append("Calculated total charge is " + totalCharges.ToString() + "\n\n");

                        //if ((carrier != "LTL BENCHMARK") || (Session["DefaultPlant"].ToString() == "POR"))
                        if (carrier != "LTL BENCHMARK")
                        {
                            UPSService service = new UPSService();
                            service.PlantCode = plantCode;
                            service.ServiceName = carrier;
                            service.Rate = cost.ToString();
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
                                Rate = cost.ToString(),
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
                    }
                    #endregion
                }


                #region -- log results --
                try
                {
                    string UserName = "TBD";

                    SqlConnection conlog = new SqlConnection(Config.ConnString);
                    conlog.Open();

                    SqlCommand cmdLog = new SqlCommand();
                    cmdLog.Connection = conlog;
                    cmdLog.CommandType = CommandType.StoredProcedure;
                    cmdLog.CommandText = "LogResultsLTL";

                    SqlParameter pPlantCode = new SqlParameter("@PlantCode", SqlDbType.VarChar, 10);
                    SqlParameter pUserName = new SqlParameter("@UserName", SqlDbType.VarChar, 10);
                    SqlParameter pFullRequest = new SqlParameter("@FullRequest", SqlDbType.NText);
                    SqlParameter pFullResults = new SqlParameter("@FullResults", SqlDbType.NText);
                    SqlParameter pXmlResponse = new SqlParameter("@XmlResponse", SqlDbType.NText);

                    pPlantCode.Value = shipment.PlantId;
                    pUserName.Value = UserName;
                    pFullRequest.Value = shipment.requestMessage;
                    pFullResults.Value = shipment.responseMessage;
                    pXmlResponse.Value = string.Empty;

                    cmdLog.Parameters.Add(pPlantCode);
                    cmdLog.Parameters.Add(pUserName);
                    cmdLog.Parameters.Add(pFullRequest);
                    cmdLog.Parameters.Add(pFullResults);
                    cmdLog.Parameters.Add(pXmlResponse);

                    cmdLog.ExecuteNonQuery();
                    conlog.Close();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                #endregion
            }
            catch (Exception ex)
            {
                //TODO
            }
            Console.WriteLine(sbResults.ToString());

            List<RateDetail> r = new List<RateDetail>();
            foreach(var s in response.UPSServices)
            {
                rates.Add(new RateDetail("LTL", s.ServiceName, Decimal.Parse(s.TotalCost), "LTL"));
            }
                return rates;
        }


        public List<RateDetail> getLTLRates_TransportationInsight(UpsComm myUPS)
        {
            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();
            ShopRateResponse response = new ShopRateResponse();

            string _request = String.Empty;
            string _response = String.Empty;

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

                string fullPostData = "";
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

                string combinedResponses = "";
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
                    postData.Append("<Constraints><PaymentTerms>Prepaid</PaymentTerms><ServiceFlags /></Constraints>");
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


                    fullPostData += postDataCred;
                    _request = postData.ToString();

                    WebRequest request = WebRequest.Create(Config.TransportationInsightUrl);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.Method = "POST";

                    byte[] byteArray = Encoding.UTF8.GetBytes(postDataCred.ToString());
                    request.ContentLength = byteArray.Length;

                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    StringBuilder results = new StringBuilder(postDataCred.ToString());
                    results.Append("\n\n\n");
                    results.Append("<br/>" + Config.TransportationInsightUrl);
                    results.Append("\n\n\n");
                    // Get the response.
                    WebResponse WebResponse = request.GetResponse();
                    // Display the status.
                    //Console.WriteLine(((HttpWebResponse)WebResponse).StatusDescription);
                    // Get the stream containing content returned by the server.
                    dataStream = WebResponse.GetResponseStream();
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();
                    // Display the content.
                    results.Append(responseFromServer);
                    results.Append("\n\n\n");
                    // Clean up the streams.
                    reader.Close();
                    dataStream.Close();
                    WebResponse.Close();

                    combinedResponses += responseFromServer;


                    #region Parse response
                    // Decode response.
                    XDocument xmlDoc = XDocument.Parse(responseFromServer);
                    string encodedData = xmlDoc.Descendants("data").FirstOrDefault()?.Value;
                    byte[] data = Convert.FromBase64String(encodedData);
                    string decodedString = System.Text.Encoding.UTF8.GetString(data);
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
                        service.Rate = priceSheet.Rate;
                        service.TotalCost = _totalCost.ToString("C");
                        service.TransitDays = priceSheet.TransitDays;
                        service.Direct = priceSheet.Direct;

                        ltlServices.Add(service);
                    }
                    response.UPSServices = ltlServices.ToArray();
                    #endregion
                }


                #region -- log results --
                try
                {
                    string UserName = "TI";

                    SqlConnection conlog = new SqlConnection(Config.ConnString);
                    conlog.Open();

                    SqlCommand cmdLog = new SqlCommand();
                    cmdLog.Connection = conlog;
                    cmdLog.CommandType = CommandType.StoredProcedure;
                    cmdLog.CommandText = "LogResultsLTL";

                    SqlParameter pPlantCode = new SqlParameter("@PlantCode", SqlDbType.VarChar, 10);
                    SqlParameter pUserName = new SqlParameter("@UserName", SqlDbType.VarChar, 10);
                    SqlParameter pFullRequest = new SqlParameter("@FullRequest", SqlDbType.NText);
                    SqlParameter pFullResults = new SqlParameter("@FullResults", SqlDbType.NText);
                    SqlParameter pXmlResponse = new SqlParameter("@XmlResponse", SqlDbType.NText);

                    pPlantCode.Value = shipment.PlantId;
                    pUserName.Value = UserName;
                    pFullRequest.Value = _request;
                    pFullResults.Value = _response;
                    pXmlResponse.Value = _response;

                    cmdLog.Parameters.Add(pPlantCode);
                    cmdLog.Parameters.Add(pUserName);
                    cmdLog.Parameters.Add(pFullRequest);
                    cmdLog.Parameters.Add(pFullResults);
                    cmdLog.Parameters.Add(pXmlResponse);

                    cmdLog.ExecuteNonQuery();
                    conlog.Close();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                #endregion
            }
            catch (Exception ex)
            {
                //TODO
            }
            Console.WriteLine(sbResults.ToString());

            List<RateDetail> r = new List<RateDetail>();
            foreach (var s in response.UPSServices)
            {
                rates.Add(new RateDetail("LTL", s.ServiceName, Decimal.Parse(s.TotalCost.Replace("$","")), "LTL"));
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
                foreach (UPSService service in uPSRequest.UPSServices)
                {
                    service.Rate = RateCalculations.CalculateRate(shipment.AcctNum, shipment.PlantId, "UPSGroundFreight", service.Rate, service.CWTRate, shipment.number_of_packages, shipment.package_weight.ToString(), shipment.last_package_weight.ToString()); // Should use CWT not ServiceName for cleanliness.
                    service.Rate = service.Rate.Replace("$", "");
                    RateDetail rateDetail = new RateDetail(shipment.PlantId, "UPSGroundFreight", 1, int.Parse(shipment.billing_weight.ToString()), false, Decimal.Parse(service.Rate), 0, 0, "UPS");
                    rates.Add(rateDetail);
                }
            }
            #endregion
            return (rates);
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
