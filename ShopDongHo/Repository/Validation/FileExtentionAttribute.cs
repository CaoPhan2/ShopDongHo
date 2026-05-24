using System.ComponentModel.DataAnnotations;

namespace ShopDongHo.Repository.Validation
{
    public class FileExtentionAttribute: ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if(value is IFormFile file)
            {
                var extention = Path.GetExtension(file.FileName);
                string[] extentions = {"jpg", "png", "jpeg", "gif" };
                bool result = extentions.Any(e => extention.EndsWith(e));
                if (!result)
                {
                    return new ValidationResult("Chỉ Chấp Nhận Ảnh Có Đuôi .jpg, .png, .jpeg, .gif");
                }
            }
            return ValidationResult.Success;    
        }
    }
}
