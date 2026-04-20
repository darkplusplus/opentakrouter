using dpp.opentakrouter.Models;
using Microsoft.EntityFrameworkCore;

namespace dpp.opentakrouter
{
    public class OpenTakRouterDbContext : DbContext
    {
        public OpenTakRouterDbContext(DbContextOptions<OpenTakRouterDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients => Set<Client>();
        public DbSet<StoredMessage> Messages => Set<StoredMessage>();
        public DbSet<DataPackage> DataPackages => Set<DataPackage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("clients");
                entity.HasKey(x => x.PrimaryKey);
                entity.HasIndex(x => x.Callsign).IsUnique();
                entity.Property(x => x.Callsign).IsRequired();
                entity.Property(x => x.LastStatus).HasDefaultValue("Connected");
            });

            modelBuilder.Entity<StoredMessage>(entity =>
            {
                entity.ToTable("messages");
                entity.HasKey(x => x.PrimaryKey);
                entity.HasIndex(x => x.Uid).IsUnique();
                entity.HasIndex(x => x.Expiration);
                entity.Property(x => x.Uid).IsRequired();
                entity.Property(x => x.Data).IsRequired();
                entity.Ignore(x => x.IsExpired);
            });

            modelBuilder.Entity<DataPackage>(entity =>
            {
                entity.ToTable("datapackages");
                entity.HasKey(x => x.PrimaryKey);
                entity.HasIndex(x => x.Hash).IsUnique();
                entity.Property(x => x.UID).IsRequired();
                entity.Property(x => x.Hash).IsRequired();
                entity.Property(x => x.Content).IsRequired();
            });
        }
    }
}
