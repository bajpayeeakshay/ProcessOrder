namespace ProcessOrder.Services.Models;

public class OrderSettings
{
    public FieldSettings FileTypeIdentifier { get; set; }
    public FieldSettings OrderNumber { get; set; }
    public FieldSettings OrderDate { get; set; }
    public FieldSettings BuyerEAN { get; set; }
    public FieldSettings SupplierEAN { get; set; }
    public FieldSettings Comment { get; set; }
}

