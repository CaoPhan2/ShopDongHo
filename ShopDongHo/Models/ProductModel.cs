using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ShopDongHo.Repository.Validation;
using ShopDongHo.Repository.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopDongHo.Models
{
    public class ProductModel
    {
        [Key]
        public long Id { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Tên Sản phẩm")]
        public string Name { get; set; }
        public string Slug { get; set; }

        [Required, MinLength(4, ErrorMessage = "Yêu Cầu Nhập Mô Tả Sản phẩm")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Yêu Cầu Nhập Giá Sản phẩm")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Yêu Cầu Nhập Giá Vốn Sản phẩm")]
        
        public decimal CapitalPrice { get; set; }

        public int Quantity { get; set; }

        public int Sold { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage ="Chọn một thương hiệu")]
        public int BrandId { get; set; }
        [Required, Range(1, int.MaxValue, ErrorMessage = "Chọn một danh mục")]
        public int CategoryId { get; set; }

        public CategoryModel Category { get; set; }

        public BrandModel Brand { get; set; }

        public string Images { get; set; }
        public List<RatingModel> Ratings { get; set; }

        [NotMapped]
        [FileExtention]
        public IFormFile ImageUpload { get; set; }
    }
}
