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
            
            try
            {
                #region -- Initialize the XAV Service and Request objects --
                XAVService xavService = new XAVService();
                XAVRequest xavRequest = new XAVRequest();

                UpsAddressValidationWebReference.RequestType request = new UpsAddressValidationWebReference.RequestType();
                String[] requestOption = { "3" };
                request.RequestOption = requestOption;
                xavRequest.Request = request;
                #endregion

                #region -- Access Security (license number, username, password) --
                UpsAddressValidationWebReference.UPSSecurity upss = new UpsAddressValidationWebReference.UPSSecurity();
                UpsAddressValidationWebReference.UPSSecurityServiceAccessToken upsSvcToken = new UpsAddressValidationWebReference.UPSSecurityServiceAccessToken();
                upsSvcToken.AccessLicenseNumber = Config.UPSAccessKey;
                upss.ServiceAccessToken = upsSvcToken;
                UpsAddressValidationWebReference.UPSSecurityUsernameToken upsSecUsrnameToken = new UpsAddressValidationWebReference.UPSSecurityUsernameToken();
                upsSecUsrnameToken.Username = Config.UPSUserName;
                upsSecUsrnameToken.Password = Config.UPSPassword;
                upss.UsernameToken = upsSecUsrnameToken;
                xavService.UPSSecurityValue = upss;
                #endregion

                #region -- Add the entered Address data to the request --
                string valAddress = toValidate.street;
                string valCity = toValidate.city;
                string valState = toValidate.state;
                string valZip = toValidate.zip;
                string valCountry = toValidate.country;

                sbResults.AppendLine("Address: " + valAddress);
                sbResults.AppendLine("City: " + valCity);
                sbResults.AppendLine("State: " + valState);
                sbResults.AppendLine("Zip: " + valZip);
                sbResults.AppendLine("Country: " + valCountry);

                AddressKeyFormatType addressKeyFormat = new AddressKeyFormatType();
                String[] addressLine = { valAddress };
                addressKeyFormat.AddressLine = addressLine;
                addressKeyFormat.PoliticalDivision2 = valCity;
                addressKeyFormat.PoliticalDivision1 = valState;
                addressKeyFormat.PostcodePrimaryLow = valZip;
                addressKeyFormat.CountryCode = valCountry;
                xavRequest.AddressKeyFormat = addressKeyFormat;
                #endregion


                #region -- Post our request to UPS Online Tools and capture the response --
                XAVResponse xavResponse = xavService.ProcessXAV(xavRequest);
                
                string requestXml = SoapTrace.TraceExtension.XmlRequest.OuterXml.ToString();
                string responseXml = SoapTrace.TraceExtension.XmlResponse.OuterXml.ToString();
                #endregion

                #region -- Log our request to the DB --
                string targetUrl = xavService.Url;

                #endregion

                if (xavResponse.Response.Alert != null)
                {
                    UpsAddressValidationWebReference.CodeDescriptionType[] alerts = xavResponse.Response.Alert;
                    foreach (UpsAddressValidationWebReference.CodeDescriptionType alert in alerts)
                    {
                        sbResults.AppendLine("Alert: " + alert.Code + " - " + alert.Description);
                    }
                }

                string addressClassCode = xavResponse.AddressClassification.Code;

                sbResults.AppendLine("Address Classification: " + addressClassCode + " - " + xavResponse.AddressClassification.Description);

                int numCandidates = 0;
                string candidateClassification = "";
                string candidateAttention = "";
                string candidateConsignee = "";
                string candidateAddress = "";
                string candidateCity = "";
                string candidateState = "";
                string candidateZip = "";
                /*
                DataSet dsCandidates = new DataSet();
                DataTable tblCandidates = dsCandidates.Tables.Add();

                tblCandidates.Columns.Add("ID", typeof(int));
                tblCandidates.Columns.Add("Attention", typeof(string));
                tblCandidates.Columns.Add("Consignee", typeof(string));
                tblCandidates.Columns.Add("AddressLine", typeof(string));
                tblCandidates.Columns.Add("City", typeof(string));
                tblCandidates.Columns.Add("State", typeof(string));
                tblCandidates.Columns.Add("Zip", typeof(string));
                tblCandidates.Columns.Add("DisplayAddress", typeof(string));
                */
                /*
                DataSet dsCandidateData = new DataSet();
                DataTable tblCandidateData = dsCandidateData.Tables.Add();
                tblCandidateData.Columns.Add("ID", typeof(int));
                tblCandidateData.Columns.Add("Attention", typeof(string));
                tblCandidateData.Columns.Add("Consignee", typeof(string));
                tblCandidateData.Columns.Add("AddressLine", typeof(string));
                tblCandidateData.Columns.Add("City", typeof(string));
                tblCandidateData.Columns.Add("State", typeof(string));
                tblCandidateData.Columns.Add("Zip", typeof(string));
                */

                if (xavResponse.Candidate != null)
                {
                    CandidateType[] candidates = xavResponse.Candidate;
                    numCandidates = candidates.Count();
                    sbResults.AppendLine("Number of Candidates: " + numCandidates);
                    int candidateCount = 0;
                    foreach (CandidateType candidate in candidates)
                    {

                        Address currentCandidate = new Address();

                        candidateCount++;

                        candidateClassification = candidate.AddressClassification.Code;

                        sbResults.AppendLine("Candidate #" + candidateCount.ToString() + " Classification: " + candidateClassification + " - " + candidate.AddressClassification.Description);

                        if (candidate.AddressKeyFormat.AttentionName != null)
                        {
                            candidateAttention = candidate.AddressKeyFormat.AttentionName;
                            sbResults.AppendLine("Attention: " + candidateAttention);
                        }
                        else { candidateAttention = ""; }

                        if (candidate.AddressKeyFormat.ConsigneeName != null)
                        {
                            candidateConsignee = candidate.AddressKeyFormat.ConsigneeName;
                            sbResults.AppendLine("Consignee: " + candidateConsignee);
                        }
                        else { candidateConsignee = ""; }

                        candidateAddress = "";
                        for (int i = 0; i < candidate.AddressKeyFormat.AddressLine.Count(); i++)
                        {
                            candidateAddress += candidate.AddressKeyFormat.AddressLine[i];
                            if ((i + 1) < candidate.AddressKeyFormat.AddressLine.Count())
                            {
                                candidateAddress += " ";
                            }
                            sbResults.AppendLine(candidate.AddressKeyFormat.AddressLine[i]);
                        }

                        candidateCity = candidate.AddressKeyFormat.PoliticalDivision2;
                        candidateState = candidate.AddressKeyFormat.PoliticalDivision1;
                        candidateZip = candidate.AddressKeyFormat.PostcodePrimaryLow + "-" + candidate.AddressKeyFormat.PostcodeExtendedLow;

                        sbResults.AppendLine(candidateCity + " " + candidateState + " " + candidateZip);

                        currentCandidate.street = candidateAddress;
                        currentCandidate.city = candidateCity;
                        currentCandidate.state = candidateState;
                        currentCandidate.zip = candidateZip;
                        currentCandidate.country = toValidate.country;

                        addressCandidates.Add(currentCandidate);

                        /*
                        string displayAddress = "";
                        if (candidateAttention != "")
                        {
                            displayAddress += candidateAttention + "\n";
                        }
                        if (candidateConsignee != "")
                        {
                            displayAddress += candidateConsignee + "\n";
                        }
                        displayAddress += candidateAddress + "\n";
                        displayAddress += candidateCity + " " + candidateState + " " + candidateZip;
                        */


                        //tblCandidates.Rows.Add(candidateCount, candidateAttention, candidateConsignee, candidateAddress, candidateCity, candidateState, candidateZip, displayAddress);
                        //tblCandidateData.Rows.Add(candidateCount, candidateAttention, candidateConsignee, candidateAddress, candidateCity, candidateState, candidateZip);


                        sbResults.AppendLine();
                    } // end of foreach candidate loop
                }

                #region unusedOldLogic
                /*
                if (numCandidates == 1)
                {
                    string selectedState = GetSelectedState();

                    if ((candidateAddress.ToUpper() != txtAddress.Text.Trim().ToUpper())
                     || (candidateCity.ToUpper() != txtCity.Text.Trim().ToUpper())
                     || (candidateState.ToUpper() != selectedState.ToUpper())
                     || (candidateZip.ToUpper() != txtZip.Text.Trim().ToUpper()))
                    {
                        bool AutoAcceptAddressCorrections = true;

                        lblEnteredAddressStreet.Text = txtAddress.Text.Trim().ToUpper();
                        lblEnteredAddressCity.Text = txtCity.Text.Trim().ToUpper();
                        lblEnteredAddressState.Text = selectedState.ToUpper();
                        lblEnteredAddressZip.Text = txtZip.Text.Trim().ToUpper();

                        lblCorrectedAddressStreet.Text = candidateAddress.ToUpper();
                        lblCorrectedAddressCity.Text = candidateCity.ToUpper();
                        lblCorrectedAddressState.Text = candidateState.ToUpper();
                        lblCorrectedAddressZip.Text = candidateZip.ToUpper();

                        lblChangeToAddressStreet.Visible = (lblEnteredAddressStreet.Text != lblCorrectedAddressStreet.Text);
                        lblChangeToAddressCity.Visible = (lblEnteredAddressCity.Text != lblCorrectedAddressCity.Text);
                        lblChangeToAddressState.Visible = (lblEnteredAddressState.Text != lblCorrectedAddressState.Text);
                        lblChangeToAddressZip.Visible = (lblEnteredAddressZip.Text != lblCorrectedAddressZip.Text);

                        if (AutoAcceptAddressCorrections)
                        {
                            sbResults.Append("  -- not an exact match, but auto-accepting UPS address correction <BR/>");
                            RateWithCorrectedAddress();
                        }
                        else
                        {
                            mvPageLayout.SetActiveView(vwVerifyAddressCorrection);
                            pnlUpsTrademarkInfo.Visible = true;
                        }
                    }
                    else // Our address exactly matches the UPS candidate, so move forward
                    {
                        sbResults.Append(" -- EXACT MATCH -- Continuing to Rating -- ");

                        RateWithEnteredAddress();

                    }
                } // end of numCandidates == 1 logic
                else if (numCandidates > 1)
                {

                    string addressedSubmitted = txtAddress.Text.Trim() + "<br/>";
                    addressedSubmitted += txtCity.Text.Trim() + " " + txtState.Text.Trim() + " " + txtZip.Text.Trim();
                    lblAddressSubmitted.Text = addressedSubmitted.ToUpper();

                    DataView dvCandidates = tblCandidates.DefaultView;
                    dvCandidates.Sort = "ID";

                    gvCandidates.DataSource = dvCandidates;
                    gvCandidates.DataBind();

                    gvCandidateData.DataSource = dvCandidates;
                    gvCandidateData.DataBind();

                    mvPageLayout.SetActiveView(vwSelectAddressCandidate);
                    pnlUpsTrademarkInfo.Visible = false;
                }
                */
                #endregion

                //Console.WriteLine(requestXml);
                //Console.WriteLine(responseXml);

            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                //WRITE ERROR TO REQUEST
                handleError("ValidateAndRateAddress", "Soap Exception - SoapException Message= " + ex.Message + " SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText + " SoapException XML String for all= " + ex.Detail.LastChild.OuterXml + " SoapException StackTrace= " + ex.StackTrace);
                #region -- Handle SOAP error --
                sbResults.AppendLine("---------XAV Web Service returns error----------------");
                sbResults.AppendLine("---------\"Hard\" is user error \"Transient\" is system error----------------");
                sbResults.AppendLine("SoapException Message= " + ex.Message);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException XML String for all= " + ex.Detail.LastChild.OuterXml);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException StackTrace= " + ex.StackTrace);
                sbResults.AppendLine("-------------------------");
                #endregion
            }
            catch (Exception ex)
            {
                //WRITE ERROR TO REQUEST
                handleError("ValidateAndRateAddress", "General Error - General Exception= " + ex.Message + " General Exception-StackTrace= " + ex.StackTrace);
                #region -- Handle misc error --
                sbResults.AppendLine();
                sbResults.AppendLine("-------------------------");
                sbResults.AppendLine(" General Exception= " + ex.Message);
                sbResults.AppendLine(" General Exception-StackTrace= " + ex.StackTrace);
                sbResults.AppendLine("-------------------------");
                #endregion
            }

            Console.WriteLine(sbResults.ToString());
            return (addressCandidates);
        }

        public List<RateDetail> getRates()
        {
            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();

            bool SoapError = false;

            double shipmentWeight = 0;
            int shipmentClassification = 0;

            #region -- Define Services dictionary (mapping codes to descriptions) --
            Dictionary<string, string> dServices = new Dictionary<string, string>();
            dServices.Add("01", "Next Day Air");
            dServices.Add("02", "Second Day Air");
            dServices.Add("03", "Ground");
            dServices.Add("12", "Three-Day Select");
            dServices.Add("13", "Next Day Air Saver");
            dServices.Add("14", "Next Day Air Early A.M.");
            dServices.Add("59", "Second Day Air A.M.");
            dServices.Add("65", "Saver");
            #endregion

            #region -- Define Service CWT Types dictionary (mapping codes to Ground, Air, or Neither) --
            Dictionary<string, string> dServiceTypes = new Dictionary<string, string>();
            dServiceTypes.Add("01", "AIR");
            dServiceTypes.Add("02", "AIR");
            dServiceTypes.Add("03", "GROUND");
            dServiceTypes.Add("12", "GROUND");
            dServiceTypes.Add("13", "AIR");
            dServiceTypes.Add("14", "AIR-NN"); //means AIR - No Negotiated Rate - on this service, we ignore negotiated rate for CWT as it is not allowed
            dServiceTypes.Add("59", "AIR");
            dServiceTypes.Add("65", "GROUND");
            #endregion

            #region -- Define Per Package Charge and Per Shipment Charge dictionaries standard (charge per plant) --
            Dictionary<string, double> dPerPackageChargeUPS = new Dictionary<string, double>();
            Dictionary<string, double> dPerShipmentChargeUPS = new Dictionary<string, double>();

            Dictionary<string, double> dUpchargeNextDayAirUPS = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSecondDayAirUPS = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeGroundUPS = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeThreeDaySelectUPS = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeNextDayAirSaverUPS = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeNextDayAirEarlyAMUPS = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSecondDayAirAMUPS = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSaverUPS = new Dictionary<string, double>();

            DBUtil db = new DBUtil();

            DataSet ds = db.getPlantCharges("UPS", acctNumber);
            
            foreach (DataRow drCharges in ds.Tables[0].Rows)
            {
                dPerPackageChargeUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                dPerShipmentChargeUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));

                dUpchargeNextDayAirUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAir"].ToString()));
                dUpchargeSecondDayAirUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["SecondDayAir"].ToString()));
                dUpchargeGroundUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
                dUpchargeThreeDaySelectUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["ThreeDaySelect"].ToString()));
                dUpchargeNextDayAirSaverUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAirSaver"].ToString()));
                dUpchargeNextDayAirEarlyAMUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAirEarlyAM"].ToString()));
                dUpchargeSecondDayAirAMUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["SecondDayAirAM"].ToString()));
                dUpchargeSaverUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Saver"].ToString()));
            }

            #endregion

            #region -- Define Per Package Charge and Per Shipment Charge dictionaries HundredWeight (charge per plant) --
            Dictionary<string, double> dPerPackageChargeCWT = new Dictionary<string, double>();
            Dictionary<string, double> dPerShipmentChargeCWT = new Dictionary<string, double>();

            Dictionary<string, double> dUpchargeNextDayAirCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSecondDayAirCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeGroundCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeThreeDaySelectCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeNextDayAirSaverCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeNextDayAirEarlyAMCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSecondDayAirAMCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSaverCWT = new Dictionary<string, double>();

            ds = db.getPlantCharges("UPSCWT", acctNumber);
            
            foreach (DataRow drCharges in ds.Tables[0].Rows)
            {
                dPerPackageChargeCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                dPerShipmentChargeCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));

                dUpchargeNextDayAirCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAir"].ToString()));
                dUpchargeSecondDayAirCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["SecondDayAir"].ToString()));
                dUpchargeGroundCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
                dUpchargeThreeDaySelectCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["ThreeDaySelect"].ToString()));
                dUpchargeNextDayAirSaverCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAirSaver"].ToString()));
                dUpchargeNextDayAirEarlyAMCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAirEarlyAM"].ToString()));
                dUpchargeSecondDayAirAMCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["SecondDayAirAM"].ToString()));
                dUpchargeSaverCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Saver"].ToString()));
            }

            #endregion

            #region -- Calculate HundredWeight eligibility --
            if (lastPkgWeight == 0)
            {
                lastPkgWeight = pkgWeight;
            }

            int totalWeight = 0;

            if (packageWeights.Count == 0)
            {
                totalWeight = (pkgWeight * (numPackages - 1)) + lastPkgWeight;
            }
            else
            {
                foreach(int pkgWgt in packageWeights)
                {
                    totalWeight += pkgWgt;
                }
            }

            bool isAirCWT = (numPackages >= Config.MinCWTPackagesAir) && (totalWeight >= Config.MinCWTWeightAir);
            bool isGroundCWT = (numPackages >= Config.MinCWTPackagesGround) && (totalWeight >= Config.MinCWTWeightGround);

            sbResults.AppendLine("Total weight: " + totalWeight.ToString() + ", # Pkgs: " + numPackages.ToString());
            sbResults.AppendLine("Qualifies for CWT Air? " + isAirCWT.ToString());
            sbResults.AppendLine("Qualifies for CWT Grd? " + isGroundCWT.ToString());
            #endregion

            #region -- Process rate request for a single plant --
            try
            {

                int RateRequestId = 0;

                #region -- Load Ship From Address --
                string shipFromAddress = fromRate.street;
                string shipFromCity = fromRate.city;
                string shipFromState = fromRate.state;
                string shipFromZip = fromRate.zip;
                string shipFromCountry = fromRate.country;

                #endregion

                #region -- Begin building Rate Request --
                RateService rateService = new RateService();
                RateRequest rateRequest = new RateRequest();

                UpsRateWebReference.RequestType request = new UpsRateWebReference.RequestType();
                String[] requestOption = { "Shop" };
                request.RequestOption = requestOption;
                rateRequest.Request = request;
                #endregion

                #region -- Access Security (license number, username, password) --
                UpsRateWebReference.UPSSecurity upss = new UpsRateWebReference.UPSSecurity();
                UpsRateWebReference.UPSSecurityServiceAccessToken upsSvcToken = new UpsRateWebReference.UPSSecurityServiceAccessToken();
                upsSvcToken.AccessLicenseNumber = Config.UPSAccessKey;
                upss.ServiceAccessToken = upsSvcToken;
                UpsRateWebReference.UPSSecurityUsernameToken upsSecUsrnameToken = new UpsRateWebReference.UPSSecurityUsernameToken();
                upsSecUsrnameToken.Username = Config.UPSUserName;
                upsSecUsrnameToken.Password = Config.UPSPassword;
                upss.UsernameToken = upsSecUsrnameToken;
                rateService.UPSSecurityValue = upss;
                #endregion

                #region -- Build Shipment object --
                ShipmentType shipment = new ShipmentType();

                #region -- Shipper --
                ShipperType shipper = new ShipperType();
                shipper.ShipperNumber = Config.ShipFromShipperNumber;
                AddressType shipperAddress = new AddressType();
                String[] shipperAddressLine = { shipFromAddress };
                shipperAddress.AddressLine = shipperAddressLine;
                shipperAddress.City = shipFromCity;
                shipperAddress.StateProvinceCode = shipFromState;
                shipperAddress.PostalCode = shipFromZip;
                shipperAddress.CountryCode = shipFromCountry;
                shipper.Address = shipperAddress;

                shipment.Shipper = shipper;
                #endregion

                #region -- Ship To --
                ShipToType shipTo = new ShipToType();
                ShipToAddressType shipToAddress = new ShipToAddressType();
                String[] shipToAddressLines = { toRate.street };
                shipToAddress.AddressLine = shipToAddressLines;
                shipToAddress.City = toRate.city;
                shipToAddress.StateProvinceCode = toRate.state;
                shipToAddress.PostalCode = toRate.zip;
                shipToAddress.CountryCode = toRate.country;

                shipTo.Address = shipToAddress;

                shipment.ShipTo = shipTo;

                #endregion

                #region -- Packages --
                PackageType package;
                PackageWeightType packageWeight;
                UpsRateWebReference.CodeDescriptionType uomCodeDesc;

                UpsRateWebReference.CodeDescriptionType packageTypeCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                packageTypeCodeDesc.Code = "00";

                string deliveryConfirmationOption = deliveryConfCode.ToString();

                PackageType[] packages = new PackageType[numPackages];

                if (packageWeights.Count == 0)
                {
                    for (int i = 0; i < numPackages; i++)
                    {
                        package = new PackageType();
                        packageWeight = new PackageWeightType();
                        uomCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                        uomCodeDesc.Code = "LBS";
                        if (i == (numPackages - 1))
                        {
                            packageWeight.Weight = lastPkgWeight.ToString();
                        }
                        else
                        {
                            packageWeight.Weight = pkgWeight.ToString();
                        }
                        packageWeight.UnitOfMeasurement = uomCodeDesc;
                        package.PackageWeight = packageWeight;

                        package.PackagingType = packageTypeCodeDesc;

                        if (deliveryConfirmationOption != "0")
                        {
                            PackageServiceOptionsType packageServiceOptions = new PackageServiceOptionsType();
                            DeliveryConfirmationType deliveryConfirmation = new DeliveryConfirmationType();
                            deliveryConfirmation.DCISType = deliveryConfirmationOption;
                            packageServiceOptions.DeliveryConfirmation = deliveryConfirmation;
                            package.PackageServiceOptions = packageServiceOptions;
                        }

                        packages[i] = package;
                    }
                }
                else
                {
                    int i = 0;
                    foreach(int pkgWgt in packageWeights)
                    {
                        package = new PackageType();
                        packageWeight = new PackageWeightType();
                        uomCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                        uomCodeDesc.Code = "LBS";

                        packageWeight.Weight = pkgWgt.ToString();
                        packageWeight.UnitOfMeasurement = uomCodeDesc;
                        package.PackageWeight = packageWeight;

                        package.PackagingType = packageTypeCodeDesc;

                        if (deliveryConfirmationOption != "0")
                        {
                            PackageServiceOptionsType packageServiceOptions = new PackageServiceOptionsType();
                            DeliveryConfirmationType deliveryConfirmation = new DeliveryConfirmationType();
                            deliveryConfirmation.DCISType = deliveryConfirmationOption;
                            packageServiceOptions.DeliveryConfirmation = deliveryConfirmation;
                            package.PackageServiceOptions = packageServiceOptions;
                        }

                        packages[i] = package;

                        i++;
                    }
                }

                shipment.Package = packages;
                #endregion

                #region -- Negotiated Rates Indicator (if needed) --
                if (isAirCWT || isGroundCWT)
                {
                    ShipmentRatingOptionsType ratingOptions = new ShipmentRatingOptionsType();
                    ratingOptions.NegotiatedRatesIndicator = "";
                    shipment.ShipmentRatingOptions = ratingOptions;
                }
                #endregion

                rateRequest.Shipment = shipment;

                #endregion

                #region -- Submit Rate Request --

                RateResponse rateResponse = rateService.ProcessRate(rateRequest);

                RatedShipmentType[] ratedShipments = rateResponse.RatedShipment;

                string requestXml = SoapTrace.TraceExtension.XmlRequest.OuterXml.ToString();
                string responseXml = SoapTrace.TraceExtension.XmlResponse.OuterXml.ToString();

                string targetUrl = rateService.Url;
                //string UserName = HttpContext.Current.User.Identity.Name.Replace(Config.NetworkDomain + "\\", "").ToLower();


                #region -- log request --
                /*
                try
                {
                    SqlConnection connLog = new SqlConnection(ConfigurationManager.ConnectionStrings["UpsRateSqlConnection"].ConnectionString);
                    connLog.Open();

                    SqlCommand cmdLog = new SqlCommand();
                    cmdLog.Connection = connLog;
                    cmdLog.CommandType = CommandType.StoredProcedure;
                    cmdLog.CommandText = "LogRequest_Rate";

                    SqlParameter pUserName = new SqlParameter("@UserName", SqlDbType.VarChar, 50);
                    SqlParameter pTargetUrl = new SqlParameter("@TargetUrl", SqlDbType.VarChar, 200);
                    SqlParameter pAddress = new SqlParameter("@Address", SqlDbType.VarChar, 200);
                    SqlParameter pCity = new SqlParameter("@City", SqlDbType.VarChar, 200);
                    SqlParameter pState = new SqlParameter("@State", SqlDbType.VarChar, 50);
                    SqlParameter pZip = new SqlParameter("@Zip", SqlDbType.VarChar, 50);
                    SqlParameter pCountry = new SqlParameter("@Country", SqlDbType.VarChar, 2);
                    SqlParameter pRequestXml = new SqlParameter("@RequestXml", SqlDbType.NText);
                    SqlParameter pResponseXml = new SqlParameter("@ResponseXml", SqlDbType.NText);
                    SqlParameter pRequestId = new SqlParameter("@RequestId", SqlDbType.Int);

                    pUserName.Value = UserName;
                    pTargetUrl.Value = targetUrl;
                    pAddress.Value = shipToStreet;
                    pCity.Value = shipToCity;
                    pState.Value = shipToState;
                    pZip.Value = shipToZip;
                    pCountry.Value = shipToCountry;
                    pRequestXml.Value = requestXml;
                    pResponseXml.Value = responseXml;
                    pRequestId.Direction = ParameterDirection.Output;

                    cmdLog.Parameters.Add(pUserName);
                    cmdLog.Parameters.Add(pTargetUrl);
                    cmdLog.Parameters.Add(pAddress);
                    cmdLog.Parameters.Add(pCity);
                    cmdLog.Parameters.Add(pState);
                    cmdLog.Parameters.Add(pZip);
                    cmdLog.Parameters.Add(pCountry);
                    cmdLog.Parameters.Add(pRequestXml);
                    cmdLog.Parameters.Add(pResponseXml);
                    cmdLog.Parameters.Add(pRequestId);

                    cmdLog.ExecuteNonQuery();

                    RateRequestId = Convert.ToInt32(pRequestId.Value);

                    connLog.Close();
                }
                catch (Exception e)
                {
                    handleError("ShowRates", "Error in saving request to DB: " + e.Message);
                    sbResults.Append("Error in saving request to DB: " + e.Message + "<br/>");
                }
                 */
                #endregion

                #endregion

                #region -- Define tblServices to hold rate data --

                DataSet dsServices = new DataSet();
                DataTable tblServices = dsServices.Tables.Add();

                tblServices.Columns.Add("Plant", typeof(string));
                tblServices.Columns.Add("Desc", typeof(string));
                tblServices.Columns.Add("Rate", typeof(double));
                tblServices.Columns.Add("BillingWeight", typeof(double));
                tblServices.Columns.Add("Classification", typeof(int));
                tblServices.Columns.Add("ServiceCharges", typeof(double));
                tblServices.Columns.Add("TransportationCharges", typeof(double));
                tblServices.Columns.Add("IsHundredWeight", typeof(bool));

                #endregion

                #region -- Process each rated service --

                foreach (RatedShipmentType ratedShipment in ratedShipments)
                {
                    string serviceDesc = "";
                    string serviceCWTType = "";
                    string serviceCode = "";
                    int addressClassification = 1;
                    double billingWeight = 0;
                    double serviceCharges = 0;
                    double transportationCharges = 0;
                    double totalCharges = 0;
                    double negotiatedCharges = 0;
                    bool isHundredWeight = false;

                    serviceCode = ratedShipment.Service.Code;

                    if (dServiceTypes.ContainsKey(ratedShipment.Service.Code))
                    {
                        serviceCWTType = dServiceTypes[ratedShipment.Service.Code];
                    }
                    else
                    {
                        serviceCWTType = "";
                    }

                    if (dServices.ContainsKey(ratedShipment.Service.Code))
                    {
                        serviceDesc = dServices[ratedShipment.Service.Code];
                    }
                    else
                    {
                        serviceDesc = "Unknown Service";
                    }

                    billingWeight = Convert.ToDouble(ratedShipment.BillingWeight.Weight);
                    serviceCharges = Convert.ToDouble(ratedShipment.ServiceOptionsCharges.MonetaryValue);
                    transportationCharges = Convert.ToDouble(ratedShipment.TransportationCharges.MonetaryValue);
                    totalCharges = Convert.ToDouble(ratedShipment.TotalCharges.MonetaryValue);

                    sbResults.AppendLine("Service: " + ratedShipment.Service.Code + " - " + serviceDesc);
                    sbResults.AppendLine("Billing Weight: " + billingWeight.ToString());
                    sbResults.AppendLine("Service Options Charges: " + serviceCharges.ToString());
                    sbResults.AppendLine("Transportation Charges: " + transportationCharges.ToString());
                    sbResults.AppendLine("Total charges: " + totalCharges.ToString());

                    if (ratedShipment.GuaranteedDelivery != null)
                    {
                        sbResults.AppendLine("Guaranteed Delivery / Business Days in Transit: " + ratedShipment.GuaranteedDelivery.BusinessDaysInTransit);
                        sbResults.AppendLine("Guaranteed Delivery / Delivery By Time: " + ratedShipment.GuaranteedDelivery.DeliveryByTime);
                    }
                    if (ratedShipment.NegotiatedRateCharges != null)
                    {
                        negotiatedCharges = Convert.ToDouble(ratedShipment.NegotiatedRateCharges.TotalCharge.MonetaryValue);
                        sbResults.AppendLine("Negotiated Rate Total Charges: " + negotiatedCharges.ToString());
                    }

                    if (ratedShipment.RatedShipmentAlert != null)
                    {
                        RatedShipmentInfoType[] ratedShipmentAlerts = ratedShipment.RatedShipmentAlert;
                        foreach (RatedShipmentInfoType ratedShipmentAlert in ratedShipmentAlerts)
                        {
                            if (ratedShipmentAlert.Code == "110920")
                            {
                                //Then the "Commercial" classification has changed to "Residential" so set classification to 2
                                addressClassification = 2;
                            }
                            sbResults.AppendLine("Shipment Alert: " + ratedShipmentAlert.Code + " - " + ratedShipmentAlert.Description);
                        }
                    }

                    sbResults.AppendLine();

                    #region -- Define variables for markup calculations --
                    double markupPercentage = 0;
                    double perPackageCharge = 0;
                    double perShipmentCharge = 0;

                    double markupPercentageUPS = 0;
                    double perPackageChargeUPS = 0;
                    double perShipmentChargeUPS = 0;

                    double markupPercentageCWT = 0;
                    double perPackageChargeCWT = 0;
                    double perShipmentChargeCWT = 0;
                    #endregion

                    #region -- Determine the UPS Standard markup percentage --
                    if (dPerPackageChargeUPS.ContainsKey(plantCode))
                    {
                        switch (serviceCode)
                        {
                            case "01":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeNextDayAirUPS[plantCode]);
                                break;
                            case "02":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeSecondDayAirUPS[plantCode]);
                                break;
                            case "03":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeGroundUPS[plantCode]);
                                break;
                            case "12":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeThreeDaySelectUPS[plantCode]);
                                break;
                            case "13":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeNextDayAirSaverUPS[plantCode]);
                                break;
                            case "14":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeNextDayAirEarlyAMUPS[plantCode]);
                                break;
                            case "59":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeSecondDayAirAMUPS[plantCode]);
                                break;
                            case "65":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeSaverUPS[plantCode]);
                                break;
                        }

                        perPackageChargeUPS = dPerPackageChargeUPS[plantCode];
                        perShipmentChargeUPS = dPerShipmentChargeUPS[plantCode];
                    }
                    else
                    {
                        sbResults.AppendLine("Unable to find UPS Standard markup charges for plant code: " + plantCode);
                    }
                    #endregion

                    #region -- Determine the HundredWeight markup percentage --
                    if (dPerPackageChargeUPS.ContainsKey(plantCode))
                    {
                        switch (serviceCode)
                        {
                            case "01":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeNextDayAirUPS[plantCode]);
                                break;
                            case "02":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeSecondDayAirUPS[plantCode]);
                                break;
                            case "03":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeGroundUPS[plantCode]);
                                break;
                            case "12":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeThreeDaySelectUPS[plantCode]);
                                break;
                            case "13":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeNextDayAirSaverUPS[plantCode]);
                                break;
                            case "14":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeNextDayAirEarlyAMUPS[plantCode]);
                                break;
                            case "59":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeSecondDayAirAMUPS[plantCode]);
                                break;
                            case "65":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeSaverUPS[plantCode]);
                                break;
                        }

                        perPackageChargeCWT = dPerPackageChargeCWT[plantCode];
                        perShipmentChargeCWT = dPerShipmentChargeCWT[plantCode];
                    }
                    else
                    {
                        sbResults.AppendLine("Unable to find HundredWeight markup charges for plant code: " + plantCode);
                    }
                    #endregion

                    //if shipment qualifies for hundredweight air and hundredweight method class is air, consider it hundredweight
                    //if shipment qualifies for hundredweight ground and hundredweight method class is ground, consider it hundredweight

                    if (isAirCWT && (serviceCWTType == "AIR-NN"))
                    {
                        isHundredWeight = true;
                        //Do not adjust total charges
                    }
                    else if ((isAirCWT && (serviceCWTType == "AIR")) || (isGroundCWT && (serviceCWTType == "GROUND")))
                    {
                        isHundredWeight = true;

                        // Since our negotiated CWT rates is 70% of the published rate, reverse that and divide neg. rate by .7 to get published rate
                        totalCharges = negotiatedCharges / .7;
                    }
                    else
                    {
                        isHundredWeight = false;
                    }

                    if (isHundredWeight)
                    {
                        markupPercentage = markupPercentageCWT;
                        perPackageCharge = perPackageChargeCWT;
                        perShipmentCharge = perShipmentChargeCWT;
                    }
                    else
                    {
                        markupPercentage = markupPercentageUPS;
                        perPackageCharge = perPackageChargeUPS;
                        perShipmentCharge = perShipmentChargeUPS;
                    }

                    totalCharges += ((markupPercentage / 100) * totalCharges);
                    totalCharges += (perPackageCharge * numPackages) + perShipmentCharge;

                    //tblServices.Rows.Add(plantCode, serviceDesc, totalCharges, billingWeight, addressClassification, serviceCharges, transportationCharges, isHundredWeight);
                    RateDetail currentRate = new RateDetail(plantCode, serviceDesc, addressClassification, int.Parse(billingWeight.ToString()), isHundredWeight, decimal.Parse(totalCharges.ToString()), decimal.Parse(serviceCharges.ToString()), decimal.Parse(transportationCharges.ToString()), "UPS");
                    rates.Add(currentRate);

                    shipmentWeight = billingWeight;
                    shipmentClassification = addressClassification;
                }
                #endregion

                
                #region -- log results --
                /*
                try
                {
                    System.IO.StringWriter swGridResults = new System.IO.StringWriter();

                    System.Web.UI.HtmlTextWriter htwGridResults = new HtmlTextWriter(swGridResults);

                    gvServices.RenderControl(htwGridResults);

                    SqlConnection connLog = new SqlConnection(ConfigurationManager.ConnectionStrings["UpsRateSqlConnection"].ConnectionString);
                    connLog.Open();

                    SqlCommand cmdLog = new SqlCommand();
                    cmdLog.Connection = connLog;
                    cmdLog.CommandType = CommandType.StoredProcedure;
                    cmdLog.CommandText = "LogResults";

                    SqlParameter pRequestId = new SqlParameter("@RequestId", SqlDbType.Int);
                    SqlParameter pPlantCode = new SqlParameter("@PlantCode", SqlDbType.VarChar, 10);
                    SqlParameter pFullResults = new SqlParameter("@FullResults", SqlDbType.NText);

                    pRequestId.Value = RateRequestId;
                    pPlantCode.Value = plantCode;
                    pFullResults.Value = swGridResults.ToString();

                    cmdLog.Parameters.Add(pRequestId);
                    cmdLog.Parameters.Add(pPlantCode);
                    cmdLog.Parameters.Add(pFullResults);

                    cmdLog.ExecuteNonQuery();
                    connLog.Close();
                }
                catch (Exception e)
                {
                    handleError("ShowRates", "Error in saving results to DB: " + e.Message);
                    sbResults.Append("Error in saving results to DB: " + e.Message + "<br/>");
                }
                 */
                #endregion

            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                if ((ex.Detail.LastChild.InnerText != "Hard110003Maximum number of packages exceeded (50)") //Do not show "scary" red message if only soap error is that it exceeds 50 packages (also catch 200 pkg message)
                    && (ex.Detail.LastChild.InnerText != "Hard110003Maximum number of packages exceeded (200)"))
                {
                    //WRITE ERROR TO REQUEST
                    handleError("ShowRates", "Soap Exception - SoapException Message= " + ex.Message + " SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText + " SoapException XML String for all= " + ex.Detail.LastChild.OuterXml + " SoapException StackTrace= " + ex.StackTrace);
                }
                #region -- Handle SOAP error --
                SoapError = true;
                sbResults.AppendLine("---------Freight Rate Web Service returns error----------------");
                sbResults.AppendLine("---------\"Hard\" is user error \"Transient\" is system error----------------");
                sbResults.AppendLine("SoapException Message= " + ex.Message);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException XML String for all= " + ex.Detail.LastChild.OuterXml);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException StackTrace= " + ex.StackTrace);
                sbResults.AppendLine("-------------------------");
                #endregion
            }
                /*
            catch (System.ServiceModel.CommunicationException ex)
            {
                //handleError("ShowRates", "Communication Exception - CommunicationException= " + ex.Message + " CommunicationException-StackTrace= " + ex.StackTrace);
                SoapError = true;
                #region -- Handle General Communication Error --
                sbResults.Append("<br/>");
                sbResults.Append("--------------------<br/>");
                sbResults.Append("CommunicationException= " + ex.Message + "<br/>");
                sbResults.Append("CommunicationException-StackTrace= " + ex.StackTrace + "<br/>");
                sbResults.Append("-------------------------<br/>");
                sbResults.Append("<br/>");
                #endregion
            }*/
            catch (Exception ex)
            {
                //WRITE ERROR TO REQUEST
                handleError("ShowRates", "General Error - General Exception= " + ex.Message + " General Exception-StackTrace= " + ex.StackTrace);
                SoapError = true;
                #region -- Handle misc error --
                sbResults.AppendLine();
                sbResults.AppendLine("-------------------------");
                sbResults.AppendLine(" General Exception= " + ex.Message);
                sbResults.AppendLine(" General Exception-StackTrace= " + ex.StackTrace);
                sbResults.AppendLine("-------------------------");
                #endregion
            }
            
            #endregion

            

            #region -- Display our Rating Results --

            //lblBillingWeight.Text = shipmentWeight.ToString();

            //lblNumPackages.Text = numPackages.ToString();

            //if (shipmentClassification == 2)
            //{
            //    lblAddressClassification.Text = "RESIDENTIAL";
            //}
            //else if (shipmentClassification == 1)
            //{
            //    lblAddressClassification.Text = "COMMERCIAL";
            //}
            //else
            //{
            //    lblAddressClassification.Text = "UNKNOWN";
            //}

            //if (addressCorrected)
            //{
            //   lblRateWarning.Text = "The address was automatically corrected to match what UPS had on file.  Please make note of the changes.<br/>";
            //}
            //else if (shipToStreet == "")
            //{
            //    lblRateWarning.Text = "This is an estimate based on the Zip Code provided.  Please be aware additional charges may apply if the address is residential, in an extended delivery area, etc.<br/>";
            //}
            //else
            //{
            //    lblRateWarning.Text = "";
            //}

            #endregion

            #region -- Show Results View --
            //mvPageLayout.SetActiveView(vwRates);
            //pnlUpsTrademarkInfo.Visible = true;
            #endregion

            Console.WriteLine(sbResults.ToString());
            //lblResults.Text += sbResults.ToString();

            return (rates);
        }
        
        public List<RateDetail> getLTLRates()
        {
            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();

            try
            {
                #region -- Define Per Package Charge and Per Shipment Charge dictionaries for LTL (charge per plant) --
                Dictionary<string, double> dPerPackageChargeLTL = new Dictionary<string, double>();
                Dictionary<string, double> dPerShipmentChargeLTL = new Dictionary<string, double>();
                Dictionary<string, double> dUpchargeLTL = new Dictionary<string, double>();

                DBUtil db = new DBUtil();

                DataSet ds = db.getPlantCharges("M33", acctNumber);

                foreach (DataRow drCharges in ds.Tables[0].Rows)
                {
                    dPerPackageChargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                    dPerShipmentChargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));
                    dUpchargeLTL.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
                }


                #endregion

                string url = Config.TransPlaceUrl;// Config.M33Url;
                string token = Config.TransPlaceToken;// Config.M33Token;


                //url = Config.M33Url;
                //token = Config.M33Token;

                string fullPostData = "";

                string pickupDateString = "";

                try
                {
                    //DateTime datePickup = DateTime.Parse(txtPickupDate.Text);
                    pickupDateString = pickupDate.Year.ToString() + "-" + pickupDate.Month.ToString() + "-" + pickupDate.Day.ToString();
                }
                catch (Exception e)
                {
                    pickupDateString = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Day.ToString();
                }
                //string ltlClass = lstLTLClass.SelectedValue.ToString();

                #region -- Define tblLTLServices to hold rate data --

                DataSet dsLTLServices = new DataSet();
                DataTable tblLTLServices = dsLTLServices.Tables.Add();

                tblLTLServices.Columns.Add("Plant", typeof(string));
                tblLTLServices.Columns.Add("Service", typeof(string));
                tblLTLServices.Columns.Add("Rate", typeof(double));
                tblLTLServices.Columns.Add("TransitDays", typeof(int));
                tblLTLServices.Columns.Add("Direct", typeof(string));
                tblLTLServices.Columns.Add("Note", typeof(string));

                #endregion

                if (lastPkgWeight == 0)
                {
                    lastPkgWeight = pkgWeight;
                }

                //int totalWeight = (pkgWeight * (numPackages - 1)) + lastPkgWeight;
                int totalWeight = 0;

                if (packageWeights.Count == 0)
                {
                    totalWeight = (pkgWeight * (numPackages - 1)) + lastPkgWeight;
                }
                else
                {
                    foreach (int pkgWgt in packageWeights)
                    {
                        totalWeight += pkgWgt;
                    }
                }



                string[] plantCodes = { "" };
                /*
                if (plantShortCode == "ALL")
                {
                    plantCodes = Config.PlantCodes;
                }
                else
                {
                    plantCodes[0] = plantShortCode;
                }
                */
                
                plantCodes[0] = plantCode;

                string combinedResponses = "";
                foreach (string currentPlantCode in plantCodes)
                {

                    WebRequest request = WebRequest.Create(url + "rate/quote?loginToken=" + token);
                    request.Method = "POST";

                    #region -- Load Plant Ship From Address --
                    string shipFromCity = "";
                    string shipFromState = "";
                    string shipFromZip = "";
                    string shipFromCountry = "";

                    shipFromCity = fromRate.city;
                    shipFromState = fromRate.state;
                    shipFromZip = fromRate.zip;
                    shipFromCountry = fromRate.country;

                    /*
                    SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["UpsRateSqlConnection"].ConnectionString);
                    conn.Open();

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT City, State, Zip, Country FROM Plants WHERE PlantCode = '" + currentPlantCode + "'";

                    SqlDataReader drResults = cmd.ExecuteReader();

                    if (drResults.Read())
                    {
                        shipFromCity = drResults["City"].ToString();
                        shipFromState = drResults["State"].ToString();
                        shipFromZip = drResults["Zip"].ToString();
                        shipFromCountry = drResults["Country"].ToString();
                    }
                    else
                    {
                        //error looking up Address Info
                        sbResults.Append("Unable to lookup address info for Plant " + currentPlantCode + "<br/>");
                    }

                    conn.Close();
                    */
                    #endregion

                    string postData = "<?xml version=\"1.0\"?>";
                    postData += "<quote>";
                    postData += "<requestedMode>LTL</requestedMode>";
                    postData += "<requestedPickupDate>" + pickupDateString + "</requestedPickupDate>";
                    postData += "<shipper>";
                    postData += "<city>" + shipFromCity + "</city>";
                    postData += "<region>" + shipFromState + "</region>";
                    postData += "<country>" + shipFromCountry + "</country>";
                    postData += "<postalCode>" + shipFromZip + "</postalCode>";
                    postData += "</shipper>";
                    postData += "<consignee>";
                    postData += "<city>" + toRate.city +"</city>";
                    postData += "<region>" + toRate.state + "</region>";
                    postData += "<country>" + toRate.country + "</country>";
                    postData += "<postalCode>" + toRate.zip + "</postalCode>";
                    postData += "</consignee>";
                    postData += "<lineItems>";
                    postData += "<lineItem>";
                    postData += "<freightClass>" + ltlClass + "</freightClass>";
                    postData += "<weight>" + totalWeight.ToString() + "</weight>";
                    postData += "<weightUnit>LB</weightUnit>";
                    postData += "</lineItem>";
                    postData += "</lineItems>";

                    //string accessorials = GetLTLAccessorials();
                    if (accessorials.Length > 0)
                    {
                        postData += "<accessorials>";
                        string[] accessorialArray = accessorials.Split(';');
                        for (int i = 0; i < accessorialArray.Count(); i++)
                        {
                            postData += "<accessorial><type>" + accessorialArray[i] + "</type></accessorial>";
                        }
                        postData += "</accessorials>";
                    }
                    postData += "</quote>";

                    fullPostData += postData;

                    /*
                    lblResults.Text = postData;
                    lblResults.Text += "\n\n\n";
                    lblResults.Text += "<br/>" + url + "rate/quote?loginToken=" + token;
                    lblResults.Text += "\n\n\n";
                     */

                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
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
                    //lblResults.Text += responseFromServer;
                    //lblResults.Text += "\n\n\n";
                    // Clean up the streams.
                    reader.Close();
                    dataStream.Close();
                    WebResponse.Close();

                    combinedResponses += responseFromServer;


                    XDocument xmlDoc = XDocument.Parse(responseFromServer);

                    Console.WriteLine(responseFromServer);

                    foreach (var rate in xmlDoc.Descendants("rate"))
                    {
                        string carrier = rate.Element("carrier").Element("name").Value.Trim();
                        string direct = rate.Element("direct").Value.Trim();
                        string note = "";
                        try {
                            //note = DateTime.Now.ToShortTimeString() + "-" + rate.Element("note").Value.Trim();
                            note = rate.Element("note").Value.Trim();
                        }
                        catch(Exception err)
                        {
                            note = "";
                        }
                        int transitDays = 0;
                        try {
                            transitDays = Convert.ToInt16(rate.Element("transitDays").Value.Trim());
                        }
                        catch(Exception err)
                        {
                            transitDays = 0;
                        }
                        double cost = double.Parse(rate.Element("cost").Element("totalAmount").Value.Trim());
                        double totalCharges = 0;

                        #region -- Define variables for markup calculations --
                        double markupPercentage = 0;
                        double perPackageCharge = 0;
                        double perShipmentCharge = 0;
                        #endregion

                        #region -- Determine the LTL markup percentage --
                        if (dPerPackageChargeLTL.ContainsKey(currentPlantCode))
                        {
                            markupPercentage = Convert.ToDouble(dUpchargeLTL[currentPlantCode]);
                            perPackageCharge = dPerPackageChargeLTL[currentPlantCode];
                            perShipmentCharge = dPerShipmentChargeLTL[currentPlantCode];
                        }
                        else
                        {
                            sbResults.Append("Unable to find LTL markup charges for plant code: " + currentPlantCode + "<br/>");
                        }
                        #endregion

                        //lblResults.Text += "Cost is " + cost.ToString() + "\n";
                        //lblResults.Text += "Markup percentage is " + markupPercentage.ToString() + "\n";
                        //lblResults.Text += "Number of Packages is " + numPackages.ToString() + "\n";
                        //lblResults.Text += "Per package charge is " + perPackageCharge.ToString() + "\n";
                        //lblResults.Text += "Per shipment charge is " + perShipmentCharge.ToString() + "\n";

                        totalCharges = cost;
                        totalCharges += ((markupPercentage / 100) * cost);
                        totalCharges += (perPackageCharge * numPackages) + perShipmentCharge;

                        //lblResults.Text += "Calculated total charge is " + totalCharges.ToString() + "\n\n";

                        //if ((carrier != "LTL BENCHMARK") || (Session["DefaultPlant"].ToString() == "POR"))
                        if (carrier != "LTL BENCHMARK")
                        {
                            tblLTLServices.Rows.Add(currentPlantCode, carrier, totalCharges, transitDays, direct, note);
                            //RateDetail currentRate = new RateDetail("LTL", carrier + " (" + transitDays.ToString() + ((transitDays == 1) ? " DAY " : " DAYS " ) + direct + ")", decimal.Parse(totalCharges.ToString()));
                            //RateDetail currentRate = new RateDetail("LTL", carrier + " (" + transitDays.ToString() + " DAY)", decimal.Parse(totalCharges.ToString()));
                            RateDetail currentRate = new RateDetail("LTL", carrier, decimal.Parse(totalCharges.ToString()), note);
                            rates.Add(currentRate);
                        }
                        /*
                        else if (Session["DefaultPlant"].ToString() == "POR")
                        {
                            tblLTLServices.Rows.Add(currentPlantCode, carrier, cost, transitDays, direct);
                        }
                         */


                    }
                }

                #region -- Display Shopped Service Rates in Gridview --

                //pnlLTLRates.Visible = (tblLTLServices.Rows.Count > 0);
                //DataView dvLTLRates = tblLTLServices.DefaultView;

                //gvLTLRates.DataSource = dvLTLRates;
                //gvLTLRates.DataBind();

                //gvLTLRates.Visible = true;

                #region -- log results --
                /*
                try
                {
                    string UserName = HttpContext.Current.User.Identity.Name.Replace(Config.NetworkDomain + "\\", "").ToLower();

                    System.IO.StringWriter swGridResults = new System.IO.StringWriter();

                    System.Web.UI.HtmlTextWriter htwGridResults = new HtmlTextWriter(swGridResults);

                    gvLTLRates.RenderControl(htwGridResults);

                    SqlConnection connLog = new SqlConnection(ConfigurationManager.ConnectionStrings["UpsRateSqlConnection"].ConnectionString);
                    connLog.Open();

                    SqlCommand cmdLog = new SqlCommand();
                    cmdLog.Connection = connLog;
                    cmdLog.CommandType = CommandType.StoredProcedure;
                    cmdLog.CommandText = "LogResultsLTL";

                    SqlParameter pPlantCode = new SqlParameter("@PlantCode", SqlDbType.VarChar, 10);
                    SqlParameter pUserName = new SqlParameter("@UserName", SqlDbType.VarChar, 50);
                    SqlParameter pFullRequest = new SqlParameter("@FullRequest", SqlDbType.NText);
                    SqlParameter pFullResults = new SqlParameter("@FullResults", SqlDbType.NText);
                    SqlParameter pXmlResponse = new SqlParameter("@XmlResponse", SqlDbType.NText);

                    pPlantCode.Value = plantShortCode;
                    pUserName.Value = UserName;
                    pFullRequest.Value = fullPostData;
                    pFullResults.Value = swGridResults.ToString();
                    pXmlResponse.Value = ""; // combinedResponses;  // <- Combined Responses only saved during testing for performance reasons

                    cmdLog.Parameters.Add(pPlantCode);
                    cmdLog.Parameters.Add(pUserName);
                    cmdLog.Parameters.Add(pFullRequest);
                    cmdLog.Parameters.Add(pFullResults);
                    cmdLog.Parameters.Add(pXmlResponse);

                    cmdLog.ExecuteNonQuery();
                    connLog.Close();
                }
                catch (Exception e)
                {
                    //handleError("ShowLTLRates", "Error in saving results to DB: " + e.Message);
                    sbResults.AppendLine("Error in saving results to DB: " + e.Message);
                    //lblResults.Text += sbResults.ToString();
                    //lblResults.Visible = true;
                }
                 */
                #endregion

                #endregion


            }
            catch (Exception e)
            {
                //WRITE ERROR TO REQUEST
                handleError("ShowLTLRates", e.ToString());
                //lblResults.Text += e.ToString();
                //lblResults.Visible = true;
            }


            Console.WriteLine(sbResults.ToString());
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
            StringBuilder sbResults = new StringBuilder();

            bool SoapError = false;

            double shipmentWeight = 0;
            int shipmentClassification = 0;


            #region -- Define Per Package Charge and Per Shipment Charge dictionaries standard (charge per plant) --

            Dictionary<string, double> dPerPackageChargeGF = new Dictionary<string, double>();
            Dictionary<string, double> dPerShipmentChargeGF = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeGF = new Dictionary<string, double>();

            DBUtil db = new DBUtil();

            DataSet ds = db.getPlantCharges("GF", acctNumber);

            foreach (DataRow drCharges in ds.Tables[0].Rows)
            {
                dPerPackageChargeGF.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                dPerShipmentChargeGF.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));
                dUpchargeGF.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
                
            }

            #endregion

            #region -- Calculate Weight / Packages --

            if (lastPkgWeight == 0)
            {
                lastPkgWeight = pkgWeight;
            }

            int totalWeight = 0;

            if (packageWeights.Count == 0)
            {
                totalWeight = (pkgWeight * (numPackages - 1)) + lastPkgWeight;
            }
            else
            {
                foreach (int pkgWgt in packageWeights)
                {
                    totalWeight += pkgWgt;
                }
            }

            sbResults.AppendLine("Total weight: " + totalWeight.ToString() + ", # Pkgs: " + numPackages.ToString());

            #endregion



            #region -- Process rate request for a single plant --
            try
            {

                int RateRequestId = 0;

                #region -- Load Ship From Address --
                string shipFromAddress = fromRate.street;
                string shipFromCity = fromRate.city;
                string shipFromState = fromRate.state;
                string shipFromZip = fromRate.zip;
                string shipFromCountry = fromRate.country;

                #endregion

                #region -- Begin building Rate Request --
                RateService rateService = new RateService();
                RateRequest rateRequest = new RateRequest();

                UpsRateWebReference.RequestType request = new UpsRateWebReference.RequestType();
                String[] requestOption = { "Rate" }; // Needed for GF
                request.RequestOption = requestOption;
                rateRequest.Request = request;
                #endregion

                #region -- Access Security (license number, username, password) --
                UpsRateWebReference.UPSSecurity upss = new UpsRateWebReference.UPSSecurity();
                UpsRateWebReference.UPSSecurityServiceAccessToken upsSvcToken = new UpsRateWebReference.UPSSecurityServiceAccessToken();
                upsSvcToken.AccessLicenseNumber = Config.UPSAccessKey;
                upss.ServiceAccessToken = upsSvcToken;
                UpsRateWebReference.UPSSecurityUsernameToken upsSecUsrnameToken = new UpsRateWebReference.UPSSecurityUsernameToken();
                upsSecUsrnameToken.Username = Config.UPSUserName;
                upsSecUsrnameToken.Password = Config.UPSPassword;
                upss.UsernameToken = upsSecUsrnameToken;
                rateService.UPSSecurityValue = upss;
                #endregion

                #region -- Build Shipment object --
                ShipmentType shipment = new ShipmentType();

                #region -- Shipper --
                ShipperType shipper = new ShipperType();
                shipper.ShipperNumber = Config.ShipFromShipperNumber;
                AddressType shipperAddress = new AddressType();
                String[] shipperAddressLine = { shipFromAddress };
                shipperAddress.AddressLine = shipperAddressLine;
                shipperAddress.City = shipFromCity;
                shipperAddress.StateProvinceCode = shipFromState;
                shipperAddress.PostalCode = shipFromZip;
                shipperAddress.CountryCode = shipFromCountry;
                shipper.Address = shipperAddress;

                shipment.Shipper = shipper;
                #endregion

                #region -- Ship To --
                ShipToType shipTo = new ShipToType();
                ShipToAddressType shipToAddress = new ShipToAddressType();
                String[] shipToAddressLines = { toRate.street };
                shipToAddress.AddressLine = shipToAddressLines;
                shipToAddress.City = toRate.city;
                shipToAddress.StateProvinceCode = toRate.state;
                shipToAddress.PostalCode = toRate.zip;
                shipToAddress.CountryCode = toRate.country;

                shipTo.Address = shipToAddress;

                shipment.ShipTo = shipTo;

                #endregion

                #region -- Packages --
                PackageType package;
                PackageWeightType packageWeight;
                UpsRateWebReference.CodeDescriptionType uomCodeDesc;

                UpsRateWebReference.CodeDescriptionType packageTypeCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                packageTypeCodeDesc.Code = "00";

                string deliveryConfirmationOption = deliveryConfCode.ToString();

                PackageType[] packages = new PackageType[numPackages];

                if (packageWeights.Count == 0)
                {
                    for (int i = 0; i < numPackages; i++)
                    {
                        package = new PackageType();
                        packageWeight = new PackageWeightType();
                        uomCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                        uomCodeDesc.Code = "LBS";
                        if (i == (numPackages - 1))
                        {
                            packageWeight.Weight = lastPkgWeight.ToString();
                        }
                        else
                        {
                            packageWeight.Weight = pkgWeight.ToString();
                        }
                        packageWeight.UnitOfMeasurement = uomCodeDesc;
                        package.PackageWeight = packageWeight;

                        package.PackagingType = packageTypeCodeDesc;

                        if (deliveryConfirmationOption != "0")
                        {
                            PackageServiceOptionsType packageServiceOptions = new PackageServiceOptionsType();
                            DeliveryConfirmationType deliveryConfirmation = new DeliveryConfirmationType();
                            deliveryConfirmation.DCISType = deliveryConfirmationOption;
                            packageServiceOptions.DeliveryConfirmation = deliveryConfirmation;
                            package.PackageServiceOptions = packageServiceOptions;
                        }

                        //Needed for GF
                        CommodityType commodity = new CommodityType();
                        commodity.FreightClass = ltlClass;
                        package.Commodity = commodity;

                        packages[i] = package;
                    }
                }
                else
                {
                    int i = 0;
                    foreach (int pkgWgt in packageWeights)
                    {
                        package = new PackageType();
                        packageWeight = new PackageWeightType();
                        uomCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                        uomCodeDesc.Code = "LBS";

                        packageWeight.Weight = pkgWgt.ToString();
                        packageWeight.UnitOfMeasurement = uomCodeDesc;
                        package.PackageWeight = packageWeight;

                        package.PackagingType = packageTypeCodeDesc;

                        if (deliveryConfirmationOption != "0")
                        {
                            PackageServiceOptionsType packageServiceOptions = new PackageServiceOptionsType();
                            DeliveryConfirmationType deliveryConfirmation = new DeliveryConfirmationType();
                            deliveryConfirmation.DCISType = deliveryConfirmationOption;
                            packageServiceOptions.DeliveryConfirmation = deliveryConfirmation;
                            package.PackageServiceOptions = packageServiceOptions;
                        }

                        //Needed for GF
                        CommodityType commodity = new CommodityType();
                        commodity.FreightClass = ltlClass;
                        package.Commodity = commodity;

                        packages[i] = package;

                        i++;
                    }
                }

                shipment.Package = packages;
                #endregion

                ShipmentRatingOptionsType ratingOptions = new ShipmentRatingOptionsType();
                ratingOptions.FRSShipmentIndicator = "";
                ratingOptions.NegotiatedRatesIndicator = "";
                shipment.ShipmentRatingOptions = ratingOptions;

                UpsRateWebReference.CodeDescriptionType service = new UpsRateWebReference.CodeDescriptionType();
                service.Code = "03";
                shipment.Service = service;

                UpsRateWebReference.FRSPaymentInfoType frsPaymentInfo = new UpsRateWebReference.FRSPaymentInfoType();
                UpsRateWebReference.CodeDescriptionType frsPaymentInfoType = new UpsRateWebReference.CodeDescriptionType();

                frsPaymentInfoType.Code = "01";
                frsPaymentInfo.Type = frsPaymentInfoType;

                shipment.FRSPaymentInformation = frsPaymentInfo;

                rateRequest.Shipment = shipment;

                #endregion

                #region -- Submit Rate Request --

                RateResponse rateResponse = rateService.ProcessRate(rateRequest);

                RatedShipmentType[] ratedShipments = rateResponse.RatedShipment;

                string requestXml = SoapTrace.TraceExtension.XmlRequest.OuterXml.ToString();
                string responseXml = SoapTrace.TraceExtension.XmlResponse.OuterXml.ToString();

                string targetUrl = rateService.Url;
                
                
                #endregion

                #region -- Define tblServices to hold rate data --

                DataSet dsServices = new DataSet();
                DataTable tblServices = dsServices.Tables.Add();

                tblServices.Columns.Add("Plant", typeof(string));
                tblServices.Columns.Add("Desc", typeof(string));
                tblServices.Columns.Add("Rate", typeof(double));

                #endregion

                #region -- Process each rated service --

                foreach (RatedShipmentType ratedShipment in ratedShipments)
                {
                    string serviceCode = "";
                    double billingWeight = 0;
                    double serviceCharges = 0;
                    double transportationCharges = 0;
                    int addressClassification = 1;
                    double totalCharges = 0; //will pull from Negotiated

                    serviceCode = ratedShipment.Service.Code;

                    totalCharges = Convert.ToDouble(ratedShipment.NegotiatedRateCharges.TotalCharge.MonetaryValue);
                    billingWeight = Convert.ToDouble(ratedShipment.BillingWeight.Weight);
                    serviceCharges = Convert.ToDouble(ratedShipment.ServiceOptionsCharges.MonetaryValue);
                    transportationCharges = Convert.ToDouble(ratedShipment.TransportationCharges.MonetaryValue);

                    sbResults.Append("Negotiated charges: " + totalCharges.ToString() + "<br/>");
                    

                    if (ratedShipment.RatedShipmentAlert != null)
                    {
                        RatedShipmentInfoType[] ratedShipmentAlerts = ratedShipment.RatedShipmentAlert;
                        foreach (RatedShipmentInfoType ratedShipmentAlert in ratedShipmentAlerts)
                        {
                            if (ratedShipmentAlert.Code == "110920")
                            {
                                //Then the "Commercial" classification has changed to "Residential" so set classification to 2
                                addressClassification = 2;
                            }
                            sbResults.AppendLine("Shipment Alert: " + ratedShipmentAlert.Code + " - " + ratedShipmentAlert.Description);
                        }
                    }

                    sbResults.AppendLine();

                    #region -- Define variables for markup calculations --
                    double markupPercentage = 0;
                    double perPackageCharge = 0;
                    double perShipmentCharge = 0;
                    
                    #endregion

                    #region -- Determine the UPS Standard markup percentage --
                    if (dPerPackageChargeGF.ContainsKey(plantCode))
                    {
                        markupPercentage = Convert.ToDouble(dUpchargeGF[plantCode]);
                        perPackageCharge = dPerPackageChargeGF[plantCode];
                        perShipmentCharge = dPerShipmentChargeGF[plantCode];
                    }
                    else
                    {
                        sbResults.AppendLine("Unable to find UPS Ground Freight markup charges for plant code: " + plantCode);
                    }
                    #endregion
                    
                    totalCharges += ((markupPercentage / 100) * totalCharges);
                    totalCharges += (perPackageCharge * numPackages) + perShipmentCharge;

                    RateDetail currentRate = new RateDetail(plantCode, "UPS Ground Freight", addressClassification, int.Parse(billingWeight.ToString()), false, decimal.Parse(totalCharges.ToString()), decimal.Parse(serviceCharges.ToString()), decimal.Parse(transportationCharges.ToString()), "UPS");
                    rates.Add(currentRate);

                    shipmentWeight = billingWeight;
                    shipmentClassification = addressClassification;
                }
                #endregion

                

            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                if ((ex.Detail.LastChild.InnerText != "Hard110003Maximum number of packages exceeded (50)") //Do not show "scary" red message if only soap error is that it exceeds 50 packages (also catch 200 pkg message)
                    && (ex.Detail.LastChild.InnerText != "Hard110003Maximum number of packages exceeded (200)"))
                {
                    //WRITE ERROR TO REQUEST
                    handleError("ShowRates", "Soap Exception - SoapException Message= " + ex.Message + " SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText + " SoapException XML String for all= " + ex.Detail.LastChild.OuterXml + " SoapException StackTrace= " + ex.StackTrace);
                }
                #region -- Handle SOAP error --
                SoapError = true;
                sbResults.AppendLine("---------Freight Rate Web Service returns error----------------");
                sbResults.AppendLine("---------\"Hard\" is user error \"Transient\" is system error----------------");
                sbResults.AppendLine("SoapException Message= " + ex.Message);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException XML String for all= " + ex.Detail.LastChild.OuterXml);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException StackTrace= " + ex.StackTrace);
                sbResults.AppendLine("-------------------------");
                #endregion
            }
            catch (Exception ex)
            {
                //WRITE ERROR TO REQUEST
                handleError("ShowRates", "General Error - General Exception= " + ex.Message + " General Exception-StackTrace= " + ex.StackTrace);
                SoapError = true;
                #region -- Handle misc error --
                sbResults.AppendLine();
                sbResults.AppendLine("-------------------------");
                sbResults.AppendLine(" General Exception= " + ex.Message);
                sbResults.AppendLine(" General Exception-StackTrace= " + ex.StackTrace);
                sbResults.AppendLine("-------------------------");
                #endregion
            }

            #endregion

            

            Console.WriteLine(sbResults.ToString());
            //lblResults.Text += sbResults.ToString();


            return (rates);
        }
        /*
        private void getGroundFreightRates()//(string shipToConsignee, string shipToStreet, string shipToCity, string shipToState, string shipToZip, string shipToCountry, string plantCode, bool addressCorrected)
        {
            int acctNumber = 0;
            Int32.TryParse(txtAcctNumber.Text.Trim(), out acctNumber);

            lblRateAddress.Text = shipToStreet;
            lblRateCity.Text = shipToCity;
            lblRateState.Text = shipToState;
            lblRateZip.Text = shipToZip;

            StringBuilder sbResults = new StringBuilder();

            bool SoapError = false;

            double shipmentWeight = 0;
            int shipmentClassification = 0;


            Dictionary<string, double> dPerPackageChargeGF = new Dictionary<string, double>();
            Dictionary<string, double> dPerShipmentChargeGF = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeGF = new Dictionary<string, double>();

            SqlConnection connCharges = new SqlConnection(ConfigurationManager.ConnectionStrings["UpsRateSqlConnection"].ConnectionString);
            connCharges.Open();

            SqlCommand cmdCharges = new SqlCommand();
            cmdCharges.Connection = connCharges;

            cmdCharges.CommandText = "GetPlantCharges";
            cmdCharges.CommandType = CommandType.StoredProcedure;

            cmdCharges.Parameters.Add("@Carrier", SqlDbType.VarChar, 50).Value = "GF";
            cmdCharges.Parameters.Add("@AcctNumber", SqlDbType.Int).Value = acctNumber;

            SqlDataReader drCharges = cmdCharges.ExecuteReader();

            while (drCharges.Read())
            {
                dPerPackageChargeGF.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                dPerShipmentChargeGF.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));
                dUpchargeGF.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
            }

            connCharges.Close();


            int numPackages = Convert.ToInt16(txtNumPackages.Text.Trim());
            string sPkgWeight = txtPackageWeight.Text.Trim();
            string sLastPkgWeight = txtLastPackageWeight.Text.Trim();

            if (sLastPkgWeight == "")
            {
                sLastPkgWeight = sPkgWeight;
            }

            int totalWeight = (Convert.ToInt16(sPkgWeight) * (numPackages - 1)) + Convert.ToInt16(sLastPkgWeight);

            sbResults.Append("Total weight: " + totalWeight.ToString() + "<br/>");


            if (plantCode != "ALL")
            {
                // -- Process rate request for a single plant --
                try
                {

                    int RateRequestId = 0;

                    // -- Load Plant Ship From Address --
                    string shipFromAddress = "";
                    string shipFromCity = "";
                    string shipFromState = "";
                    string shipFromZip = "";
                    string shipFromCountry = "";

                    SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["UpsRateSqlConnection"].ConnectionString);
                    conn.Open();

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT Address, City, State, Zip, Country FROM Plants WHERE PlantCode = '" + plantCode + "'";

                    SqlDataReader drResults = cmd.ExecuteReader();

                    if (drResults.Read())
                    {
                        shipFromAddress = drResults["Address"].ToString();
                        shipFromCity = drResults["City"].ToString();
                        shipFromState = drResults["State"].ToString();
                        shipFromZip = drResults["Zip"].ToString();
                        shipFromCountry = drResults["Country"].ToString();
                    }
                    else
                    {
                        //error looking up Address Info
                        sbResults.Append("Unable to lookup address info for Plant " + plantCode + "<br/>");
                    }

                    conn.Close();


                    // -- Begin building Rate Request --
                    RateService rateService = new RateService();
                    RateRequest rateRequest = new RateRequest();

                    UpsRateWebReference.RequestType request = new UpsRateWebReference.RequestType();
                    String[] requestOption = { "Rate" }; // Needed for GF
                    request.RequestOption = requestOption;
                    rateRequest.Request = request;


                    // -- Access Security (license number, username, password) --
                    UpsRateWebReference.UPSSecurity upss = new UpsRateWebReference.UPSSecurity();
                    UpsRateWebReference.UPSSecurityServiceAccessToken upsSvcToken = new UpsRateWebReference.UPSSecurityServiceAccessToken();
                    upsSvcToken.AccessLicenseNumber = Config.UPSAccessKey;
                    upss.ServiceAccessToken = upsSvcToken;
                    UpsRateWebReference.UPSSecurityUsernameToken upsSecUsrnameToken = new UpsRateWebReference.UPSSecurityUsernameToken();
                    upsSecUsrnameToken.Username = Config.UPSUserName;
                    upsSecUsrnameToken.Password = Config.UPSPassword;
                    upss.UsernameToken = upsSecUsrnameToken;
                    rateService.UPSSecurityValue = upss;


                    // -- Build Shipment object --
                    ShipmentType shipment = new ShipmentType();

                    // -- Shipper --
                    ShipperType shipper = new ShipperType();
                    shipper.ShipperNumber = Config.ShipFromShipperNumber;
                    AddressType shipperAddress = new AddressType();
                    String[] shipperAddressLine = { shipFromAddress };
                    shipperAddress.AddressLine = shipperAddressLine;
                    shipperAddress.City = shipFromCity;
                    shipperAddress.StateProvinceCode = shipFromState;
                    shipperAddress.PostalCode = shipFromZip;
                    shipperAddress.CountryCode = shipFromCountry;
                    shipper.Address = shipperAddress;

                    shipment.Shipper = shipper;


                    // -- Ship To --
                    ShipToType shipTo = new ShipToType();
                    ShipToAddressType shipToAddress = new ShipToAddressType();
                    String[] shipToAddressLines = { shipToStreet };
                    shipToAddress.AddressLine = shipToAddressLines;
                    shipToAddress.City = shipToCity;
                    shipToAddress.StateProvinceCode = shipToState;
                    shipToAddress.PostalCode = shipToZip;
                    shipToAddress.CountryCode = shipToCountry;

                    shipTo.Address = shipToAddress;

                    shipment.ShipTo = shipTo;



                    // -- Packages --
                    PackageType package;
                    PackageWeightType packageWeight;
                    UpsRateWebReference.CodeDescriptionType uomCodeDesc;

                    UpsRateWebReference.CodeDescriptionType packageTypeCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                    packageTypeCodeDesc.Code = "00";

                    string deliveryConfirmationOption = lstDeliveryConfirmation.SelectedValue;

                    PackageType[] packages = new PackageType[numPackages];
                    for (int i = 0; i < numPackages; i++)
                    {
                        package = new PackageType();
                        packageWeight = new PackageWeightType();
                        uomCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                        uomCodeDesc.Code = "LBS";
                        if (i == (numPackages - 1))
                        {
                            packageWeight.Weight = sLastPkgWeight;
                        }
                        else
                        {
                            packageWeight.Weight = sPkgWeight;
                        }
                        packageWeight.UnitOfMeasurement = uomCodeDesc;
                        package.PackageWeight = packageWeight;

                        package.PackagingType = packageTypeCodeDesc;

                        if (deliveryConfirmationOption != "0")
                        {
                            PackageServiceOptionsType packageServiceOptions = new PackageServiceOptionsType();
                            DeliveryConfirmationType deliveryConfirmation = new DeliveryConfirmationType();
                            deliveryConfirmation.DCISType = deliveryConfirmationOption;
                            packageServiceOptions.DeliveryConfirmation = deliveryConfirmation;
                            package.PackageServiceOptions = packageServiceOptions;
                        }

                        //Needed for GF
                        CommodityType commodity = new CommodityType();
                        commodity.FreightClass = lstLTLClass.SelectedValue;
                        package.Commodity = commodity;

                        packages[i] = package;
                    }

                    shipment.Package = packages;


                    ShipmentRatingOptionsType ratingOptions = new ShipmentRatingOptionsType();
                    ratingOptions.FRSShipmentIndicator = "";
                    ratingOptions.NegotiatedRatesIndicator = "";
                    shipment.ShipmentRatingOptions = ratingOptions;

                    UpsRateWebReference.CodeDescriptionType service = new UpsRateWebReference.CodeDescriptionType();
                    service.Code = "03";
                    shipment.Service = service;

                    UpsRateWebReference.FRSPaymentInfoType frsPaymentInfo = new UpsRateWebReference.FRSPaymentInfoType();
                    UpsRateWebReference.CodeDescriptionType frsPaymentInfoType = new UpsRateWebReference.CodeDescriptionType();

                    frsPaymentInfoType.Code = "01";
                    frsPaymentInfo.Type = frsPaymentInfoType;

                    shipment.FRSPaymentInformation = frsPaymentInfo;

                    rateRequest.Shipment = shipment;


                    // -- Submit Rate Request --

                    RateResponse rateResponse = rateService.ProcessRate(rateRequest);

                    RatedShipmentType[] ratedShipments = rateResponse.RatedShipment;

                    string requestXml = SoapTrace.TraceExtension.XmlRequest.OuterXml.ToString();
                    string responseXml = SoapTrace.TraceExtension.XmlResponse.OuterXml.ToString();

                    //Response.Write("<br/><br/><br/>");
                    // Response.Write(requestXml);
                    // Response.Write("<br/><br/><br/>");
                    //Response.Write(responseXml);
                    // Response.Write("<br/><br/><br/>");

                    string targetUrl = rateService.Url;
                    string UserName = HttpContext.Current.User.Identity.Name.Replace(Config.NetworkDomain + "\\", "").ToLower();

                    // -- log request --
                    try
                    {
                        SqlConnection connLog = new SqlConnection(ConfigurationManager.ConnectionStrings["UpsRateSqlConnection"].ConnectionString);
                        connLog.Open();

                        SqlCommand cmdLog = new SqlCommand();
                        cmdLog.Connection = connLog;
                        cmdLog.CommandType = CommandType.StoredProcedure;
                        cmdLog.CommandText = "LogRequest_Rate";

                        SqlParameter pUserName = new SqlParameter("@UserName", SqlDbType.VarChar, 50);
                        SqlParameter pTargetUrl = new SqlParameter("@TargetUrl", SqlDbType.VarChar, 200);
                        SqlParameter pAddress = new SqlParameter("@Address", SqlDbType.VarChar, 200);
                        SqlParameter pCity = new SqlParameter("@City", SqlDbType.VarChar, 200);
                        SqlParameter pState = new SqlParameter("@State", SqlDbType.VarChar, 50);
                        SqlParameter pZip = new SqlParameter("@Zip", SqlDbType.VarChar, 50);
                        SqlParameter pCountry = new SqlParameter("@Country", SqlDbType.VarChar, 2);
                        SqlParameter pRequestXml = new SqlParameter("@RequestXml", SqlDbType.NText);
                        SqlParameter pResponseXml = new SqlParameter("@ResponseXml", SqlDbType.NText);
                        SqlParameter pRequestId = new SqlParameter("@RequestId", SqlDbType.Int);

                        pUserName.Value = UserName;
                        pTargetUrl.Value = targetUrl;
                        pAddress.Value = shipToStreet;
                        pCity.Value = shipToCity;
                        pState.Value = shipToState;
                        pZip.Value = shipToZip;
                        pCountry.Value = shipToCountry;
                        pRequestXml.Value = requestXml;
                        pResponseXml.Value = responseXml;
                        pRequestId.Direction = ParameterDirection.Output;

                        cmdLog.Parameters.Add(pUserName);
                        cmdLog.Parameters.Add(pTargetUrl);
                        cmdLog.Parameters.Add(pAddress);
                        cmdLog.Parameters.Add(pCity);
                        cmdLog.Parameters.Add(pState);
                        cmdLog.Parameters.Add(pZip);
                        cmdLog.Parameters.Add(pCountry);
                        cmdLog.Parameters.Add(pRequestXml);
                        cmdLog.Parameters.Add(pResponseXml);
                        cmdLog.Parameters.Add(pRequestId);

                        cmdLog.ExecuteNonQuery();

                        RateRequestId = Convert.ToInt32(pRequestId.Value);

                        connLog.Close();
                    }
                    catch (Exception e)
                    {
                        handleError("ShowRates", "Error in saving request to DB: " + e.Message);
                        sbResults.Append("Error in saving request to DB: " + e.Message + "<br/>");
                    }




                    // -- Define tblServices to hold rate data --

                    DataSet dsServices = new DataSet();
                    DataTable tblServices = dsServices.Tables.Add();

                    tblServices.Columns.Add("Plant", typeof(string));
                    tblServices.Columns.Add("Desc", typeof(string));
                    tblServices.Columns.Add("Rate", typeof(double));



                    // -- Process each rated service --

                    foreach (RatedShipmentType ratedShipment in ratedShipments)
                    {
                        string serviceCode = "";
                        int addressClassification = 1;
                        double totalCharges = 0; //will pull from Negotiated

                        serviceCode = ratedShipment.Service.Code;

                        totalCharges = Convert.ToDouble(ratedShipment.NegotiatedRateCharges.TotalCharge.MonetaryValue);

                        sbResults.Append("Negotiated charges: " + totalCharges.ToString() + "<br/>");

                        if (ratedShipment.RatedShipmentAlert != null)
                        {
                            RatedShipmentInfoType[] ratedShipmentAlerts = ratedShipment.RatedShipmentAlert;
                            foreach (RatedShipmentInfoType ratedShipmentAlert in ratedShipmentAlerts)
                            {
                                sbResults.Append("Shipment Alert: " + ratedShipmentAlert.Code + " - " + ratedShipmentAlert.Description + "<br/>");
                            }
                        }

                        sbResults.Append("<br/>");

                        // -- Define variables for markup calculations --
                        double markupPercentage = 0;
                        double perPackageCharge = 0;
                        double perShipmentCharge = 0;

                        // -- Determine the UPS Standard markup percentage --
                        if (dPerPackageChargeGF.ContainsKey(plantCode))
                        {
                            markupPercentage = Convert.ToDouble(dUpchargeGF[plantCode]);
                            perPackageCharge = dPerPackageChargeGF[plantCode];
                            perShipmentCharge = dPerShipmentChargeGF[plantCode];
                        }
                        else
                        {
                            sbResults.Append("Unable to find UPS Ground Freight markup charges for plant code: " + plantCode + "<br/>");
                        }

                        totalCharges += ((markupPercentage / 100) * totalCharges);
                        totalCharges += (perPackageCharge * numPackages) + perShipmentCharge;

                        tblServices.Rows.Add(plantCode, "Ground Freight", totalCharges);

                    }


                    // -- Display Service Rates in Gridview --
                    DataView dvServices = tblServices.DefaultView;
                    dvServices.Sort = "Rate";

                    gvGFRates.DataSource = dvServices;
                    gvGFRates.DataBind();

                    gvGFRates.Visible = true;
                    pnlGFRates.Visible = true;

                    // -- log results --
                    try
                    {

                    }
                    catch (Exception e)
                    {
                        handleError("ShowRates", "Error in saving results to DB: " + e.Message);
                        sbResults.Append("Error in saving results to DB: " + e.Message + "<br/>");
                    }





                }
                catch (System.Web.Services.Protocols.SoapException ex)
                {
                    if ((ex.Detail.LastChild.InnerText != "Hard110003Maximum number of packages exceeded (50)") //Do not show "scary" red message if only soap error is that it exceeds 50 packages (also catch 200 pkg message)
                       && (ex.Detail.LastChild.InnerText != "Hard110003Maximum number of packages exceeded (200)"))
                    {
                        handleError("ShowRates", "Soap Exception - SoapException Message= " + ex.Message + " SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText + " SoapException XML String for all= " + ex.Detail.LastChild.OuterXml + " SoapException StackTrace= " + ex.StackTrace);
                    }
                    // -- Handle SOAP error --
                    SoapError = true;
                    sbResults.Append("---------Freight Rate Web Service returns error----------------<br/>");
                    sbResults.Append("---------\"Hard\" is user error \"Transient\" is system error----------------<br/>");
                    sbResults.Append("SoapException Message= " + ex.Message + "<br/>");
                    sbResults.Append("<br/>");
                    sbResults.Append("SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText + "<br/>");
                    sbResults.Append("<br/>");
                    sbResults.Append("SoapException XML String for all= " + ex.Detail.LastChild.OuterXml + "<br/>");
                    sbResults.Append("<br/>");
                    sbResults.Append("SoapException StackTrace= " + ex.StackTrace + "<br/>");
                    sbResults.Append("-------------------------<br/>");

                }
                catch (System.ServiceModel.CommunicationException ex)
                {
                    handleError("ShowRates", "Communication Exception - CommunicationException= " + ex.Message + " CommunicationException-StackTrace= " + ex.StackTrace);
                    SoapError = true;
                    // -- Handle General Communication Error --
                    sbResults.Append("<br/>");
                    sbResults.Append("--------------------<br/>");
                    sbResults.Append("CommunicationException= " + ex.Message + "<br/>");
                    sbResults.Append("CommunicationException-StackTrace= " + ex.StackTrace + "<br/>");
                    sbResults.Append("-------------------------<br/>");
                    sbResults.Append("<br/>");

                }
                catch (Exception ex)
                {
                    handleError("ShowRates", "General Error - General Exception= " + ex.Message + " General Exception-StackTrace= " + ex.StackTrace);
                    SoapError = true;
                    // -- Handle misc error --
                    sbResults.Append("<br/>");
                    sbResults.Append("-------------------------<br/>");
                    sbResults.Append(" General Exception= " + ex.Message + "<br/>");
                    sbResults.Append(" General Exception-StackTrace= " + ex.StackTrace + "<br/>");
                    sbResults.Append("-------------------------<br/>");

                }


                if (!SoapError)
                {
                    lblResultsGF.Visible = false;
                    //pnlUpsCommError.Visible = false;
                }
                else
                {
                    lblResultsGF.Visible = true;
                    //gvShopServices.Visible = false;
                    gvGFRates.Visible = false;
                    //litUpsCommError.Text = "There was a error getting Ground Freight results from UPS.  Please check your information and try again.";

                    //pnlUpsCommError.Visible = true;
                }


            } //end of single Plant rating process
            else
            { // Shop rates for all plants
              // -- Process rate request for all plants and combine into a single dataset --
              // -- Define tblShopServices to hold rate data --

                DataSet dsShopServices = new DataSet();
                DataTable tblShopServices = dsShopServices.Tables.Add();

                tblShopServices.Columns.Add("Plant", typeof(string));
                tblShopServices.Columns.Add("Desc", typeof(string));
                tblShopServices.Columns.Add("Rate", typeof(double));

                int RateRequestId = 0;

                for (int iPlantCount = 0; iPlantCount < Config.PlantCodesMultiRate.Count(); iPlantCount++)
                {
                    string currentPlant = Config.PlantCodesMultiRate[iPlantCount];

                    // -- Process rate request for currentPlant --
                    try
                    {

                        // -- Load Plant Ship From Address --
                        string shipFromAddress = "";
                        string shipFromCity = "";
                        string shipFromState = "";
                        string shipFromZip = "";
                        string shipFromCountry = "";

                        SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["UpsRateSqlConnection"].ConnectionString);
                        conn.Open();

                        SqlCommand cmd = new SqlCommand();
                        cmd.Connection = conn;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT Address, City, State, Zip, Country FROM Plants WHERE PlantCode = '" + currentPlant + "'";

                        SqlDataReader drResults = cmd.ExecuteReader();

                        if (drResults.Read())
                        {
                            shipFromAddress = drResults["Address"].ToString();
                            shipFromCity = drResults["City"].ToString();
                            shipFromState = drResults["State"].ToString();
                            shipFromZip = drResults["Zip"].ToString();
                            shipFromCountry = drResults["Country"].ToString();
                        }
                        else
                        {
                            //error looking up Address Info
                            sbResults.Append("Unable to lookup address info for Plant " + currentPlant + "<br/>");
                        }

                        conn.Close();



                        // -- Begin building Rate Request --
                        RateService rateService = new RateService();
                        RateRequest rateRequest = new RateRequest();

                        UpsRateWebReference.RequestType request = new UpsRateWebReference.RequestType();
                        String[] requestOption = { "Rate" };
                        request.RequestOption = requestOption;
                        rateRequest.Request = request;


                        // -- Access Security (license number, username, password) --
                        UpsRateWebReference.UPSSecurity upss = new UpsRateWebReference.UPSSecurity();
                        UpsRateWebReference.UPSSecurityServiceAccessToken upsSvcToken = new UpsRateWebReference.UPSSecurityServiceAccessToken();
                        upsSvcToken.AccessLicenseNumber = Config.UPSAccessKey;
                        upss.ServiceAccessToken = upsSvcToken;
                        UpsRateWebReference.UPSSecurityUsernameToken upsSecUsrnameToken = new UpsRateWebReference.UPSSecurityUsernameToken();
                        upsSecUsrnameToken.Username = Config.UPSUserName;
                        upsSecUsrnameToken.Password = Config.UPSPassword;
                        upss.UsernameToken = upsSecUsrnameToken;
                        rateService.UPSSecurityValue = upss;


                        // -- Build Shipment object --
                        ShipmentType shipment = new ShipmentType();

                        // -- Shipper --
                        ShipperType shipper = new ShipperType();
                        shipper.ShipperNumber = Config.ShipFromShipperNumber;
                        AddressType shipperAddress = new AddressType();
                        String[] shipperAddressLine = { shipFromAddress };
                        shipperAddress.AddressLine = shipperAddressLine;
                        shipperAddress.City = shipFromCity;
                        shipperAddress.StateProvinceCode = shipFromState;
                        shipperAddress.PostalCode = shipFromZip;
                        shipperAddress.CountryCode = shipFromCountry;
                        shipper.Address = shipperAddress;

                        shipment.Shipper = shipper;


                        // -- Ship To --
                        ShipToType shipTo = new ShipToType();
                        ShipToAddressType shipToAddress = new ShipToAddressType();
                        String[] shipToAddressLines = { shipToStreet };
                        shipToAddress.AddressLine = shipToAddressLines;
                        shipToAddress.City = shipToCity;
                        shipToAddress.StateProvinceCode = shipToState;
                        shipToAddress.PostalCode = shipToZip;
                        shipToAddress.CountryCode = shipToCountry;

                        shipTo.Address = shipToAddress;

                        shipment.ShipTo = shipTo;



                        // -- Packages --
                        PackageType package;
                        PackageWeightType packageWeight;
                        UpsRateWebReference.CodeDescriptionType uomCodeDesc;

                        UpsRateWebReference.CodeDescriptionType packageTypeCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                        packageTypeCodeDesc.Code = "00";

                        string deliveryConfirmationOption = lstDeliveryConfirmation.SelectedValue;

                        PackageType[] packages = new PackageType[numPackages];
                        for (int i = 0; i < numPackages; i++)
                        {
                            package = new PackageType();
                            packageWeight = new PackageWeightType();
                            uomCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                            uomCodeDesc.Code = "LBS";
                            if (i == (numPackages - 1))
                            {
                                packageWeight.Weight = sLastPkgWeight;
                            }
                            else
                            {
                                packageWeight.Weight = sPkgWeight;
                            }
                            packageWeight.UnitOfMeasurement = uomCodeDesc;
                            package.PackageWeight = packageWeight;

                            package.PackagingType = packageTypeCodeDesc;

                            if (deliveryConfirmationOption != "0")
                            {
                                PackageServiceOptionsType packageServiceOptions = new PackageServiceOptionsType();
                                DeliveryConfirmationType deliveryConfirmation = new DeliveryConfirmationType();
                                deliveryConfirmation.DCISType = deliveryConfirmationOption;
                                packageServiceOptions.DeliveryConfirmation = deliveryConfirmation;
                                package.PackageServiceOptions = packageServiceOptions;
                            }

                            //Needed for GF
                            CommodityType commodity = new CommodityType();
                            commodity.FreightClass = lstLTLClass.SelectedValue;
                            package.Commodity = commodity;

                            packages[i] = package;
                        }

                        shipment.Package = packages;


                        ShipmentRatingOptionsType ratingOptions = new ShipmentRatingOptionsType();
                        ratingOptions.FRSShipmentIndicator = "";
                        ratingOptions.NegotiatedRatesIndicator = "";
                        shipment.ShipmentRatingOptions = ratingOptions;

                        UpsRateWebReference.CodeDescriptionType service = new UpsRateWebReference.CodeDescriptionType();
                        service.Code = "03";
                        shipment.Service = service;

                        UpsRateWebReference.FRSPaymentInfoType frsPaymentInfo = new UpsRateWebReference.FRSPaymentInfoType();
                        UpsRateWebReference.CodeDescriptionType frsPaymentInfoType = new UpsRateWebReference.CodeDescriptionType();

                        frsPaymentInfoType.Code = "01";
                        frsPaymentInfo.Type = frsPaymentInfoType;

                        shipment.FRSPaymentInformation = frsPaymentInfo;

                        rateRequest.Shipment = shipment;


                        // -- Submit Rate Request --

                        RateResponse rateResponse = rateService.ProcessRate(rateRequest);

                        RatedShipmentType[] ratedShipments = rateResponse.RatedShipment;

                        string requestXml = SoapTrace.TraceExtension.XmlRequest.OuterXml.ToString();
                        string responseXml = SoapTrace.TraceExtension.XmlResponse.OuterXml.ToString();

                        string targetUrl = rateService.Url;
                        string UserName = HttpContext.Current.User.Identity.Name.Replace(Config.NetworkDomain + "\\", "").ToLower();

                        // -- log request --
                        try
                        {
                        }
                        catch (Exception e)
                        {
                            handleError("ShowRates", "Error in saving request to DB: " + e.Message);
                            sbResults.Append("Error in saving request to DB: " + e.Message + "<br/>");
                        }


                        // -- Process each rated service --

                        foreach (RatedShipmentType ratedShipment in ratedShipments)
                        {
                            string serviceCode = "";
                            double totalCharges = 0;

                            serviceCode = ratedShipment.Service.Code;

                            totalCharges = Convert.ToDouble(ratedShipment.NegotiatedRateCharges.TotalCharge.MonetaryValue);

                            sbResults.Append("Total charges: " + totalCharges.ToString() + "<br/>");


                            if (ratedShipment.RatedShipmentAlert != null)
                            {
                                RatedShipmentInfoType[] ratedShipmentAlerts = ratedShipment.RatedShipmentAlert;
                                foreach (RatedShipmentInfoType ratedShipmentAlert in ratedShipmentAlerts)
                                {
                                    sbResults.Append("Shipment Alert: " + ratedShipmentAlert.Code + " - " + ratedShipmentAlert.Description + "<br/>");
                                }
                            }

                            sbResults.Append("<br/>");

                            // -- Define variables for markup calculations --
                            // -- Define variables for markup calculations --
                            double markupPercentage = 0;
                            double perPackageCharge = 0;
                            double perShipmentCharge = 0;

                            // -- Determine the UPS Standard markup percentage --
                            if (dPerPackageChargeGF.ContainsKey(currentPlant))
                            {
                                markupPercentage = Convert.ToDouble(dUpchargeGF[currentPlant]);
                                perPackageCharge = dPerPackageChargeGF[currentPlant];
                                perShipmentCharge = dPerShipmentChargeGF[currentPlant];
                            }
                            else
                            {
                                sbResults.Append("Unable to find UPS Ground Freight markup charges for plant code: " + currentPlant + "<br/>");
                            }

                            totalCharges += ((markupPercentage / 100) * totalCharges);
                            totalCharges += (perPackageCharge * numPackages) + perShipmentCharge;

                            tblShopServices.Rows.Add(currentPlant, "Ground Freight", totalCharges);

                        }





                    }
                    catch (System.Web.Services.Protocols.SoapException ex)
                    {
                        handleError("ShowRates", "Soap Exception - SoapException Message= " + ex.Message + " SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText + " SoapException XML String for all= " + ex.Detail.LastChild.OuterXml + " SoapException StackTrace= " + ex.StackTrace);
                        // -- Handle SOAP error --
                        SoapError = true;
                        sbResults.Append("---------Freight Rate Web Service returns error----------------<br/>");
                        sbResults.Append("---------\"Hard\" is user error \"Transient\" is system error----------------<br/>");
                        sbResults.Append("SoapException Message= " + ex.Message + "<br/>");
                        sbResults.Append("<br/>");
                        sbResults.Append("SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText + "<br/>");
                        sbResults.Append("<br/>");
                        sbResults.Append("SoapException XML String for all= " + ex.Detail.LastChild.OuterXml + "<br/>");
                        sbResults.Append("<br/>");
                        sbResults.Append("SoapException StackTrace= " + ex.StackTrace + "<br/>");
                        sbResults.Append("-------------------------<br/>");

                    }
                    catch (System.ServiceModel.CommunicationException ex)
                    {
                        handleError("ShowRates", "Communication Exception - CommunicationException= " + ex.Message + " CommunicationException-StackTrace= " + ex.StackTrace);
                        // -- Handle General Communication Error --
                        SoapError = true;
                        sbResults.Append("<br/>");
                        sbResults.Append("--------------------<br/>");
                        sbResults.Append("CommunicationException= " + ex.Message + "<br/>");
                        sbResults.Append("CommunicationException-StackTrace= " + ex.StackTrace + "<br/>");
                        sbResults.Append("-------------------------<br/>");
                        sbResults.Append("<br/>");

                    }
                    catch (Exception ex)
                    {
                        handleError("ShowRates", "General Error - General Exception= " + ex.Message + " General Exception-StackTrace= " + ex.StackTrace);
                        // -- Handle misc error --
                        SoapError = true;
                        sbResults.Append("<br/>");
                        sbResults.Append("-------------------------<br/>");
                        sbResults.Append(" General Exception= " + ex.Message + "<br/>");
                        sbResults.Append(" General Exception-StackTrace= " + ex.StackTrace + "<br/>");
                        sbResults.Append("-------------------------<br/>");

                    }




                }


                if (!SoapError)
                {
                    lblResultsGF.Visible = false;

                    // -- Display Service Rates in Gridview --
                    DataView dvServices = tblShopServices.DefaultView;
                    dvServices.Sort = "Plant";

                    gvGFRates.DataSource = dvServices;
                    gvGFRates.DataBind();

                    gvGFRates.Visible = true;
                    pnlGFRates.Visible = true;

                    //pnlUpsCommError.Visible = false;
                }
                else
                {
                    //lblResultsGF.Visible = true;
                    //litUpsCommError.Text = "There was a error getting Ground Freight results from UPS.  Please check your information and try again.";

                    //pnlUpsCommError.Visible = true;
                }

                // -- log results --
                try
                {
                }
                catch (Exception e)
                {
                    handleError("ShowRates", "Error in saving results to DB: " + e.Message);
                    sbResults.Append("Error in saving results to DB: " + e.Message + "<br/>");
                }


            } //end of all plants rating process


            lblResultsGF.Text += sbResults.ToString();
            // Uncomment to show results for debugging
            lblResultsGF.Visible = true;

        }

        public List<RateDetail> getGroundFreightRates_TODELETE()
        {
            List<RateDetail> rates = new List<RateDetail>();
            StringBuilder sbResults = new StringBuilder();

            bool SoapError = false;

            double shipmentWeight = 0;
            int shipmentClassification = 0;


            #region -- Define Per Package Charge and Per Shipment Charge dictionaries standard (charge per plant) --
            Dictionary<string, double> dPerPackageChargeUPS = new Dictionary<string, double>();
            Dictionary<string, double> dPerShipmentChargeUPS = new Dictionary<string, double>();


            DBUtil db = new DBUtil();

            DataSet ds = db.getPlantCharges("UPS", acctNumber);

            foreach (DataRow drCharges in ds.Tables[0].Rows)
            {
                dPerPackageChargeUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                dPerShipmentChargeUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));

                dUpchargeNextDayAirUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAir"].ToString()));
                dUpchargeSecondDayAirUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["SecondDayAir"].ToString()));
                dUpchargeGroundUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
                dUpchargeThreeDaySelectUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["ThreeDaySelect"].ToString()));
                dUpchargeNextDayAirSaverUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAirSaver"].ToString()));
                dUpchargeNextDayAirEarlyAMUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAirEarlyAM"].ToString()));
                dUpchargeSecondDayAirAMUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["SecondDayAirAM"].ToString()));
                dUpchargeSaverUPS.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Saver"].ToString()));
            }

            #endregion

            #region -- Define Per Package Charge and Per Shipment Charge dictionaries HundredWeight (charge per plant) --
            Dictionary<string, double> dPerPackageChargeCWT = new Dictionary<string, double>();
            Dictionary<string, double> dPerShipmentChargeCWT = new Dictionary<string, double>();

            Dictionary<string, double> dUpchargeNextDayAirCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSecondDayAirCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeGroundCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeThreeDaySelectCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeNextDayAirSaverCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeNextDayAirEarlyAMCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSecondDayAirAMCWT = new Dictionary<string, double>();
            Dictionary<string, double> dUpchargeSaverCWT = new Dictionary<string, double>();

            ds = db.getPlantCharges("UPSCWT", acctNumber);

            foreach (DataRow drCharges in ds.Tables[0].Rows)
            {
                dPerPackageChargeCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerPackageCharge"].ToString()));
                dPerShipmentChargeCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["PerShipmentCharge"].ToString()));

                dUpchargeNextDayAirCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAir"].ToString()));
                dUpchargeSecondDayAirCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["SecondDayAir"].ToString()));
                dUpchargeGroundCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Ground"].ToString()));
                dUpchargeThreeDaySelectCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["ThreeDaySelect"].ToString()));
                dUpchargeNextDayAirSaverCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAirSaver"].ToString()));
                dUpchargeNextDayAirEarlyAMCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["NextDayAirEarlyAM"].ToString()));
                dUpchargeSecondDayAirAMCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["SecondDayAirAM"].ToString()));
                dUpchargeSaverCWT.Add(drCharges["PlantCode"].ToString(), Convert.ToDouble(drCharges["Saver"].ToString()));
            }

            #endregion

            #region -- Calculate HundredWeight eligibility --
            if (lastPkgWeight == 0)
            {
                lastPkgWeight = pkgWeight;
            }

            int totalWeight = 0;

            if (packageWeights.Count == 0)
            {
                totalWeight = (pkgWeight * (numPackages - 1)) + lastPkgWeight;
            }
            else
            {
                foreach (int pkgWgt in packageWeights)
                {
                    totalWeight += pkgWgt;
                }
            }

            bool isAirCWT = (numPackages >= Config.MinCWTPackagesAir) && (totalWeight >= Config.MinCWTWeightAir);
            bool isGroundCWT = (numPackages >= Config.MinCWTPackagesGround) && (totalWeight >= Config.MinCWTWeightGround);

            sbResults.AppendLine("Total weight: " + totalWeight.ToString() + ", # Pkgs: " + numPackages.ToString());
            sbResults.AppendLine("Qualifies for CWT Air? " + isAirCWT.ToString());
            sbResults.AppendLine("Qualifies for CWT Grd? " + isGroundCWT.ToString());
            #endregion

            #region -- Process rate request for a single plant --
            try
            {

                int RateRequestId = 0;

                #region -- Load Ship From Address --
                string shipFromAddress = fromRate.street;
                string shipFromCity = fromRate.city;
                string shipFromState = fromRate.state;
                string shipFromZip = fromRate.zip;
                string shipFromCountry = fromRate.country;

                #endregion

                #region -- Begin building Rate Request --
                RateService rateService = new RateService();
                RateRequest rateRequest = new RateRequest();

                UpsRateWebReference.RequestType request = new UpsRateWebReference.RequestType();
                String[] requestOption = { "Shop" };
                request.RequestOption = requestOption;
                rateRequest.Request = request;
                #endregion

                #region -- Access Security (license number, username, password) --
                UpsRateWebReference.UPSSecurity upss = new UpsRateWebReference.UPSSecurity();
                UpsRateWebReference.UPSSecurityServiceAccessToken upsSvcToken = new UpsRateWebReference.UPSSecurityServiceAccessToken();
                upsSvcToken.AccessLicenseNumber = Config.UPSAccessKey;
                upss.ServiceAccessToken = upsSvcToken;
                UpsRateWebReference.UPSSecurityUsernameToken upsSecUsrnameToken = new UpsRateWebReference.UPSSecurityUsernameToken();
                upsSecUsrnameToken.Username = Config.UPSUserName;
                upsSecUsrnameToken.Password = Config.UPSPassword;
                upss.UsernameToken = upsSecUsrnameToken;
                rateService.UPSSecurityValue = upss;
                #endregion

                #region -- Build Shipment object --
                ShipmentType shipment = new ShipmentType();

                #region -- Shipper --
                ShipperType shipper = new ShipperType();
                shipper.ShipperNumber = Config.ShipFromShipperNumber;
                AddressType shipperAddress = new AddressType();
                String[] shipperAddressLine = { shipFromAddress };
                shipperAddress.AddressLine = shipperAddressLine;
                shipperAddress.City = shipFromCity;
                shipperAddress.StateProvinceCode = shipFromState;
                shipperAddress.PostalCode = shipFromZip;
                shipperAddress.CountryCode = shipFromCountry;
                shipper.Address = shipperAddress;

                shipment.Shipper = shipper;
                #endregion

                #region -- Ship To --
                ShipToType shipTo = new ShipToType();
                ShipToAddressType shipToAddress = new ShipToAddressType();
                String[] shipToAddressLines = { toRate.street };
                shipToAddress.AddressLine = shipToAddressLines;
                shipToAddress.City = toRate.city;
                shipToAddress.StateProvinceCode = toRate.state;
                shipToAddress.PostalCode = toRate.zip;
                shipToAddress.CountryCode = toRate.country;

                shipTo.Address = shipToAddress;

                shipment.ShipTo = shipTo;

                #endregion

                #region -- Packages --
                PackageType package;
                PackageWeightType packageWeight;
                UpsRateWebReference.CodeDescriptionType uomCodeDesc;

                UpsRateWebReference.CodeDescriptionType packageTypeCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                packageTypeCodeDesc.Code = "00";

                string deliveryConfirmationOption = deliveryConfCode.ToString();

                PackageType[] packages = new PackageType[numPackages];

                if (packageWeights.Count == 0)
                {
                    for (int i = 0; i < numPackages; i++)
                    {
                        package = new PackageType();
                        packageWeight = new PackageWeightType();
                        uomCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                        uomCodeDesc.Code = "LBS";
                        if (i == (numPackages - 1))
                        {
                            packageWeight.Weight = lastPkgWeight.ToString();
                        }
                        else
                        {
                            packageWeight.Weight = pkgWeight.ToString();
                        }
                        packageWeight.UnitOfMeasurement = uomCodeDesc;
                        package.PackageWeight = packageWeight;

                        package.PackagingType = packageTypeCodeDesc;

                        if (deliveryConfirmationOption != "0")
                        {
                            PackageServiceOptionsType packageServiceOptions = new PackageServiceOptionsType();
                            DeliveryConfirmationType deliveryConfirmation = new DeliveryConfirmationType();
                            deliveryConfirmation.DCISType = deliveryConfirmationOption;
                            packageServiceOptions.DeliveryConfirmation = deliveryConfirmation;
                            package.PackageServiceOptions = packageServiceOptions;
                        }

                        packages[i] = package;
                    }
                }
                else
                {
                    int i = 0;
                    foreach (int pkgWgt in packageWeights)
                    {
                        package = new PackageType();
                        packageWeight = new PackageWeightType();
                        uomCodeDesc = new UpsRateWebReference.CodeDescriptionType();
                        uomCodeDesc.Code = "LBS";

                        packageWeight.Weight = pkgWgt.ToString();
                        packageWeight.UnitOfMeasurement = uomCodeDesc;
                        package.PackageWeight = packageWeight;

                        package.PackagingType = packageTypeCodeDesc;

                        if (deliveryConfirmationOption != "0")
                        {
                            PackageServiceOptionsType packageServiceOptions = new PackageServiceOptionsType();
                            DeliveryConfirmationType deliveryConfirmation = new DeliveryConfirmationType();
                            deliveryConfirmation.DCISType = deliveryConfirmationOption;
                            packageServiceOptions.DeliveryConfirmation = deliveryConfirmation;
                            package.PackageServiceOptions = packageServiceOptions;
                        }

                        packages[i] = package;

                        i++;
                    }
                }

                shipment.Package = packages;
                #endregion

                #region -- Negotiated Rates Indicator (if needed) --
                if (isAirCWT || isGroundCWT)
                {
                    ShipmentRatingOptionsType ratingOptions = new ShipmentRatingOptionsType();
                    ratingOptions.NegotiatedRatesIndicator = "";
                    shipment.ShipmentRatingOptions = ratingOptions;
                }
                #endregion

                rateRequest.Shipment = shipment;

                #endregion

                #region -- Submit Rate Request --

                RateResponse rateResponse = rateService.ProcessRate(rateRequest);

                RatedShipmentType[] ratedShipments = rateResponse.RatedShipment;

                string requestXml = SoapTrace.TraceExtension.XmlRequest.OuterXml.ToString();
                string responseXml = SoapTrace.TraceExtension.XmlResponse.OuterXml.ToString();

                string targetUrl = rateService.Url;
                //string UserName = HttpContext.Current.User.Identity.Name.Replace(Config.NetworkDomain + "\\", "").ToLower();



                #endregion

                #region -- Define tblServices to hold rate data --

                DataSet dsServices = new DataSet();
                DataTable tblServices = dsServices.Tables.Add();

                tblServices.Columns.Add("Plant", typeof(string));
                tblServices.Columns.Add("Desc", typeof(string));
                tblServices.Columns.Add("Rate", typeof(double));
                tblServices.Columns.Add("BillingWeight", typeof(double));
                tblServices.Columns.Add("Classification", typeof(int));
                tblServices.Columns.Add("ServiceCharges", typeof(double));
                tblServices.Columns.Add("TransportationCharges", typeof(double));
                tblServices.Columns.Add("IsHundredWeight", typeof(bool));

                #endregion

                #region -- Process each rated service --

                foreach (RatedShipmentType ratedShipment in ratedShipments)
                {
                    string serviceDesc = "";
                    string serviceCWTType = "";
                    string serviceCode = "";
                    int addressClassification = 1;
                    double billingWeight = 0;
                    double serviceCharges = 0;
                    double transportationCharges = 0;
                    double totalCharges = 0;
                    double negotiatedCharges = 0;
                    bool isHundredWeight = false;

                    serviceCode = ratedShipment.Service.Code;

                    if (dServiceTypes.ContainsKey(ratedShipment.Service.Code))
                    {
                        serviceCWTType = dServiceTypes[ratedShipment.Service.Code];
                    }
                    else
                    {
                        serviceCWTType = "";
                    }

                    if (dServices.ContainsKey(ratedShipment.Service.Code))
                    {
                        serviceDesc = dServices[ratedShipment.Service.Code];
                    }
                    else
                    {
                        serviceDesc = "Unknown Service";
                    }

                    billingWeight = Convert.ToDouble(ratedShipment.BillingWeight.Weight);
                    serviceCharges = Convert.ToDouble(ratedShipment.ServiceOptionsCharges.MonetaryValue);
                    transportationCharges = Convert.ToDouble(ratedShipment.TransportationCharges.MonetaryValue);
                    totalCharges = Convert.ToDouble(ratedShipment.TotalCharges.MonetaryValue);

                    sbResults.AppendLine("Service: " + ratedShipment.Service.Code + " - " + serviceDesc);
                    sbResults.AppendLine("Billing Weight: " + billingWeight.ToString());
                    sbResults.AppendLine("Service Options Charges: " + serviceCharges.ToString());
                    sbResults.AppendLine("Transportation Charges: " + transportationCharges.ToString());
                    sbResults.AppendLine("Total charges: " + totalCharges.ToString());

                    if (ratedShipment.GuaranteedDelivery != null)
                    {
                        sbResults.AppendLine("Guaranteed Delivery / Business Days in Transit: " + ratedShipment.GuaranteedDelivery.BusinessDaysInTransit);
                        sbResults.AppendLine("Guaranteed Delivery / Delivery By Time: " + ratedShipment.GuaranteedDelivery.DeliveryByTime);
                    }
                    if (ratedShipment.NegotiatedRateCharges != null)
                    {
                        negotiatedCharges = Convert.ToDouble(ratedShipment.NegotiatedRateCharges.TotalCharge.MonetaryValue);
                        sbResults.AppendLine("Negotiated Rate Total Charges: " + negotiatedCharges.ToString());
                    }

                    if (ratedShipment.RatedShipmentAlert != null)
                    {
                        RatedShipmentInfoType[] ratedShipmentAlerts = ratedShipment.RatedShipmentAlert;
                        foreach (RatedShipmentInfoType ratedShipmentAlert in ratedShipmentAlerts)
                        {
                            if (ratedShipmentAlert.Code == "110920")
                            {
                                //Then the "Commercial" classification has changed to "Residential" so set classification to 2
                                addressClassification = 2;
                            }
                            sbResults.AppendLine("Shipment Alert: " + ratedShipmentAlert.Code + " - " + ratedShipmentAlert.Description);
                        }
                    }

                    sbResults.AppendLine();

                    #region -- Define variables for markup calculations --
                    double markupPercentage = 0;
                    double perPackageCharge = 0;
                    double perShipmentCharge = 0;

                    double markupPercentageUPS = 0;
                    double perPackageChargeUPS = 0;
                    double perShipmentChargeUPS = 0;

                    double markupPercentageCWT = 0;
                    double perPackageChargeCWT = 0;
                    double perShipmentChargeCWT = 0;
                    #endregion

                    #region -- Determine the UPS Standard markup percentage --
                    if (dPerPackageChargeUPS.ContainsKey(plantCode))
                    {
                        switch (serviceCode)
                        {
                            case "01":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeNextDayAirUPS[plantCode]);
                                break;
                            case "02":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeSecondDayAirUPS[plantCode]);
                                break;
                            case "03":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeGroundUPS[plantCode]);
                                break;
                            case "12":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeThreeDaySelectUPS[plantCode]);
                                break;
                            case "13":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeNextDayAirSaverUPS[plantCode]);
                                break;
                            case "14":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeNextDayAirEarlyAMUPS[plantCode]);
                                break;
                            case "59":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeSecondDayAirAMUPS[plantCode]);
                                break;
                            case "65":
                                markupPercentageUPS = Convert.ToDouble(dUpchargeSaverUPS[plantCode]);
                                break;
                        }

                        perPackageChargeUPS = dPerPackageChargeUPS[plantCode];
                        perShipmentChargeUPS = dPerShipmentChargeUPS[plantCode];
                    }
                    else
                    {
                        sbResults.AppendLine("Unable to find UPS Standard markup charges for plant code: " + plantCode);
                    }
                    #endregion

                    #region -- Determine the HundredWeight markup percentage --
                    if (dPerPackageChargeUPS.ContainsKey(plantCode))
                    {
                        switch (serviceCode)
                        {
                            case "01":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeNextDayAirUPS[plantCode]);
                                break;
                            case "02":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeSecondDayAirUPS[plantCode]);
                                break;
                            case "03":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeGroundUPS[plantCode]);
                                break;
                            case "12":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeThreeDaySelectUPS[plantCode]);
                                break;
                            case "13":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeNextDayAirSaverUPS[plantCode]);
                                break;
                            case "14":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeNextDayAirEarlyAMUPS[plantCode]);
                                break;
                            case "59":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeSecondDayAirAMUPS[plantCode]);
                                break;
                            case "65":
                                markupPercentageCWT = Convert.ToDouble(dUpchargeSaverUPS[plantCode]);
                                break;
                        }

                        perPackageChargeCWT = dPerPackageChargeCWT[plantCode];
                        perShipmentChargeCWT = dPerShipmentChargeCWT[plantCode];
                    }
                    else
                    {
                        sbResults.AppendLine("Unable to find HundredWeight markup charges for plant code: " + plantCode);
                    }
                    #endregion

                    //if shipment qualifies for hundredweight air and hundredweight method class is air, consider it hundredweight
                    //if shipment qualifies for hundredweight ground and hundredweight method class is ground, consider it hundredweight

                    if (isAirCWT && (serviceCWTType == "AIR-NN"))
                    {
                        isHundredWeight = true;
                        //Do not adjust total charges
                    }
                    else if ((isAirCWT && (serviceCWTType == "AIR")) || (isGroundCWT && (serviceCWTType == "GROUND")))
                    {
                        isHundredWeight = true;

                        // Since our negotiated CWT rates is 70% of the published rate, reverse that and divide neg. rate by .7 to get published rate
                        totalCharges = negotiatedCharges / .7;
                    }
                    else
                    {
                        isHundredWeight = false;
                    }

                    if (isHundredWeight)
                    {
                        markupPercentage = markupPercentageCWT;
                        perPackageCharge = perPackageChargeCWT;
                        perShipmentCharge = perShipmentChargeCWT;
                    }
                    else
                    {
                        markupPercentage = markupPercentageUPS;
                        perPackageCharge = perPackageChargeUPS;
                        perShipmentCharge = perShipmentChargeUPS;
                    }

                    totalCharges += ((markupPercentage / 100) * totalCharges);
                    totalCharges += (perPackageCharge * numPackages) + perShipmentCharge;

                    //tblServices.Rows.Add(plantCode, serviceDesc, totalCharges, billingWeight, addressClassification, serviceCharges, transportationCharges, isHundredWeight);
                    RateDetail currentRate = new RateDetail(plantCode, serviceDesc, addressClassification, int.Parse(billingWeight.ToString()), isHundredWeight, decimal.Parse(totalCharges.ToString()), decimal.Parse(serviceCharges.ToString()), decimal.Parse(transportationCharges.ToString()), "UPS");
                    rates.Add(currentRate);

                    shipmentWeight = billingWeight;
                    shipmentClassification = addressClassification;
                }
                #endregion


            }
            catch (System.Web.Services.Protocols.SoapException ex)
            {
                if ((ex.Detail.LastChild.InnerText != "Hard110003Maximum number of packages exceeded (50)") //Do not show "scary" red message if only soap error is that it exceeds 50 packages (also catch 200 pkg message)
                    && (ex.Detail.LastChild.InnerText != "Hard110003Maximum number of packages exceeded (200)"))
                {
                    //WRITE ERROR TO REQUEST
                    handleError("ShowRates", "Soap Exception - SoapException Message= " + ex.Message + " SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText + " SoapException XML String for all= " + ex.Detail.LastChild.OuterXml + " SoapException StackTrace= " + ex.StackTrace);
                }
                #region -- Handle SOAP error --
                SoapError = true;
                sbResults.AppendLine("---------Freight Rate Web Service returns error----------------");
                sbResults.AppendLine("---------\"Hard\" is user error \"Transient\" is system error----------------");
                sbResults.AppendLine("SoapException Message= " + ex.Message);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException Category:Code:Message= " + ex.Detail.LastChild.InnerText);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException XML String for all= " + ex.Detail.LastChild.OuterXml);
                sbResults.AppendLine();
                sbResults.AppendLine("SoapException StackTrace= " + ex.StackTrace);
                sbResults.AppendLine("-------------------------");
                #endregion
            }

            catch (Exception ex)
            {
                //WRITE ERROR TO REQUEST
                handleError("ShowRates", "General Error - General Exception= " + ex.Message + " General Exception-StackTrace= " + ex.StackTrace);
                SoapError = true;
                #region -- Handle misc error --
                sbResults.AppendLine();
                sbResults.AppendLine("-------------------------");
                sbResults.AppendLine(" General Exception= " + ex.Message);
                sbResults.AppendLine(" General Exception-StackTrace= " + ex.StackTrace);
                sbResults.AppendLine("-------------------------");
                #endregion
            }

            #endregion



            #region -- Display our Rating Results --

            //lblBillingWeight.Text = shipmentWeight.ToString();

            //lblNumPackages.Text = numPackages.ToString();

            //if (shipmentClassification == 2)
            //{
            //    lblAddressClassification.Text = "RESIDENTIAL";
            //}
            //else if (shipmentClassification == 1)
            //{
            //    lblAddressClassification.Text = "COMMERCIAL";
            //}
            //else
            //{
            //    lblAddressClassification.Text = "UNKNOWN";
            //}

            //if (addressCorrected)
            //{
            //   lblRateWarning.Text = "The address was automatically corrected to match what UPS had on file.  Please make note of the changes.<br/>";
            //}
            //else if (shipToStreet == "")
            //{
            //    lblRateWarning.Text = "This is an estimate based on the Zip Code provided.  Please be aware additional charges may apply if the address is residential, in an extended delivery area, etc.<br/>";
            //}
            //else
            //{
            //    lblRateWarning.Text = "";
            //}

            #endregion

            #region -- Show Results View --
            //mvPageLayout.SetActiveView(vwRates);
            //pnlUpsTrademarkInfo.Visible = true;
            #endregion

            Console.WriteLine(sbResults.ToString());
            //lblResults.Text += sbResults.ToString();



            return (rates);
        }
        */
    }
}
