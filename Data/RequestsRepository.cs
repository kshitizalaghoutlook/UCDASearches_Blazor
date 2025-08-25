using System.Data;
using Microsoft.Data.SqlClient;

namespace UCDASearches_Blazor.Data
{
    public sealed class RequestRow
    {
        public int RequestID { get; set; }
        public int UID { get; set; }
        public string VIN { get; set; } = "";
        public DateTime Time_Stamp { get; set; }
        public string Account { get; set; } = "";
        public string? Operator { get; set; }

        public short? AutoCheck { get; set; }
        public short? Lien { get; set; }
        public short? History { get; set; }
        public short? OOPS { get; set; }
        public DateTime? ExCaDate { get; set; }
        public short? EXCA { get; set; }
        public short? IRE { get; set; }
        public short? Carfax { get; set; }
        public short? CPIC { get; set; }
        public DateTime? CPICTime { get; set; }
        public short? CAMVAP { get; set; }
        public byte? LNONpath { get; set; }   // tinyint
        public DateTime? LNONcompleted { get; set; }
    }

    public sealed class RequestsRepository
    {
        private readonly string _connStr;
        public RequestsRepository(IConfiguration config)
        {
            _connStr = config.GetConnectionString("AppDb")
                ?? throw new InvalidOperationException("Missing connection string 'AppDb'.");
        }

        /// <summary>
        /// Load the most recent rows for an account with optional VIN and date filters.
        /// </summary>
        public async Task<List<RequestRow>> GetPreviousAsync(
            string account,
            string? vin = null,
            DateTime? from = null,
            DateTime? to = null,
            int take = 100,
            CancellationToken ct = default)
        {
            const string sql = @"
SELECT TOP (@Take)
    RequestID, UID, VIN, Time_Stamp, Account, [Operator],
    AutoCheck, Lien, History, OOPS, ExCaDate, EXCA, IRE,
    Carfax, CPIC, CPICTime, CAMVAP, LNONpath, LNONcompleted
FROM dbo.Requests WITH (NOLOCK)
WHERE RTRIM(Account)=RTRIM(@Account)
  AND (@Vin IS NULL OR VIN LIKE '%' + @Vin + '%')
  AND (@From IS NULL OR Time_Stamp >= @From)
  AND (@To   IS NULL OR Time_Stamp < DATEADD(DAY, 1, @To))
ORDER BY Time_Stamp DESC;";

            using var conn = new SqlConnection(_connStr);
            using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 10 };

            cmd.Parameters.Add(new SqlParameter("@Account", SqlDbType.Char, 11) { Value = account });
            cmd.Parameters.AddWithValue("@Vin", (object?)vin ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@From", (object?)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@To", (object?)to ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Take", take);

            await conn.OpenAsync(ct);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            var list = new List<RequestRow>();
            while (await reader.ReadAsync(ct))
            {
                var r = new RequestRow
                {
                    RequestID = reader.GetInt32(0),
                    UID = reader.GetInt32(1),
                    VIN = reader.GetString(2),
                    Time_Stamp = reader.GetDateTime(3),
                    Account = reader.GetString(4),
                    Operator = reader.IsDBNull(5) ? null : reader.GetString(5),
                    AutoCheck = reader.IsDBNull(6) ? null : reader.GetInt16(6),
                    Lien = reader.IsDBNull(7) ? null : reader.GetInt16(7),
                    History = reader.IsDBNull(8) ? null : reader.GetInt16(8),
                    OOPS = reader.IsDBNull(9) ? null : reader.GetInt16(9),
                    ExCaDate = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                    EXCA = reader.IsDBNull(11) ? null : reader.GetInt16(11),
                    IRE = reader.IsDBNull(12) ? null : reader.GetInt16(12),
                    Carfax = reader.IsDBNull(13) ? null : reader.GetInt16(13),
                    CPIC = reader.IsDBNull(14) ? null : reader.GetInt16(14),
                    CPICTime = reader.IsDBNull(15) ? null : reader.GetDateTime(15),
                    CAMVAP = reader.IsDBNull(16) ? null : reader.GetInt16(16),
                    LNONpath = reader.IsDBNull(17) ? null : reader.GetByte(17),
                    LNONcompleted = reader.IsDBNull(18) ? null : reader.GetDateTime(18),
                };
                list.Add(r);
            }
            return list;
        }

        /// <summary>Just the single most recent row for an account.</summary>
        public async Task<RequestRow?> GetLatestAsync(string account, CancellationToken ct = default)
        {
            var list = await GetPreviousAsync(account, null, null, null, take: 1, ct);
            return list.FirstOrDefault();
        }
    }
}
