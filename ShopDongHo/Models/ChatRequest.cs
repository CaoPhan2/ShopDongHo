using System.Collections.Generic;

namespace ShopDongHo.Models
{
    public class ChatRequest
    {
        public string Message { get; set; }
        public string SessionId { get; set; }
    }

    public class ChatResponse
    {
        public string Reply { get; set; }
        public List<ProductItemDto> Products { get; set; } = new List<ProductItemDto>();
    }

    public class ProductItemDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string BrandName { get; set; } // Phải có dòng này để chứa tên thương hiệu truyền xuống Frontend
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
    }
}