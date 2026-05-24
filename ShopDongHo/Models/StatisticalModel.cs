namespace ShopDongHo.Models
{
    public class StatisticalModel
    {
        public int Id { get; set; }
        public int Quantity { get; set; } //so luong bán
        public int Sold { get; set; } //so luong dơn hàng
        public decimal Revenue { get; set; } //doanh thu
        public decimal Profit { get; set; }//lợi nhuận

        public DateTime DateCreate { get; set; }

    }
}
