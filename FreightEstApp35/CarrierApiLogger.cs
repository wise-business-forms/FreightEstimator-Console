using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

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
     RequestedAtUtc, RespondedAtUtc, DurationMs)
VALUES
    (@CorrelationId, @Application, @ApiProvider, @ShippingMode, @Operation, @PlantCode, @AccountNumber,
     @ShipToAddress, @ShipToCity, @ShipToState, @ShipToZip, @ShipToCountry,
     @RequestUrl, @HttpStatusCode, @IsSuccess, @ErrorMessage, @RequestBody, @ResponseBody,
     @RequestedAtUtc, @RespondedAtUtc, @DurationMs);
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

            return (long)cmd.ExecuteScalar();
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
