using System.Collections.Generic;

namespace ShelfApi.Models;

public class KitDetails
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public List<KitItemDetail> Items { get; set; } = new();
}