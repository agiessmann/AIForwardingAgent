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
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FreightRequest>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Incoterm)
                .HasMaxLength(20);

            entity.Property(x => x.CustomerReference)
                .HasMaxLength(200);

            entity.Property(x => x.GrossWeightKg)
                .HasPrecision(18, 3);

            entity.Property(x => x.VolumeM3)
                .HasPrecision(18, 4);

            entity.Property(x => x.LoadingMeters)
                .HasPrecision(18, 3);

            entity.Property(x => x.MinimumTemperatureCelsius)
                .HasPrecision(8, 2);

            entity.Property(x => x.MaximumTemperatureCelsius)
                .HasPrecision(8, 2);

            entity.HasMany(x => x.Packages)
                .WithOne(x => x.FreightRequest)
                .HasForeignKey(x => x.FreightRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.PickupLocation)
                .WithMany()
                .HasForeignKey(x => x.PickupLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.DeliveryLocation)
                .WithMany()
                .HasForeignKey(x => x.DeliveryLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
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
    }
}


