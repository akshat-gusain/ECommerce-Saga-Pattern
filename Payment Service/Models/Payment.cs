namespace PaymentService.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public decimal Price { get; set; } // Price of the product
        public int Quantity { get; set; } // Quantity ordered
        public decimal Amount { get; set; } // Total amount calculated
        public string Status { get; set; }
    }
}
