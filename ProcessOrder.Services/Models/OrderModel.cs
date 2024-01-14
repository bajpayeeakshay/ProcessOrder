using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessOrder.Services.Models;

public class OrderModel
{
    public string OrderNumber { get; set; }
    public string OrderDate { get; set; }
    public string BuyerEAN { get; set; }
    public string SupplierEAN { get; set; }
    public string Comment { get; set; }
    public List<OrderItemModel> OrderItems { get; set; } = new List<OrderItemModel>();
}
