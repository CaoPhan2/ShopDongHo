namespace ShopDongHo.Models
{
    public class ChatSessionContext
    {
        public int Id { get; set; }

        public string SessionId { get; set; }

        public string LastBrand { get; set; }

        public long? LastProductId { get; set; }

        public string LastCategory { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}