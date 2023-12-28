namespace ChummyFoodBack.Interactions.Intefaces;

public interface IMailInteractionService
{
    public Task SendMessageToSingleReceiver(SendMailToOneReceiver mailToOneReceiver);
}