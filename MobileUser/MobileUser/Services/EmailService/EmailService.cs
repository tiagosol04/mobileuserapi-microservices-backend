namespace MobileUser.Services.EmailService
{
    using System.Net;
    using System.Net.Mail;

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendDelegationInviteAsync(string toEmail, string inviteLink)
        {
            var host = _configuration["Email:SmtpHost"];
            var port = int.Parse(_configuration["Email:SmtpPort"]!);
            var senderEmail = _configuration["Email:SenderEmail"];
            var senderPassword = _configuration["Email:SenderPassword"];

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(senderEmail, senderPassword)
            };

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail!, "A-Mover"),
                Subject = "Convite para usar uma moto A-Mover",
                Body = $"""
                Foste convidado para usar uma moto A-Mover.

                Para aceitar ou rejeitar o convite, abre o seguinte link:

                {inviteLink}
                """,
                    IsBodyHtml = false
                };

            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
    }
}
