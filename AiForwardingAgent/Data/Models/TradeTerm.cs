using System.ComponentModel.DataAnnotations;

namespace AiForwardingAgent.Data.Models;

public class TradeTerm
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string? Incoterm { get; set; }

    [MaxLength(200)]
    public string? NamedPlace { get; set; }
}