namespace AiForwardingAgent.AgentServices;

public class PackageInformation
{
    /// <summary>
    /// Anzahl identischer Packstücke dieser Gruppe.
    /// Beispiel: 3 Europaletten.
    /// </summary>
    public int? Quantity { get; init; }
    /// <summary>
    /// Verpackungsart, z. B. Pallet, Euro Pallet, Carton, Crate.
    /// </summary>
    public string? PackageType { get; init; }
    /// <summary>
    /// Länge eines Packstücks in cm.
    /// </summary>
    public decimal? LengthCm { get; init; }
    /// <summary>
    /// Breite eines Packstücks in cm.
    /// </summary>
    public decimal? WidthCm { get; init; }
    /// <summary>
    /// Höhe eines Packstücks in cm.
    /// </summary>
    public decimal? HeightCm { get; init; }
    /// <summary>
    /// Gewicht eines einzelnen Packstücks in kg.
    /// </summary>
    public decimal? WeightPerPackageKg { get; init; }
    /// <summary>
    /// Gesamtgewicht dieser Packstückgruppe, falls explizit angegeben.
    /// </summary>
    public decimal? TotalWeightKg { get; init; }
    /// <summary>
    /// Freitextbeschreibung der Ware innerhalb dieser Packstückgruppe.
    /// </summary>
    public string? GoodsDescription { get; init; }
    /// <summary>
    /// Falls explizit angegeben: stapelbar oder nicht.
    /// </summary>
    public bool? Stackable { get; init; }
}