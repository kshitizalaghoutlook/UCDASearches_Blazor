using Microsoft.EntityFrameworkCore;

namespace UCDASearches_Blazor.Data
{
    public class PreviousSearchService
    {
        private readonly AppDbContext _db;
        public PreviousSearchService(AppDbContext db) => _db = db;
        public async Task<Request?> GetFirstAsync(string accountNumber, CancellationToken ct = default)
        {
            return await _db.Requests.AsNoTracking()
                .Where(r => r.Account.Trim() == accountNumber.Trim())
                .OrderByDescending(r => r.Time_Stamp)
                .FirstOrDefaultAsync(ct);
        }
        public async Task<(IReadOnlyList<Request> Items, int TotalCount)> GetAsync(
            string accountNumber,
            string? vin,
            DateTime? fromUtc,
            DateTime? toUtc,
            int page, int pageSize,
            string? sortBy, bool sortDesc,
            CancellationToken ct = default)
        {
            var q = _db.Requests.AsNoTracking()
                                .Where(r => r.Account == accountNumber);

            if (!string.IsNullOrWhiteSpace(vin))
                q = q.Where(r => r.VIN.Contains(vin));

            if (fromUtc.HasValue) q = q.Where(r => r.Time_Stamp >= fromUtc.Value);
            if (toUtc.HasValue) q = q.Where(r => r.Time_Stamp <= toUtc.Value);

            q = (sortBy, sortDesc) switch
            {
                ("VIN", true) => q.OrderByDescending(r => r.VIN),
                ("VIN", false) => q.OrderBy(r => r.VIN),
                ("Time_Stamp", false) => q.OrderBy(r => r.Time_Stamp),
                ("Time_Stamp", true) => q.OrderByDescending(r => r.Time_Stamp),
                _ => q.OrderByDescending(r => r.Time_Stamp)
            };

            var total = await q.CountAsync(ct);
            var items = await q.Skip(page * pageSize).Take(pageSize).ToListAsync(ct);
            return (items, total);
        }
    }
}
