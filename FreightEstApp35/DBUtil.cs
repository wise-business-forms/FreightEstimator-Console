using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Newtonsoft.Json.Linq;

namespace FreightEstApp35
{
    class DBUtil
    {
        string connString = Config.ConnString;

        public DataSet getNextRequestFromQueue()
        {
            SqlConnection conn = new SqlConnection(connString);
            string sql = "SELECT TOP 1 LoginId, QtyNumber, ToAddress, ToCity, ToState, ToZip, ToCountry, FromAddress, ";
            sql += "CASE WHEN FromCity = 'Ft Wayne' THEN 'Fort Wayne' ELSE FromCity END as FromCity, ";
            sql += "FromState, FromZip, FromCountry, NumPackages, PkgWeight, LastPkgWeight, RequestUPS, RequestLTL, FreightClass, ";
            sql += "PickupDate, NotifyBeforeDelivery, LiftgatePickup, LiftgateDelivery, LimitedAccessPickup, LimitedAccessDelivery, ";
            sql += "ResidentialPickup, ResidentialDelivery, InsidePickup, InsideDelivery, SortAndSegregate, StopoffCharge, DateRequested, ";
			sql += "DateProcessed, AcctNumber, ShipWithArray ";
            sql += "FROM " + Config.RemoteServerName + ".CostPlus.dbo.FreightRequests ";
            sql += "WHERE (DateProcessed IS NULL OR DateRated IS NULL) ";
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
        

        internal void saveResults(string source, string uniqueId, List<string[]> ratesToSave, Address toAddress)
        {
            try
            {
                string substitutedZip = "";
                bool _residential = false;

                substitutedZip = toAddress.zip;

                if (ratesToSave.Count > 0)
                {
                    _residential = (ratesToSave[0][4] == "2"); //If address classification is 2, then this was classified residential
                }
                else
                {
                    _residential = false;
                }
                
                StringBuilder sql = new StringBuilder("UPDATE ");
                sql.Append(Config.RemoteServerName);
                sql.Append(".CostPlus.dbo.FreightRequests SET ");
                int i = 1;
                foreach (string[] rate in ratesToSave)
                {
                    sql.Append("Carrier");
                    sql.Append(i.ToString("D2"));
                    sql.Append(" = '");
                    sql.Append(rate[0]);
                    sql.Append("',");
                    sql.Append("Service");
                    sql.Append(i.ToString("D2"));
                    sql.Append(" = '");
                    sql.Append(rate[1]);
                    sql.Append("',");
                    //sql.Append("ServiceCode");
                    //sql.Append(i.ToString("D2"));
                    //sql.Append(" = ");
                    //sql.Append(rate[2]);
                    //sql.Append(",");
                    sql.Append("Rate");
                    sql.Append(i.ToString("D2"));
                    sql.Append(" = ");
                    sql.Append(rate[2]);
                    sql.Append(',');
                    sql.Append("Note");
                    sql.Append(i.ToString("D2"));
                    sql.Append(" = ");
                    sql.Append(rate[4]);
                    sql.Append(',');
                    i++;
                }
                sql.Append("ClassifiedResidential=").Append(_residential ? 1 : 0).Append(",");
                sql.Append("SubstitutedZip=").Append(substitutedZip).Append(",");
                sql.Append("ErrorCode01=").Append("NULL,");
                sql.Append("ErrorDesc01=").Append("NULL,");
                sql.Append("ErrorCode02=").Append("NULL,");
                sql.Append("ErrorDesc02=").Append("NULL,");
                sql.Append("ErrorCode03=").Append("NULL,");
                sql.Append("ErrorDesc03=").Append("NULL,");
                sql.Append("DateProcessed=GETDATE(),");
                sql.Append("DateRated=GETDATE() ");
                sql.Append("WHERE LoginId=").Append("'").Append(source).Append("' AND "); //source
                sql.Append("QtyNumber=").Append(int.Parse(uniqueId)); //uniqueid
                //sql.Append("DateRequested >= DATEADD(MINUTE, -1, GETDATE());");


                //WiseTools.logToFile(Config.logFile, "Starting saveResults", true);
                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(sql.ToString(), conn);
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 300;
                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            //WiseTools.logToFile(Config.logFile, "Completed saveResults", true);


        }


        internal void saveErrorMessage(string source, string uniqueId, string errorCode, string errorMessage)
        {
            SqlConnection conn = new SqlConnection(connString);
            string sql = "UPDATE " + Config.RemoteServerName + ".CostPlus.dbo.FreightRequests SET ErrorCode01 = @ErrorCode01, ErrorDesc01 = @ErrorDesc01, ";
            //string sql = "UPDATE UPSRATE.dbo.FreightRequests_" + Config.RemoteServerName + " SET ErrorCode01 = @ErrorCode01, ErrorDesc01 = @ErrorDesc01, ";
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
                SqlCommand cmd = new SqlCommand(Config.SQLProviderAbbriviations, conn);
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
                SqlCommand cmd = new SqlCommand(Config.SQLProviderAbbriviations, conn);
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
            SqlCommand cmdProviders = new SqlCommand(Config.SQLProviderAbbriviations, conn);
            cmdProviders.CommandType = CommandType.Text;

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmdProviders);
            da.Fill(ds);

            WiseTools.logToFile(Config.logFile, "Finished getFreightProviderInfo", true);

            return (ds);
        }
    }
}
