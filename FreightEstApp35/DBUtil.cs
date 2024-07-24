using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace FreightEstApp35
{
    class DBUtil
    {
        string connString = Config.ConnString;

        public DBUtil()
        {
        }

        public DataSet getNextRequestFromQueue()
        {
            //WiseTools.logToFile(Config.logFile, "getNextRequestFromQueue", true);
            SqlConnection conn = new SqlConnection(connString);
            //SqlCommand cmd = new SqlCommand("getNextRequestFromQueue", conn);
            //cmd.CommandType = CommandType.StoredProcedure;

            //string sqlTempFix = "UPDATE " + Config.RemoteServerName + ".CostPlus.dbo.FreightRequests SET FromCity = 'Fort Wayne' WHERE FromCity = 'Ft Wayne'";
            string sqlTempFix = "UPDATE UPSRATE.dbo.FreightRequests_" + Config.RemoteServerName + " SET FromCity = 'Fort Wayne' WHERE FromCity = 'Ft Wayne'";

            SqlCommand cmdTempFix = new SqlCommand(sqlTempFix, conn);
            cmdTempFix.CommandType = CommandType.Text;
            conn.Open();
            cmdTempFix.ExecuteNonQuery();
            conn.Close();


            string sql = "SELECT TOP 1 LoginId, QtyNumber, ToAddress, ToCity, ToState, ToZip, ToCountry, FromAddress, ";
            //    FromCity, ";
            sql += "CASE WHEN FromCity = 'Ft Wayne' THEN 'Fort Wayne' ELSE FromCity END as FromCity, ";
            sql += "FromState, FromZip, FromCountry, NumPackages, PkgWeight, LastPkgWeight, RequestUPS, RequestLTL, FreightClass, ";
            sql += "PickupDate, NotifyBeforeDelivery, LiftgatePickup, LiftgateDelivery, LimitedAccessPickup, LimitedAccessDelivery, ";
            sql += "ResidentialPickup, ResidentialDelivery, InsidePickup, InsideDelivery, SortAndSegregate, StopoffCharge, DateRequested, ";
			sql += "DateProcessed, AcctNumber, ShipWithArray ";
            //sql += "FROM " + Config.RemoteServerName + ".CostPlus.dbo.FreightRequests ";
            sql += "FROM UPSRATE.dbo.FreightRequests_" + Config.RemoteServerName + " ";


            sql += "WHERE (DateProcessed IS NULL OR DateRated IS NULL) ";//and loginid ='smacphail' ";
            sql += "AND ISNULL(NumPackages,0) > 0 ";
            sql += "ORDER BY DateRequested ASC";

            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;

            DataSet ds = new DataSet();

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            return (ds);
        }

        public DataSet getPlantCharges(string carrier, int acctNumber)
        {
            /*
            SqlConnection conn = new SqlConnection(connString);
            string sql = "SELECT PlantCode, PerPackageCharge, PerShipmentCharge, NextDayAir, SecondDayAir, Ground, ThreeDaySelect, NextDayAirSaver, NextDayAirEarlyAM, SecondDayAirAM, Saver FROM PlantCarrierCharges WHERE CarrierId = @carrier ORDER BY PlantCode";

            SqlCommand cmdCharges = new SqlCommand(sql, conn);
            cmdCharges.CommandType = CommandType.Text;
            cmdCharges.Parameters.Add("@carrier", SqlDbType.VarChar, 10).Value = carrier;

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmdCharges);
            da.Fill(ds);

            return (ds);
            */

            SqlConnection conn = new SqlConnection(connString);
            
            string sql = "GetPlantCharges";

            SqlCommand cmdCharges = new SqlCommand(sql, conn);
            cmdCharges.CommandType = CommandType.StoredProcedure;

            cmdCharges.Parameters.Add("@Carrier", SqlDbType.VarChar, 50).Value = carrier;
            cmdCharges.Parameters.Add("@AcctNumber", SqlDbType.Int).Value = acctNumber;

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmdCharges);
            da.Fill(ds);

            return (ds);
        }
        /*
        public DataSet getRequestsToProcess()
        {
            SqlConnection conn = new SqlConnection(connString);
            string sql = "NOLONGERUSED";// "SELECT LoginId, QtyNumber, ToAddress, ToCity, ToState, ToZip, ToCountry, NumPackages, PkgWeight, LastPkgWeight FROM FreightRequests WHERE DateProcessed IS NULL AND DateRated IS NULL AND ISNULL(NumPackages,0) > 0 ORDER BY DateRequested ASC";
            
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            return (ds);
        }
        */

        internal void saveResults(string source, string uniqueId, List<string[]> ratesToSave, Address toAddress)
        {
            //WiseTools.logToFile(Config.logFile, "Starting saveResults", true);
            SqlConnection conn = new SqlConnection(connString);
            
            string sql = "UPDATE UPSRATE.dbo.FreightRequests_" + Config.RemoteServerName + " SET Carrier01 = @Carrier01, Service01 = @Service01, ServiceCode01 = @ServiceCode01, Rate01 = @Rate01, Note01 = @Note01, ";
            sql += "Carrier02 = @Carrier02, Service02 = @Service02, ServiceCode02 = @ServiceCode02, Rate02 = @Rate02, Note02 = @Note02, ";
            sql += "Carrier03 = @Carrier03, Service03 = @Service03, ServiceCode03 = @ServiceCode03, Rate03 = @Rate03, Note03 = @Note03, ";
            sql += "Carrier04 = @Carrier04, Service04 = @Service04, ServiceCode04 = @ServiceCode04, Rate04 = @Rate04, Note04 = @Note04, ";
            sql += "Carrier05 = @Carrier05, Service05 = @Service05, ServiceCode05 = @ServiceCode05, Rate05 = @Rate05, Note05 = @Note05, ";
            sql += "Carrier06 = @Carrier06, Service06 = @Service06, ServiceCode06 = @ServiceCode06, Rate06 = @Rate06, Note06 = @Note06, ";
            sql += "Carrier07 = @Carrier07, Service07 = @Service07, ServiceCode07 = @ServiceCode07, Rate07 = @Rate07, Note07 = @Note07, ";
            sql += "Carrier08 = @Carrier08, Service08 = @Service08, ServiceCode08 = @ServiceCode08, Rate08 = @Rate08, Note08 = @Note08, ";
            sql += "Carrier09 = @Carrier09, Service09 = @Service09, ServiceCode09 = @ServiceCode09, Rate09 = @Rate09, Note09 = @Note09, ";
            sql += "Carrier10 = @Carrier10, Service10 = @Service10, ServiceCode10 = @ServiceCode10, Rate10 = @Rate10, Note10 = @Note10, ";
            sql += "Carrier11 = @Carrier11, Service11 = @Service11, ServiceCode11 = @ServiceCode11, Rate11 = @Rate11, Note11 = @Note11, ";
            sql += "Carrier12 = @Carrier12, Service12 = @Service12, ServiceCode12 = @ServiceCode12, Rate12 = @Rate12, Note12 = @Note12, ";
            sql += "Carrier13 = @Carrier13, Service13 = @Service13, ServiceCode13 = @ServiceCode13, Rate13 = @Rate13, Note13 = @Note13, ";
            sql += "Carrier14 = @Carrier14, Service14 = @Service14, ServiceCode14 = @ServiceCode14, Rate14 = @Rate14, Note14 = @Note14, ";
            sql += "Carrier15 = @Carrier15, Service15 = @Service15, ServiceCode15 = @ServiceCode15, Rate15 = @Rate15, Note15 = @Note15, ";
            sql += "ClassifiedResidential = @ClassifiedResidential, SubstitutedZip = @SubstitutedZip, ";
            sql += "ErrorCode01 = NULL, ErrorDesc01 = NULL, ";
            sql += "ErrorCode02 = NULL, ErrorDesc02 = NULL, ";
            sql += "ErrorCode03 = NULL, ErrorDesc03 = NULL, ";
            sql += "DateProcessed = GETDATE(), DateRated = GETDATE() ";
            sql += "WHERE LoginId = @source AND QtyNumber = @uniqueId";


            //WiseTools.logToFile(Config.logFile, sql, true);

            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 300;

            cmd.Parameters.Add("@source", SqlDbType.VarChar, 50).Value = source;
            cmd.Parameters.Add("@uniqueId", SqlDbType.TinyInt).Value = int.Parse(uniqueId);

            string substitutedZip = "";
            if (toAddress.zip.IndexOf("-") < 0)
            {
                substitutedZip = verifyAndCorrectZipCode(toAddress.city, toAddress.state, toAddress.zip);
            }

            cmd.Parameters.Add("@SubstitutedZip", SqlDbType.VarChar, 50).Value = substitutedZip;

            if (ratesToSave.Count > 0)
            {
                cmd.Parameters.Add("@ClassifiedResidential", SqlDbType.Bit).Value = (ratesToSave[0][4] == "2"); //If address classification is 2, then this was classified residential
            }
            else
            {
                cmd.Parameters.Add("@ClassifiedResidential", SqlDbType.Bit).Value = false;
            }

            int loopNumber = 0;
            foreach (string[] rateInfo in ratesToSave)
            {
                loopNumber++;
                string loopString = loopNumber.ToString();
                if (loopString.Length < 2) { loopString = "0" + loopString; }

                if(rateInfo[5].Length > 200) { rateInfo[5] = rateInfo[5].Substring(0, 200); }

                cmd.Parameters.Add("@Carrier" + loopString, SqlDbType.VarChar, 50).Value = rateInfo[0];
                cmd.Parameters.Add("@Service" + loopString, SqlDbType.VarChar, 50).Value = rateInfo[3];
                cmd.Parameters.Add("@Rate" + loopString, SqlDbType.Decimal).Value = decimal.Parse(rateInfo[2]);
                cmd.Parameters.Add("@ServiceCode" + loopString, SqlDbType.VarChar, 50).Value = rateInfo[1];
                cmd.Parameters.Add("@Note" + loopString, SqlDbType.VarChar, 200).Value = rateInfo[5];
            }

            while (loopNumber < 15)
            {
                loopNumber++;
                string loopString = loopNumber.ToString();
                if (loopString.Length < 2) { loopString = "0" + loopString; }

                cmd.Parameters.Add("@Carrier" + loopString, SqlDbType.VarChar, 50).Value = DBNull.Value;
                cmd.Parameters.Add("@Service" + loopString, SqlDbType.VarChar, 50).Value = DBNull.Value;
                cmd.Parameters.Add("@Rate" + loopString, SqlDbType.Decimal).Value = DBNull.Value;
                cmd.Parameters.Add("@ServiceCode" + loopString, SqlDbType.VarChar, 50).Value = DBNull.Value;
                cmd.Parameters.Add("@Note" + loopString, SqlDbType.VarChar, 200).Value = DBNull.Value;
            }


            //WiseTools.logToFile(Config.logFile, "About to execute sql in saveResults", true);

            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();            
            
            //WiseTools.logToFile(Config.logFile, "Completed saveResults", true);


        }


        internal void saveErrorMessage(string source, string uniqueId, string errorCode, string errorMessage)
        {
            SqlConnection conn = new SqlConnection(connString);
            //string sql = "UPDATE " + Config.RemoteServerName + ".CostPlus.dbo.FreightRequests SET ErrorCode01 = @ErrorCode01, ErrorDesc01 = @ErrorDesc01, ";
            string sql = "UPDATE UPSRATE.dbo.FreightRequests_" + Config.RemoteServerName + " SET ErrorCode01 = @ErrorCode01, ErrorDesc01 = @ErrorDesc01, ";
            sql += "ErrorCode02 = NULL, ErrorDesc02 = NULL, ";
            sql += "ErrorCode03 = NULL, ErrorDesc03 = NULL, ";
            sql += "DateProcessed = GETDATE(), DateRated = GETDATE() ";
            sql += "WHERE LoginId = @source AND QtyNumber = @uniqueId";

            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;

            cmd.Parameters.Add("@source", SqlDbType.VarChar, 50).Value = source;
            cmd.Parameters.Add("@uniqueId", SqlDbType.TinyInt).Value = int.Parse(uniqueId);
            cmd.Parameters.Add("@ErrorCode01", SqlDbType.VarChar, 10).Value = errorCode;
            cmd.Parameters.Add("@ErrorDesc01", SqlDbType.VarChar, 1000).Value = errorMessage;

            conn.Open();
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        internal string getProviderAbbrev(string fullProvider)
        {
            string abbrev = "";

            try
            {
                SqlConnection conn = new SqlConnection(connString);
                //string sql = "SELECT @Abbrev = FreightAbbreviation FROM " + Config.RemoteServerName + ".CostPlus.dbo.FreightProviderAbbreviations ";
                string sql = "SELECT @Abbrev = FreightAbbreviation FROM SUWDB03.UPSRate.dbo.FreightProviderAbbreviations ";
                sql += "WHERE FreightProvider = @FreightProvider";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add("@FreightProvider", SqlDbType.VarChar, 100).Value = fullProvider;
                cmd.Parameters.Add("@Abbrev", SqlDbType.VarChar, 10).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                abbrev = cmd.Parameters["@Abbrev"].Value.ToString();

                conn.Close();
            }
            catch (Exception err)
            {
                WiseTools.logToFile(Config.logFile, "getProviderAbbrev - provider = " + fullProvider + " - Error encountered: " + err.ToString(), true);
            }

            return (abbrev);
        }

        public string verifyAndCorrectZipCode(string originalCity, string originalState, string originalZip)
        {
            string correctZipCode = "";

            try
            {
                SqlConnection conn = new SqlConnection(connString);
                string sql = "VerifyAndCorrect_PO_ZipCode";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@City", SqlDbType.VarChar, 200).Value = originalCity;
                cmd.Parameters.Add("@State", SqlDbType.VarChar, 50).Value = originalState;   
                cmd.Parameters.Add("@ZipCode", SqlDbType.VarChar, 50).Value = originalZip;   
                cmd.Parameters.Add("@CorrectZipCode", SqlDbType.VarChar, 50).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                correctZipCode = cmd.Parameters["@CorrectZipCode"].Value.ToString();

                conn.Close();
            }
            catch (Exception err)
            {
                WiseTools.logToFile(Config.logFile, "verifyAndCorrectZipCode - Error encountered: " + err.ToString(), true);
            }

            if (correctZipCode.Trim() == originalZip.Trim())
            {
                correctZipCode = "";
            }

            return (correctZipCode);

        }

        internal string getProviderDesc(string fullProvider)
        {
            string abbrev = "";

            try
            {
                SqlConnection conn = new SqlConnection(connString);
                //string sql = "SELECT @Desc = FreightDesc FROM " + Config.RemoteServerName + ".CostPlus.dbo.FreightProviderAbbreviations ";
                string sql = "SELECT @Desc = FreightDesc FROM SUWDB03.UPSRate.dbo.FreightProviderAbbreviations ";
                sql += "WHERE FreightProvider = @FreightProvider";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add("@FreightProvider", SqlDbType.VarChar, 100).Value = fullProvider;
                cmd.Parameters.Add("@Desc", SqlDbType.VarChar, 50).Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                abbrev = cmd.Parameters["@Desc"].Value.ToString();

                conn.Close();
            }
            catch (Exception err)
            {
                WiseTools.logToFile(Config.logFile, "getProviderDesc - provider = " + fullProvider + " - Error encountered: " + err.ToString(), true);
            }

            return (abbrev);
        }

        internal DataSet getFreightProviderInfo()
        {

            WiseTools.logToFile(Config.logFile, "Starting getFreightProviderInfo", true);

            SqlConnection conn = new SqlConnection(connString);
            //string sql = "SELECT * FROM " + Config.RemoteServerName + ".CostPlus.dbo.FreightProviderAbbreviations ORDER BY FreightProvider";
            string sql = "SELECT * FROM SUWDB03.UPSRate.dbo.FreightProviderAbbreviations ORDER BY FreightProvider";

            SqlCommand cmdProviders = new SqlCommand(sql, conn);
            cmdProviders.CommandType = CommandType.Text;

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmdProviders);
            da.Fill(ds);

            WiseTools.logToFile(Config.logFile, "Finished getFreightProviderInfo", true);

            return (ds);
        }
    }
}
