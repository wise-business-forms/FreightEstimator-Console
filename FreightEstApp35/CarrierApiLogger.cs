using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FreightEstApp35
{
    /// <summary>
    /// Insert-only: every carrier API call gets its own row, and rows are never updated in
    /// place. A prior upsert-by-CorrelationId design silently overwrote sibling calls sharing
    /// a CorrelationId - see docs/adr/0001-carrier-api-call-quote-schema.md.
    /// Logging must never break the primary rate-fetching flow (e.g. if the target tables
    /// haven't been created yet, or the DB is briefly unreachable), so failures here are
    /// caught and reported to the console rather than propagated.
    /// </summary>
    public static class CarrierApiLogger
    {
        public static long LogApiCall(CarrierApiCall call)
        {
            try
            {
                return Insert(call);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: failed to write CarrierApiCall entry - " + ex.Message);
                return 0;
            }
        }

        public static void LogQuotes(long carrierApiCallId, IEnumerable<CarrierRateQuote> quotes)
        {
            if (carrierApiCallId <= 0 || quotes == null)
            {
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(Config.ConnString))
                {
                    conn.Open();
                    using (SqlTransaction tx = conn.BeginTransaction())
                    {
                        InsertQuotes(conn, tx, carrierApiCallId, quotes);
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Warning: failed to write CarrierRateQuote entries - " + ex.Message);
            }
        }

        private static long Insert(CarrierApiCall call)
        {
            using (SqlConnection conn = new SqlConnection(Config.ConnString))
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    long callId = InsertCall(conn, tx, call);

                    if (call.Quotes != null && call.Quotes.Count > 0)
                    {
                        InsertQuotes(conn, tx, callId, call.Quotes);
                    }

                    tx.Commit();
                    return callId;
                }
            }
        }

        private static long InsertCall(SqlConnection conn, SqlTransaction tx, CarrierApiCall call)
        {
            SqlCommand cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
INSERT INTO dbo.CarrierApiCall
    (CorrelationId, Application, ApiProvider, ShippingMode, Operation, PlantCode, AccountNumber,
     ShipToAddress, ShipToCity, ShipToState, ShipToZip, ShipToCountry,
     RequestUrl, HttpStatusCode, IsSuccess, ErrorMessage, RequestBody, ResponseBody,
     RequestedAtUtc, RespondedAtUtc, DurationMs, IsTest)
VALUES
    (@CorrelationId, @Application, @ApiProvider, @ShippingMode, @Operation, @PlantCode, @AccountNumber,
     @ShipToAddress, @ShipToCity, @ShipToState, @ShipToZip, @ShipToCountry,
     @RequestUrl, @HttpStatusCode, @IsSuccess, @ErrorMessage, @RequestBody, @ResponseBody,
     @RequestedAtUtc, @RespondedAtUtc, @DurationMs, @IsTest);
SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            cmd.Parameters.Add("@CorrelationId", SqlDbType.VarChar, 100).Value = (object)call.CorrelationId ?? DBNull.Value;
            cmd.Parameters.Add("@Application", SqlDbType.VarChar, 50).Value = call.Application;
            cmd.Parameters.Add("@ApiProvider", SqlDbType.VarChar, 30).Value = call.ApiProvider;
            cmd.Parameters.Add("@ShippingMode", SqlDbType.VarChar, 10).Value = call.ShippingMode;
            cmd.Parameters.Add("@Operation", SqlDbType.VarChar, 30).Value = (object)call.Operation ?? DBNull.Value;
            cmd.Parameters.Add("@PlantCode", SqlDbType.VarChar, 10).Value = (object)call.PlantCode ?? DBNull.Value;
            cmd.Parameters.Add("@AccountNumber", SqlDbType.VarChar, 20).Value = (object)call.AccountNumber ?? DBNull.Value;
            cmd.Parameters.Add("@ShipToAddress", SqlDbType.VarChar, 200).Value = (object)call.ShipToAddress ?? DBNull.Value;
            cmd.Parameters.Add("@ShipToCity", SqlDbType.VarChar, 200).Value = (object)call.ShipToCity ?? DBNull.Value;
            cmd.Parameters.Add("@ShipToState", SqlDbType.VarChar, 50).Value = (object)call.ShipToState ?? DBNull.Value;
            cmd.Parameters.Add("@ShipToZip", SqlDbType.VarChar, 50).Value = (object)call.ShipToZip ?? DBNull.Value;
            cmd.Parameters.Add("@ShipToCountry", SqlDbType.VarChar, 10).Value = (object)call.ShipToCountry ?? DBNull.Value;
            cmd.Parameters.Add("@RequestUrl", SqlDbType.VarChar, 500).Value = (object)call.RequestUrl ?? DBNull.Value;
            cmd.Parameters.Add("@HttpStatusCode", SqlDbType.Int).Value = (object)call.HttpStatusCode ?? DBNull.Value;
            cmd.Parameters.Add("@IsSuccess", SqlDbType.Bit).Value = call.IsSuccess;
            cmd.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, 4000).Value = (object)call.ErrorMessage ?? DBNull.Value;
            cmd.Parameters.Add("@RequestBody", SqlDbType.NVarChar, -1).Value = (object)call.RequestBody ?? DBNull.Value;
            cmd.Parameters.Add("@ResponseBody", SqlDbType.NVarChar, -1).Value = (object)call.ResponseBody ?? DBNull.Value;
            cmd.Parameters.Add("@RequestedAtUtc", SqlDbType.DateTime2).Value = call.RequestedAtUtc;
            cmd.Parameters.Add("@RespondedAtUtc", SqlDbType.DateTime2).Value = (object)call.RespondedAtUtc ?? DBNull.Value;
            cmd.Parameters.Add("@DurationMs", SqlDbType.Int).Value = (object)call.DurationMs ?? DBNull.Value;
            cmd.Parameters.Add("@IsTest", SqlDbType.Bit).Value = call.IsTest;

            return (long)cmd.ExecuteScalar();
        }

        /// <summary>
        /// Reads back the persisted rows for a CorrelationId - used by the Carrier Test Harness
        /// to prove a Test Call's rows actually made it into the database, since LogApiCall/
        /// LogQuotes deliberately swallow write failures rather than propagate them.
        /// </summary>
        public static List<CarrierApiCall> GetCallsByCorrelationId(string correlationId)
        {
            var calls = new List<CarrierApiCall>();

            using (SqlConnection conn = new SqlConnection(Config.ConnString))
            {
                conn.Open();

                SqlCommand callCmd = conn.CreateCommand();
                callCmd.CommandType = CommandType.Text;
                callCmd.CommandText = @"
SELECT Id, CorrelationId, Application, ApiProvider, ShippingMode, Operation, PlantCode, AccountNumber,
       ShipToAddress, ShipToCity, ShipToState, ShipToZip, ShipToCountry,
       RequestUrl, HttpStatusCode, IsSuccess, ErrorMessage, RequestBody, ResponseBody,
       RequestedAtUtc, RespondedAtUtc, DurationMs, IsTest
FROM dbo.CarrierApiCall
WHERE CorrelationId = @CorrelationId
ORDER BY Id;";
                callCmd.Parameters.Add("@CorrelationId", SqlDbType.VarChar, 100).Value = correlationId;

                using (SqlDataReader reader = callCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        calls.Add(new CarrierApiCall
                        {
                            Id = (long)reader["Id"],
                            CorrelationId = reader["CorrelationId"] as string,
                            Application = reader["Application"] as string,
                            ApiProvider = reader["ApiProvider"] as string,
                            ShippingMode = reader["ShippingMode"] as string,
                            Operation = reader["Operation"] as string,
                            PlantCode = reader["PlantCode"] as string,
                            AccountNumber = reader["AccountNumber"] as string,
                            ShipToAddress = reader["ShipToAddress"] as string,
                            ShipToCity = reader["ShipToCity"] as string,
                            ShipToState = reader["ShipToState"] as string,
                            ShipToZip = reader["ShipToZip"] as string,
                            ShipToCountry = reader["ShipToCountry"] as string,
                            RequestUrl = reader["RequestUrl"] as string,
                            HttpStatusCode = reader["HttpStatusCode"] as int?,
                            IsSuccess = (bool)reader["IsSuccess"],
                            ErrorMessage = reader["ErrorMessage"] as string,
                            RequestBody = reader["RequestBody"] as string,
                            ResponseBody = reader["ResponseBody"] as string,
                            RequestedAtUtc = (DateTime)reader["RequestedAtUtc"],
                            RespondedAtUtc = reader["RespondedAtUtc"] as DateTime?,
                            DurationMs = reader["DurationMs"] as int?,
                            IsTest = (bool)reader["IsTest"]
                        });
                    }
                }

                SqlCommand quoteCmd = conn.CreateCommand();
                quoteCmd.CommandType = CommandType.Text;
                quoteCmd.CommandText = @"
SELECT Id, CarrierApiCallId, CarrierName, ServiceLevel, BaseCost, MarkupPercent,
       PerPackageCharge, PerShipmentCharge, TotalCharge, Currency, TransitDays
FROM dbo.CarrierRateQuote
WHERE CarrierApiCallId = @CarrierApiCallId
ORDER BY Id;";
                var callIdParam = quoteCmd.Parameters.Add("@CarrierApiCallId", SqlDbType.BigInt);

                foreach (CarrierApiCall call in calls)
                {
                    callIdParam.Value = call.Id;

                    using (SqlDataReader reader = quoteCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            call.Quotes.Add(new CarrierRateQuote
                            {
                                Id = (long)reader["Id"],
                                CarrierApiCallId = (long)reader["CarrierApiCallId"],
                                CarrierName = reader["CarrierName"] as string,
                                ServiceLevel = reader["ServiceLevel"] as string,
                                BaseCost = reader["BaseCost"] as decimal?,
                                MarkupPercent = reader["MarkupPercent"] as decimal?,
                                PerPackageCharge = reader["PerPackageCharge"] as decimal?,
                                PerShipmentCharge = reader["PerShipmentCharge"] as decimal?,
                                TotalCharge = (decimal)reader["TotalCharge"],
                                Currency = reader["Currency"] as string,
                                TransitDays = reader["TransitDays"] as int?
                            });
                        }
                    }
                }
            }

            return calls;
        }

        private static void InsertQuotes(SqlConnection conn, SqlTransaction tx, long carrierApiCallId, IEnumerable<CarrierRateQuote> quotes)
        {
            foreach (CarrierRateQuote quote in quotes)
            {
                SqlCommand cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"
INSERT INTO dbo.CarrierRateQuote
    (CarrierApiCallId, CarrierName, ServiceLevel, BaseCost, MarkupPercent,
     PerPackageCharge, PerShipmentCharge, TotalCharge, Currency, TransitDays)
VALUES
    (@CarrierApiCallId, @CarrierName, @ServiceLevel, @BaseCost, @MarkupPercent,
     @PerPackageCharge, @PerShipmentCharge, @TotalCharge, @Currency, @TransitDays);";

                cmd.Parameters.Add("@CarrierApiCallId", SqlDbType.BigInt).Value = carrierApiCallId;
                cmd.Parameters.Add("@CarrierName", SqlDbType.VarChar, 100).Value = (object)quote.CarrierName ?? DBNull.Value;
                cmd.Parameters.Add("@ServiceLevel", SqlDbType.VarChar, 100).Value = (object)quote.ServiceLevel ?? DBNull.Value;
                cmd.Parameters.Add("@BaseCost", SqlDbType.Decimal).Value = (object)quote.BaseCost ?? DBNull.Value;
                cmd.Parameters.Add("@MarkupPercent", SqlDbType.Decimal).Value = (object)quote.MarkupPercent ?? DBNull.Value;
                cmd.Parameters.Add("@PerPackageCharge", SqlDbType.Decimal).Value = (object)quote.PerPackageCharge ?? DBNull.Value;
                cmd.Parameters.Add("@PerShipmentCharge", SqlDbType.Decimal).Value = (object)quote.PerShipmentCharge ?? DBNull.Value;
                cmd.Parameters.Add("@TotalCharge", SqlDbType.Decimal).Value = quote.TotalCharge;
                cmd.Parameters.Add("@Currency", SqlDbType.VarChar, 3).Value = string.IsNullOrEmpty(quote.Currency) ? "USD" : quote.Currency;
                cmd.Parameters.Add("@TransitDays", SqlDbType.Int).Value = (object)quote.TransitDays ?? DBNull.Value;

                cmd.ExecuteNonQuery();
            }
        }
    }
}
