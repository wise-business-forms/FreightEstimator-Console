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

                foreach (UPSService service in uPSRequest.UPSServices)
                {
                    service.Rate = RateCalculations.CalculateRate(shipment.AcctNum, shipment.PlantId, service.ServiceName, service.Rate, service.CWTRate, shipment.number_of_packages, shipment.package_weight.ToString(), shipment.last_package_weight.ToString()); // Should use CWT not ServiceName for cleanliness.
                    try
                    {
                        RateDetail rateDetail = new RateDetail(shipment.PlantId, service.ServiceName, 1, int.Parse(shipment.billing_weight.ToString()), false, Decimal.Parse(service.Rate.Replace("$", "")), 0, 0, "UPS");
                    
                                    
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
            #endregion
            return (rates);
        }
        
        public List<RateDetail> getLTLRates()
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
                string pickupDate = "";

                try
                {
                    DateTime datePickup = shipment.pick_up_date;
                    pickupDate = datePickup.Year.ToString() + "-" + datePickup.Month.ToString() + "-" + datePickup.Day.ToString();
                }
                catch
                {
                    pickupDate = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString();
                }
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
                    postData.Append("<requestedPickupDate>" + pickupDate + "</requestedPickupDate>");
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

                    string accessorials = GetLTLAccessorials(shipment);
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

            #region Return results
            ShopRateResponse shopRateResponse = new ShopRateResponse();
            UPSRequest uPSRequest = new UPSRequest(shipment, new Plant(shipment.PlantId), UPSRequest.RequestOption.Shop);
            uPSRequest.Response();
            shopRateResponse.UPSServices = uPSRequest.UPSServices;

            foreach (UPSService service in uPSRequest.UPSServices)
            {
                RateDetail rateDetail = new RateDetail(shipment.PlantId, service.ServiceName, 1, int.Parse(shipment.billing_weight.ToString()), false, Decimal.Parse(service.Rate), 0, 0, "UPS");
                rates.Add(rateDetail);
            }
            #endregion

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

            foreach (UPSService service in uPSRequest.UPSServices)
            {
                RateDetail rateDetail = new RateDetail(shipment.PlantId, service.ServiceName, 1, int.Parse(shipment.billing_weight.ToString()), false, Decimal.Parse(service.Rate), 0, 0, "UPS");
                rates.Add(rateDetail);
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
    }

}
