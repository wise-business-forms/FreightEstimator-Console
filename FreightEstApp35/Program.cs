using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Net;

namespace FreightEstApp35
{
    class Program
    {
        static void Main(string[] args)
        {
            WiseTools.logToFile(Config.logFile, "Launching application: " + Config.ENVIRONMENT, true);
            Console.WriteLine("Launching application: " + Config.ENVIRONMENT);
            //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            //Console.WriteLine("Security Protocol configured.");
            // Sets environment flag [PROD|DEBUG] based on host name.
            Console.WriteLine("Set Prod/Debug Flag.");
            Config.SetProdDebug();            

            while (1 == 1)
            {
                processNextRequest();        
            }
        }


        static void processNextRequest()
        {
            Request myRequest = new Request(true);

            if (myRequest.isLoaded)
            {
                WiseTools.logToFile(Config.logFile, "Request found - processing....", true);

                if (((myRequest.numPackages - 1) * myRequest.pkgWeight) + myRequest.lastPkgWeight > 19999)
                {
                    myRequest.writeError("WEIGHT", "Weight Exceeds Limit – Call");

                    WiseTools.logToFile(Config.logFile, "Weight exceeds limit", true);
                }
                else
                {

                    try
                    {
                        Console.WriteLine("Number of Packages: " + myRequest.numPackages);
                        Console.WriteLine("Package weight: " + myRequest.pkgWeight);
                        Console.WriteLine("Last Package weight: " + myRequest.lastPkgWeight);
                        Console.WriteLine("Plant: " + myRequest.fromPlant);

                        WiseTools.logToFile(Config.logFile, "Beginning UPS address validation", true);
                        
                        UpsComm rates = new UpsComm();

                        rates.toValidate = myRequest.toAddress;
                        rates.fromRate = myRequest.fromAddress;

                        rates.ltlClass = myRequest.freightClass;
                        rates.deliveryConfCode = 0;// myRequest.signatureRequired ? 1 : 0;
                        rates.pickupDate = DateTime.Parse(myRequest.pickupDate);
                        rates.plantCode = myRequest.fromPlant;

                        rates.packageWeights = myRequest.packageWeights;

                        if (myRequest.packageWeights.Count == 0)
                        {
                            rates.numPackages = myRequest.numPackages;
                            rates.pkgWeight = myRequest.pkgWeight;
                            rates.lastPkgWeight = myRequest.lastPkgWeight;
                        } 
                        else
                        {
                            //myUPS.numPackages = myRequest.packageWeights.Count;
                            //myUPS.pkgWeight = 0;
                            //myUPS.lastPkgWeight = 0;
                            rates.numPackages = myRequest.numPackages;
                            rates.pkgWeight = myRequest.pkgWeight;
                            rates.lastPkgWeight = myRequest.lastPkgWeight;
                        }

                        rates.accessorials = myRequest.accessorials;

                        rates.acctNumber = myRequest.acctNumber;

                        List<Address> candidates = rates.validateAddress();

                        if (candidates.Count <= 1)
                        {
                            if (candidates.Count == 1)
                            {
                                rates.toRate = candidates[0];

                                WiseTools.logToFile(Config.logFile, "Address corrected", true);
                            }
                            else
                            {
                                rates.toRate = myRequest.toAddress;

                                WiseTools.logToFile(Config.logFile, "Entered address being used", true);
                            }

                            List<RateDetail> upsRates = new List<RateDetail>();
                            List<RateDetail> groundFreightRates = new List<RateDetail>();
                            List<RateDetail> ltlRates = new List<RateDetail>();

                            if (myRequest.requestUPS)
                            {
                                WiseTools.logToFile(Config.logFile, "Requesting UPS rates", true);
                                upsRates = rates.getRates();

                                foreach (RateDetail rate in upsRates)
                                {
                                    Console.WriteLine(rate.basicProvider + " " + rate.basicMethod + " " + rate.basicRate.ToString());
                                }

                                WiseTools.logToFile(Config.logFile, "Done with UPS rates", true);
                            }
                            
                            if (myRequest.requestLTL)
                            {
                                WiseTools.logToFile(Config.logFile, "Requesting UPS Ground Freight rates", true);
                                groundFreightRates = rates.getGroundFreightRates();

                                foreach (RateDetail rate in groundFreightRates)
                                {
                                    Console.WriteLine(rate.basicProvider + " " + rate.basicMethod + " " + rate.basicRate.ToString() + " " + rate.note);
                                    ltlRates.Add(rate);
                                }
                                WiseTools.logToFile(Config.logFile, "Done with UPS Ground Freight rates", true);

                                WiseTools.logToFile(Config.logFile, "Requesting LTL rates", true);                                                                

                                foreach (RateDetail rate in rates.getLTLRates_TransportationInsight(rates))
                                {
                                    Console.WriteLine(rate.basicProvider + " " + rate.basicMethod + " " + rate.basicRate.ToString() + " " + rate.note);
                                    ltlRates.Add(rate);
                                }

                                WiseTools.logToFile(Config.logFile, "Done with LTL rates", true);
                            }

                            Console.WriteLine("Address: " + myRequest.toAddress.street);
                            //WiseTools.logToFile(Config.logFile, "About to call routine to save results", true);
                            myRequest.saveResults(upsRates, ltlRates);

                            //WiseTools.logToFile(Config.logFile, "Back from routine that saves results", true);

                        }
                        else
                        {
                            //WRITE ERROR TO REQUEST
                            //Console.WriteLine("Invalid address -- unable to validate");
                            myRequest.writeError("ADDRESS", "Invalid address -- unable to validate");

                            WiseTools.logToFile(Config.logFile, "ERROR - bad address", true);
                        }
                    }
                    catch (Exception err)
                    {
                        //WRITE ERROR TO REQUEST
                        myRequest.writeError("GENERAL", "Error processing request: processNextRequest() " + err.Message);
                        WiseTools.logToFile(Config.logFile, "General error encountered: " + err.ToString(), true);
                    }
                }

            }
            else
            {
                System.Threading.Thread.Sleep(250);
            }
            //LoginId, QtyNumber, ToAddress, ToCity, ToState, ToZip, ToCountry, NumPackages, PkgWeight, LastPkgWeight
        }

        static void testRoutine()
        {

            Request myRequest = new Request(true);
            if (myRequest.isLoaded)
            {
                //Console.Write(myRequest.toAddress.zip);
            }
            else
            {
                //Console.Write("No request loaded");
            }

            UpsComm myUPS = new UpsComm();

            myUPS.toValidate = new Address();
            myUPS.toValidate.street = "";//502 Sapphire Valley Ln";
            myUPS.toValidate.city = "FRESNO";
            myUPS.toValidate.state = "CA";
            myUPS.toValidate.zip = "93725";
            myUPS.toValidate.country = "US";

            List<Address> candidates = myUPS.validateAddress();

            if (candidates.Count <= 1)
            {
                if (candidates.Count == 1)
                {
                    myUPS.toRate = candidates[0];
                }
                else
                {
                    myUPS.toRate = myUPS.toValidate;
                }

                myUPS.fromRate = new Address();
                myUPS.fromRate.street = "555 McFarland 400 Drive";
                myUPS.fromRate.city = "Alpharetta";
                myUPS.fromRate.state = "GA";
                myUPS.fromRate.zip = "30004";
                myUPS.fromRate.country = "US";

                myUPS.ltlClass = "50";
                myUPS.deliveryConfCode = 0;
                myUPS.pickupDate = DateTime.Parse(DateTime.Now.ToShortDateString());

                myUPS.plantCode = "ALP";

                myUPS.numPackages = 300;
                myUPS.pkgWeight = 30;
                myUPS.lastPkgWeight = 0;
                myUPS.accessorials = "";
                /*
                List<RateDetail> rates = myUPS.getRates();

                foreach (RateDetail rate in rates)
                {
                    Console.WriteLine(rate.basicProvider + " " + rate.basicMethod + " " + rate.basicRate.ToString());
                }
                */
                //List<RateDetail> ltlRates = myUPS.getLTLRates(myUPS);
                List<RateDetail> ltlRates = myUPS.getLTLRates_TransportationInsight(myUPS);
                foreach (RateDetail rate in ltlRates)
                {
                    Console.WriteLine(rate.basicProvider + " " + rate.basicMethod + " " + rate.basicRate.ToString());
                }

            }
            else
            {
                //Invalid address
            }

            Console.ReadKey();
        }
    }
}
