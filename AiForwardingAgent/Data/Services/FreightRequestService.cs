using AiForwardingAgent.Data.Models;
using AiForwardingAgent.AgentServices.Models;
using Microsoft.EntityFrameworkCore;

namespace AiForwardingAgent.Data.Services;

public interface IFreightRequestService
{
    Task<FreightRequest> CreateFreightRequestAsync(FreightRequest freightRequest);
    Task<FreightRequest?> GetFreightRequestByIdAsync(int id);
    Task<IEnumerable<FreightRequest>> GetAllFreightRequestsAsync();
    Task<FreightRequest> UpdateFreightRequestAsync(FreightRequest freightRequest);
    Task<bool> DeleteFreightRequestAsync(FreightRequest freightRequest);
    
    Task<FreightRequest?> SaveFreightRequestStatusAsync(int freightRequestId, FreightRequestStatus status);
    Task<FreightRequestDto?> GetFreightRequestDtoByIdAsync(int id);
    Task<IEnumerable<FreightRequestDto>> GetAllFreightRequestsAsDtoAsync();
    
    Task<FreightRequest> UpsertFreightRequestAsync(FreightRequest freightRequest);
    
}

public class FreightRequestService(
    IRepositoryService<FreightRequest> repositoryService,
    IDbContextFactory<ApplicationDbContext> dbContextFactory) : IFreightRequestService
{
    // CRUD operations for FreightRequest entity
    public async Task<FreightRequest> CreateFreightRequestAsync(FreightRequest freightRequest)
    {
        return await repositoryService.Create(freightRequest);
    }
    
    public async Task<FreightRequest?> GetFreightRequestByIdAsync(int id)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        return await BuildFreightRequestReadQuery(ctx)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
    
    public async Task<IEnumerable<FreightRequest>> GetAllFreightRequestsAsync()
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        return await BuildFreightRequestReadQuery(ctx).ToListAsync();
    }
    
    public async Task<FreightRequest> UpdateFreightRequestAsync(FreightRequest freightRequest)
    {
        return await repositoryService.Update(freightRequest);
    }
    
    public async Task<bool> DeleteFreightRequestAsync(FreightRequest freightRequest)
    {
        return  await repositoryService.Delete(freightRequest);
    }

    public async Task<FreightRequest?> SaveFreightRequestStatusAsync(int freightRequestId, FreightRequestStatus status)
    {
        await using var ctx = await dbContextFactory.CreateDbContextAsync();

        var freightRequest = await BuildFreightRequestUpdateQuery(ctx)
            .FirstOrDefaultAsync(x => x.Id == freightRequestId);

        if (freightRequest is null)
        {
            return null;
        }

        freightRequest.Status = status;
        await ctx.SaveChangesAsync();

        return freightRequest;
    }

    public async Task<FreightRequestDto?> GetFreightRequestDtoByIdAsync(int id)
    {
        var freightRequest = await GetFreightRequestByIdAsync(id);
        return freightRequest is null ? null : FreightRequestMapper.ToDto(freightRequest);
    }

    public async Task<IEnumerable<FreightRequestDto>> GetAllFreightRequestsAsDtoAsync()
    {
        var freightRequests = await GetAllFreightRequestsAsync();
        return FreightRequestMapper.ToDto(freightRequests);
    }

    private static IQueryable<FreightRequest> BuildFreightRequestBaseQuery(ApplicationDbContext ctx)
    {
        return ctx.FreightRequests
            .Include(x => x.FreightRequestEmail)
            .Include(x => x.Customer)
            .Include(x => x.Packages)
            .Include(x => x.Shipment)
            .ThenInclude(x => x.Pickup)
            .Include(x => x.Shipment)
            .ThenInclude(x => x.Delivery)
            .Include(x => x.Shipment)
            .ThenInclude(x => x.OriginTerm)
            .Include(x => x.Shipment)
            .ThenInclude(x => x.DestinationTerm);
    }

    private static IQueryable<FreightRequest> BuildFreightRequestReadQuery(ApplicationDbContext ctx)
        => BuildFreightRequestBaseQuery(ctx).AsNoTracking();

    private static IQueryable<FreightRequest> BuildFreightRequestUpdateQuery(ApplicationDbContext ctx)
        => BuildFreightRequestBaseQuery(ctx); // tracking
    public async Task<FreightRequest> UpsertFreightRequestAsync(FreightRequest freightRequest)
    {
        ArgumentNullException.ThrowIfNull(freightRequest);

        await using var ctx = await dbContextFactory.CreateDbContextAsync();

        var messageId = freightRequest.FreightRequestEmail?.MessageId;
        FreightRequest? existing = null;

        if (messageId is not null)
        {
            existing = await BuildFreightRequestUpdateQuery(ctx)
                .FirstOrDefaultAsync(x => x.FreightRequestEmail.MessageId == messageId);
        }

        if (existing is null && freightRequest.Id > 0)
        {
            existing = await BuildFreightRequestUpdateQuery(ctx)
                .FirstOrDefaultAsync(x => x.Id == freightRequest.Id);
        }

        if (existing is null)
        {
            if (freightRequest.Shipment is not null)
            {
                freightRequest.Shipment.RequestId = null;
            }

            await ctx.FreightRequests.AddAsync(freightRequest);
            await ctx.SaveChangesAsync();

            if (freightRequest.Shipment is not null)
            {
                freightRequest.Shipment.RequestId = freightRequest.Id;
                await ctx.SaveChangesAsync();
            }

            return freightRequest;
        }

        UpdateCoreFields(existing, freightRequest);
        existing.FreightRequestEmail = UpdateEmail(existing.FreightRequestEmail, freightRequest.FreightRequestEmail);
        existing.Customer =UpdateCustomer(existing.Customer, freightRequest.Customer);
        UpdateShipment(existing, freightRequest.Shipment);
        ReplacePackages(ctx, existing, freightRequest.Packages);

        await ctx.SaveChangesAsync();
        return existing;
    }

    private static void UpdateCoreFields(FreightRequest target, FreightRequest source)
    {
        target.Status = source.Status;
        target.IsFreightRequest = source.IsFreightRequest;
        target.QuotationRequested = source.QuotationRequested;
        target.Confidence = source.Confidence;
        target.Reason = source.Reason;
        target.CustomerReference = source.CustomerReference;
        target.MissingInformationJson = source.MissingInformationJson;
    }

    private static Email? UpdateEmail(Email? target, Email? source)
    {
        if (source is null)
        {
            return target;
        }

        target ??= new Email();

        target.Subject = source.Subject;
        target.Body = source.Body;
        target.MessageId = source.MessageId;
        target.EmailSender = source.EmailSender;
        target.EmailRecipient = source.EmailRecipient;
        
        return target;
    }

    private static Customer? UpdateCustomer(Customer? target, Customer? source)
    {
        if (source is null)
        {
            return target;
        }

        

        target.CustomerName = source.CustomerName;
        target.CustomerCode = source.CustomerCode;
        target.CustomerEmail = source.CustomerEmail;
        target.CustomerPhone = source.CustomerPhone;
        target.CustomerAddress = source.CustomerAddress;
        target.CustomerCity = source.CustomerCity;
        target.CustomerCountry = source.CustomerCountry;
        
        return target;
    }

    private static void UpdateShipment(FreightRequest targetRequest, Shipment? sourceShipment)
    {
        if (sourceShipment is null)
        {
            return;
        }

        if (targetRequest.Shipment is null)
        {
            targetRequest.Shipment = new Shipment();
        }

        var targetShipment = targetRequest.Shipment;

        targetShipment.RequestId = targetRequest.Id;
        targetShipment.TransportMode = sourceShipment.TransportMode;
        targetShipment.ShipmentType = sourceShipment.ShipmentType;
        targetShipment.PickupDate = sourceShipment.PickupDate;
        targetShipment.DeliveryDate = sourceShipment.DeliveryDate;
        targetShipment.Incoterm = sourceShipment.Incoterm;
        targetShipment.GoodsDescription = sourceShipment.GoodsDescription;
        targetShipment.GrossWeightKg = sourceShipment.GrossWeightKg;
        targetShipment.VolumeM3 = sourceShipment.VolumeM3;
        targetShipment.LoadingMeters = sourceShipment.LoadingMeters;
        targetShipment.ContainerType = sourceShipment.ContainerType;
        targetShipment.DangerousGoods = sourceShipment.DangerousGoods;
        targetShipment.DangerousGoodsDetails = sourceShipment.DangerousGoodsDetails;
        targetShipment.TemperatureControlled = sourceShipment.TemperatureControlled;
        targetShipment.MinimumTemperatureCelsius = sourceShipment.MinimumTemperatureCelsius;
        targetShipment.MaximumTemperatureCelsius = sourceShipment.MaximumTemperatureCelsius;
        targetShipment.Stackable = sourceShipment.Stackable;
        targetShipment.CustomsRequired = sourceShipment.CustomsRequired;
        targetShipment.HsCode = sourceShipment.HsCode;
        targetShipment.CustomerReference = sourceShipment.CustomerReference;
        targetShipment.SpecialInstructions = sourceShipment.SpecialInstructions;

        targetShipment.Pickup = UpdateLocation(targetShipment.Pickup, sourceShipment.Pickup);
        targetShipment.Delivery =UpdateLocation(targetShipment.Delivery, sourceShipment.Delivery);
        targetShipment.OriginTerm = UpdateTradeTerm(targetShipment.OriginTerm, sourceShipment.OriginTerm);
        targetShipment.DestinationTerm = UpdateTradeTerm(targetShipment.DestinationTerm, sourceShipment.DestinationTerm);
    }

    private static Location? UpdateLocation(Location? target, Location? source)
    {
        if (source is null)
        {
            return target;
        }
        

        target.Company = source.Company;
        target.Address = source.Address;
        target.PostalCode = source.PostalCode;
        target.City = source.City;
        target.State = source.State;
        target.Country = source.Country;
        target.ResolvedPostalCode = source.ResolvedPostalCode;
        target.ResolvedCity = source.ResolvedCity;
        target.CountryCode = source.CountryCode;
        target.CountryName = source.CountryName;
        target.Latitude = source.Latitude;
        target.Longitude = source.Longitude;
        target.PostalCodeMatchQuality = source.PostalCodeMatchQuality;
        target.PostalCodeSimilarity = source.PostalCodeSimilarity;
        
        return target;
    }

    private static TradeTerm? UpdateTradeTerm(TradeTerm? target, TradeTerm? source)
    {
        if (source is null)
        {
            return target;
        }
        

        target.Incoterm = source.Incoterm;
        target.NamedPlace = source.NamedPlace;
        
        return target;
    }

    private static void ReplacePackages(ApplicationDbContext ctx, FreightRequest targetRequest, List<FreightPackage>? sourcePackages)
    {
        if (sourcePackages is null)
        {
            return;
        }

        if (targetRequest.Packages.Count > 0)
        {
            ctx.RemoveRange(targetRequest.Packages);
        }

        targetRequest.Packages = sourcePackages.Select(p => new FreightPackage
        {
            Quantity = p.Quantity,
            PackageType = p.PackageType,
            LengthCm = p.LengthCm,
            WidthCm = p.WidthCm,
            HeightCm = p.HeightCm,
            WeightPerPackageKg = p.WeightPerPackageKg,
            TotalWeightKg = p.TotalWeightKg,
            GoodsDescription = p.GoodsDescription,
            Stackable = p.Stackable
        }).ToList();
    }
}