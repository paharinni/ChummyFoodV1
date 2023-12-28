namespace ChummyFoodBack.Feature.VoucherManagement.Exceptions;

public class VoucherValidationException : Exception
{
    public VoucherValidationException(string message): base(message)
    {
    }
}