namespace ChummyFoodBack.Shared;

public class OperationResult<TFailModel>
{
    public bool IsSuccess { get; set; }

    public TFailModel? Error { get; set; }

}
