using System;
using System.Collections.Generic;

namespace FreightEstApp35
{
    class Program
    {
        static void Main(string[] args)
        {
            Config.ResolveEnvironment();
            StartupBanner.Print();

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
                //WiseTools.logToFile(Config.logFile, "Request found - processing....", true);

                if (((myRequest.numPackages - 1) * myRequest.pkgWeight) + myRequest.lastPkgWeight > 19999)
                {
                    myRequest.writeError("WEIGHT", "Weight Exceeds Limit – Call");

                    //WiseTools.logToFile(Config.logFile, "Weight exceeds limit", true);
                }
                else
                {
                    try
                    {
                        Console.WriteLine("Number of Packages: " + myRequest.numPackages);
                        Console.WriteLine("Package weight: " + myRequest.pkgWeight);
                        Console.WriteLine("Last Package weight: " + myRequest.lastPkgWeight);
                        Console.WriteLine("Plant: " + myRequest.fromPlant);

                        //WiseTools.logToFile(Config.logFile, "Beginning UPS address validation", true);
                        
                        UpsComm rates = new UpsComm();

                        rates.CorrelationId = myRequest.source + "-" + myRequest.uniqueId;

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
                            }
                            else
                            {
                                rates.toRate = myRequest.toAddress;
                            }

                            List<RateDetail> upsRates = new List<RateDetail>();
                            List<RateDetail> groundFreightRates = new List<RateDetail>();
                            List<RateDetail> ltlRates = new List<RateDetail>();

                            if (myRequest.requestUPS)
                            {
                                upsRates = rates.getRates();

                                foreach (RateDetail rate in upsRates)
                                {
                                    Console.WriteLine(rate.basicProvider + " " + rate.basicMethod + " " + rate.totalCharges);
                                }
                            }
                            
                            if (myRequest.requestLTL)
                            {
                                groundFreightRates = rates.getGroundFreightRates();

                                foreach (RateDetail rate in groundFreightRates)
                                {
                                    Console.WriteLine(rate.basicProvider + " " + rate.basicMethod + " " + rate.totalCharges + " " + rate.note);
                                    ltlRates.Add(rate);
                                }

                                foreach (RateDetail rate in rates.getLTLRates_TransportationInsight(rates))
                                {
                                    Console.WriteLine(rate.basicProvider + " " + rate.basicMethod + " " + rate.basicRate + " " + rate.note);
                                    rate.totalCharges = rate.basicRate;
                                    ltlRates.Add(rate);
                                }

                            }                            

                            Console.WriteLine("Address: " + myRequest.toAddress.street);
                            myRequest.saveResults(upsRates, ltlRates);
                        }
                        else
                        {
                            //WRITE ERROR TO REQUEST
                            myRequest.writeError("ADDRESS", "Invalid address -- unable to validate");
                        }
                    }
                    catch (Exception err)
                    {
                        //WRITE ERROR TO REQUEST
                        myRequest.writeError("GENERAL", "Error processing request: processNextRequest() " + err.Message);
                    }
                }

            }
            else
            {
                System.Threading.Thread.Sleep(250);
            }
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
