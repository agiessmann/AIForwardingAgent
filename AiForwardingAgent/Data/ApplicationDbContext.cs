using AiForwardingAgent.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace AiForwardingAgent.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<EmailSenderInformation> EmailSenderInformation { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<FreightRequest> FreightRequests => Set<FreightRequest>();
    public DbSet<FreightPackage> FreightPackages => Set<FreightPackage>();
    public DbSet<FreightLocation> FreightLocations => Set<FreightLocation>();
    public DbSet<TradeTerm> TradeTermInformations => Set<TradeTerm>();

    public DbSet<FreightRequestCheck> FreightRequestChecks { get; set; }
    

    public DbSet<Shipment> Shipments { get; set; }

    public DbSet<Package> Packages { get; set; }
    
    public DbSet<Email> Emails { get; set; }
    
    public DbSet<Location> Locations { get; set; }
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FreightRequest>(entity =>
        {
            entity.HasKey(x => x.Id);

             entity.Property(x => x.Status)
                .HasDefaultValue(FreightRequestStatus.New);

            });

        modelBuilder.Entity<TradeTerm>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Incoterm)
                .HasMaxLength(20);

            entity.Property(x => x.NamedPlace)
                .HasMaxLength(200);
        });

        modelBuilder.Entity<FreightPackage>(entity =>
        {
            entity.Property(x => x.LengthCm)
                .HasPrecision(18, 2);

            entity.Property(x => x.WidthCm)
                .HasPrecision(18, 2);

            entity.Property(x => x.HeightCm)
                .HasPrecision(18, 2);

            entity.Property(x => x.WeightPerPackageKg)
                .HasPrecision(18, 3);

            entity.Property(x => x.TotalWeightKg)
                .HasPrecision(18, 3);
        });

        modelBuilder.Entity<FreightLocation>(entity =>
        {
            entity.Property(x => x.ResolvedPostalCode)
                .HasMaxLength(100);

            entity.Property(x => x.ResolvedCity)
                .HasMaxLength(100);

            entity.Property(x => x.CountryCode)
                .HasMaxLength(100);

            entity.Property(x => x.CountryName)
                .HasMaxLength(100);

            entity.Property(x => x.PostalCodeMatchQuality);

            entity.Property(x => x.PostalCodeSimilarity);

            entity.Property(x => x.Latitude);

            entity.Property(x => x.Longitude);
        });
    }
}


