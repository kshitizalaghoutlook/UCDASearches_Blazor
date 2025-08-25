
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace UCDASearches_Blazor.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Request> Requests => Set<Request>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Request>(e =>
            {
                e.ToTable("Requests", "dbo");   // table = dbo.Requests
                e.HasKey(x => x.RequestID);
                e.Property(x => x.UID);
                e.Property(x => x.VIN).HasMaxLength(17).IsRequired();
                e.Property(x => x.Time_Stamp);
                e.Property(x => x.Account).HasMaxLength(11).IsRequired();
                e.Property(x => x.Operator).HasMaxLength(20);
                // smallint columns map to short in C#
                e.Property(x => x.AutoCheck);
                e.Property(x => x.Lien);
                e.Property(x => x.History);
                e.Property(x => x.OOPS);
                e.Property(x => x.ExCaDate);
                e.Property(x => x.EXCA);
                e.Property(x => x.IRE);
                e.Property(x => x.Carfax);
                e.Property(x => x.CPIC);
                e.Property(x => x.CPICTime);
                e.Property(x => x.CAMVAP);
                e.Property(x => x.LNONpath);
                e.Property(x => x.LNONcompleted);
            });
        }
    }

    public class Request
    {
        public int RequestID { get; set; }
        public int UID { get; set; }
        public string VIN { get; set; } = default!;
        public DateTime Time_Stamp { get; set; }
        public string Account { get; set; } = default!;
        public string? Operator { get; set; }

        // smallint -> short (0/1/2… depending on your app)
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
        public bool? LNONpath { get; set; }   // tinyint → bool? if 0/1
        public DateTime? LNONcompleted { get; set; }
    }

}
