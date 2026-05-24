using System.Net;
using System.Net.Mail;
namespace ShopDongHo.Areas.Admin.Repository
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true, // batj bao mat
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("caophan210604@gmail.com", "izxixzakyftygtsy")
            };
            return client.SendMailAsync(
                new MailMessage(from: "caophan210604@gmail.com",
                                to: email,
                                subject,
                                message
                                ));
        }
    }
}
