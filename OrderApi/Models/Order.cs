using System.Collections.Generic;

namespace OrderApi.Models
{
    public class Order
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public string OrderStatus { get; set; }
    }

    public class OrderItem
    {
        public int OrderItemID { get; set; }
        public int OrderID { get; set; }
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public double ProductPrice { get; set; }
        public double Total { get; set; }
    }

    public class CartOrder
    {
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public List<Product> Products { get; set; }
    }

    public class Product
    {
        public int ProductID { get; set; }
        public double ProductPrice { get; set; }
        public int Quantity { get; set; }
        public double Total { get; set; }
    }
}
