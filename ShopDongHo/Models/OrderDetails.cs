using System.ComponentModel.DataAnnotations.Schema;

namespace ShopDongHo.Models
{
    public class OrderDetails
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string OrderCode { get; set; }
        public long productId { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        [ForeignKey("productId")]
        public ProductModel Product { get; set; }

    }
}
