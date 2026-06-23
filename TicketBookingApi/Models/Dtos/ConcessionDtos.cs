namespace TicketBookingApi.Models.Dtos;

public class CreateItemDto
{
    public string Name { get; set; } = null!;
    public string ItemType { get; set; } = null!;
    public int Price { get; set; }
    public string? ImageUrl { get; set; }
    public int? StockQuantity { get; set; }
    public string? Unit { get; set; }
}

public class UpdateItemDto
{
    public string? Name { get; set; }
    public string? ItemType { get; set; }
    public int? Price { get; set; }
    public string? ImageUrl { get; set; }
    public int? StockQuantity { get; set; }
    public bool? IsAvailable { get; set; }
    public string? Unit { get; set; }
}

public class ComboItemInputDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class CreateComboDto
{
    public string Name { get; set; } = null!;
    public int Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<ComboItemInputDto>? Items { get; set; }
}

public class UpdateComboDto
{
    public string? Name { get; set; }
    public int? Price { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsAvailable { get; set; }
    public List<ComboItemInputDto>? Items { get; set; }
}

public class PosConcessionInputDto
{
    public int ComboId { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
}

public class PosConcessionBookDto
{
    public List<PosConcessionInputDto> Concessions { get; set; } = new();
}
