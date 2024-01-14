namespace ProcessOrder.Services.Models;

public class OrderItemSettings
{
    public FieldSettings EAN { get; set; }
    public FieldSettings Description { get; set; }
    public FieldSettings Quantity { get; set; }
    public FieldSettings UnitPrice { get; set; }
}
