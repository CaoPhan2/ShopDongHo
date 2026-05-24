using System.ComponentModel.DataAnnotations;

namespace ShopDongHo.Models
{
    public class CouponModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Yêu cầu nhập tên Coupon")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Yêu cầu nhập mô tả")]
        public string Description { get; set; }

        // mã coupon user nhập
        [Required(ErrorMessage = "Yêu cầu nhập mã Coupon")]
        public string Code { get; set; }

        // percent hoặc money
        public string DiscountType { get; set; }

        // giảm %
        public int DiscountPercent { get; set; }

        // giảm tiền
        public decimal DiscountAmount { get; set; }

        public DateTime DateStart { get; set; }

        public DateTime DateExpired { get; set; }

        [Required(ErrorMessage = "Yêu cầu nhập số lượng coupon")]
        public int Quantity { get; set; }

        public int Status { get; set; }
    }
}
