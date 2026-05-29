namespace ShopDongHo.Models
{
    public class ChatHistory
    {
        public int Id { get; set; }

        public string SessionId { get; set; }

        public string UserMessage { get; set; }

        public string BotReply { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}