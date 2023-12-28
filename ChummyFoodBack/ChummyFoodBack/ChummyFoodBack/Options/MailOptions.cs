namespace ChummyFoodBack.Options;

public class MailOptions
{
    public const string Mail = "Mail";

    public string SenderEmail { get; set; }

    public string SmtpServer { get; set; }

    public int ServerPort { get; set; } = 587;
    
    public string Password { get; set; }
}
