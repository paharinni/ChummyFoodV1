using ChummyFoodBack.Interactions.Intefaces;
using ChummyFoodBack.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ChummyFoodBack.Interactions;

public class BaseMailSenderModel<TRecipient>
{
    public TRecipient TargetRecipient { get; set; }
    public string Payload { get; set; }
    public string MailSubject { get; set; }
}

public class SendMailToOneReceiver : BaseMailSenderModel<string>
{
    
}

public class SendMailToMultiReceivers : BaseMailSenderModel<IEnumerable<string>>
{
    
}


public class MailInteractionService: IMailInteractionService
{
    private readonly MailOptions _mailOptions;

    public MailInteractionService(IOptions<MailOptions> mailSettings)
    {
        _mailOptions = mailSettings.Value;
    }
    public async Task SendMessageToSingleReceiver(SendMailToOneReceiver mailToOneReceiver)
    {
        var email = new MimeMessage();
        email.Sender = MailboxAddress.Parse(_mailOptions.SenderEmail);
        email.To.Add(MailboxAddress.Parse(mailToOneReceiver.TargetRecipient));
        email.Subject = mailToOneReceiver.MailSubject;

        var body = new BodyBuilder();
        body.HtmlBody = mailToOneReceiver.Payload;
        email.Body = body.ToMessageBody();
        using var smtpClient = new SmtpClient();
        //In case of gmail account probability of getting 
        await smtpClient.ConnectAsync(_mailOptions.SmtpServer,
            _mailOptions.ServerPort,
            SecureSocketOptions.StartTls);
        await smtpClient.AuthenticateAsync(_mailOptions.SenderEmail, _mailOptions.Password);
        await smtpClient.SendAsync(email);
        await smtpClient.DisconnectAsync(true);
    }
}
