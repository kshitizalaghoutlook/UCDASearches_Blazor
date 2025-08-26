using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

public interface IRequestsRepository
{
    Task<bool> ValidateLoginAsync(string accountId, string password);
    Task<IReadOnlyList<RequestDto>> GetByAccountAsync(string accountId);
    Task<IReadOnlyList<RequestDto>> SearchRequestsAsync(string accountId, string? vin, DateTime? from, DateTime? to);
}

public class RequestsRepository : IRequestsRepository
{
    private readonly string _cs;
    public RequestsRepository(IConfiguration cfg) => _cs = cfg.GetConnectionString("DefaultConnection")!;

    public async Task<bool> ValidateLoginAsync(string accountId, string password)
    {
        const string sql = @"SELECT COUNT(1) 
                             FROM dbo.Dealers 
                             WHERE AccountId = @acc AND Password = @pwd";
        await using var con = new SqlConnection(_cs);
        await con.OpenAsync();
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@acc", accountId);
        cmd.Parameters.AddWithValue("@pwd", password);
        var count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }
    public async Task<IReadOnlyList<RequestDto>> GetByAccountAsync(string accountId)
    {
        const string sql = @"SELECT RequestID, UID, VIN, Time_Stamp, Account, Operator,
                                AutoCheck, Lien, History, OOPS, ExCaDate, EXCA, IRE, Carfax,
                                CPIC, CPICTime, CAMVAP, LNONpath, LNONcompleted
                         FROM dbo.Requests
                         WHERE Account = @acc
                         ORDER BY Time_Stamp DESC";

        var list = new List<RequestDto>();
        await using var con = new SqlConnection(_cs);
        await con.OpenAsync();
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@acc", accountId);

        await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        while (await rdr.ReadAsync())
        {
            var dto = new RequestDto
            {
                RequestID = rdr.GetInt32(0),
                VIN = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                TimeStamp = rdr.GetDateTime(2),
                Account = rdr.GetString(3),

                Lien = rdr.IsDBNull(4) ? null : rdr.GetInt16(4),
                AutoCheck = rdr.IsDBNull(5) ? null : rdr.GetInt16(5),
                History = rdr.IsDBNull(6) ? null : rdr.GetInt16(6),
                EXCA = rdr.IsDBNull(7) ? null : rdr.GetInt16(7),
                Carfax = rdr.IsDBNull(8) ? null : rdr.GetInt16(8),
            };
            list.Add(dto);
        }
            return list;
    }

    public async Task<IReadOnlyList<RequestDto>> SearchRequestsAsync(
    string accountId, string? vin, DateTime? from, DateTime? to)
    {
        var sql = @"SELECT RequestID, VIN, Time_Stamp, Account,
                       Lien, AutoCheck, History, EXCA, Carfax
                FROM dbo.Requests
                WHERE Account = @acc";

        if (!string.IsNullOrWhiteSpace(vin))
            sql += " AND VIN = @vin";

        if (from.HasValue)
            sql += " AND Time_Stamp >= @from";

        if (to.HasValue)
            sql += " AND Time_Stamp <= @to";

        sql += " ORDER BY Time_Stamp DESC";

        var list = new List<RequestDto>();
        await using var con = new SqlConnection(_cs);
        await con.OpenAsync();
        await using var cmd = new SqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@acc", accountId);
        if (!string.IsNullOrWhiteSpace(vin)) cmd.Parameters.AddWithValue("@vin", vin);
        if (from.HasValue) cmd.Parameters.AddWithValue("@from", from);
        if (to.HasValue) cmd.Parameters.AddWithValue("@to", to);

        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            list.Add(new RequestDto
            {
                RequestID = rdr.GetInt32(0),
                VIN = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                TimeStamp = rdr.GetDateTime(2),
                Account = rdr.GetString(3),
                Lien = rdr.IsDBNull(4) ? null : rdr.GetInt16(4),
                AutoCheck = rdr.IsDBNull(5) ? null : rdr.GetInt16(5),
                History = rdr.IsDBNull(6) ? null : rdr.GetInt16(6),
                EXCA = rdr.IsDBNull(7) ? null : rdr.GetInt16(7),
                Carfax = rdr.IsDBNull(8) ? null : rdr.GetInt16(8),
            });
        }
        return list;
    }


}
public class RequestDto
{
    public int RequestID { get; set; }
    public string VIN { get; set; } = "";
    public DateTime TimeStamp { get; set; }
    public string Account { get; set; } = "";
    public short? Lien { get; set; }
    public short? AutoCheck { get; set; }
    public short? History { get; set; }
    public short? EXCA { get; set; }
    public short? Carfax { get; set; }
}


