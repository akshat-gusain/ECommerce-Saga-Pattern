namespace InventoryService.Models
{
    public class Inventory
    {
        public int ProductId { get; set; }
        public int OrderId { get; set; } 
        public int Stock { get; set; }
        public decimal Price { get; set; }
    }
}
