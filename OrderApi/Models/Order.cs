namespace OrderApi.Models
{
    public class Order
    {
        public int OrderID { get; set; }
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
}
