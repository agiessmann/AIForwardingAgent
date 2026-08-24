using System.ComponentModel.DataAnnotations;

namespace AiForwardingAgent.Data.Models;

public class FreightPackage
{
    public int Id { get; set; }

    public int FreightRequestId { get; set; }
    public FreightRequest FreightRequest { get; set; } = null!;

    public int? Quantity { get; set; }

    [MaxLength(100)]
    public string? PackageType { get; set; }

    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }

    public decimal? WeightPerPackageKg { get; set; }
    public decimal? TotalWeightKg { get; set; }

    [MaxLength(100)]
    public string? GoodsDescription { get; set; }

    public bool? Stackable { get; set; }
}