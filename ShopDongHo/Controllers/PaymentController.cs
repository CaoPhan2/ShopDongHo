using Microsoft.AspNetCore.Mvc;
using ShopDongHo.Models;
using ShopDongHo.Models.VNPay;
using ShopDongHo.Services.Momo;
using ShopDongHo.Services.VnPay;

namespace ShopDongHo.Controllers
{
    

    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private IMomoService _momoService;
        
        public PaymentController(IMomoService momoService, IVnPayService vnPayService)
        {
            _momoService = momoService;
            _vnPayService = vnPayService;

        }
        [HttpPost]
        
        [Route("CreatePaymentMomo")]
        public async Task<IActionResult> CreatePaymentMomo(OrderInfoModel model)
        {
            var response = await _momoService.CreatePaymentMomo(model);
            return Redirect(response.PayUrl);
        }

        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);
            return View(response);
        }

        [HttpPost]
        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Redirect(url);
        }
        

    }
}
