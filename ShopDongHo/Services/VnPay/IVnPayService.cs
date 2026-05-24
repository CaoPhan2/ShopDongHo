using ShopDongHo.Models.VNPay;

namespace ShopDongHo.Services.VnPay
{
    public interface IVnPayService
    {

        string CreatePaymentUrl(PaymentInformationModel model, HttpContext context);
        PaymentResponseModel PaymentExecute(IQueryCollection collections);

    }
}
