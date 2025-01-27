using System.ComponentModel.DataAnnotations;

namespace OrderService.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Status { get; set; }
    }
}
