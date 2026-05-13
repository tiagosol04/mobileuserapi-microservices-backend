namespace MobileUser.Services.EmailService
{
    public interface IEmailService
    {
        Task SendDelegationInviteAsync(string toEmail, string inviteLink);
    }
}
