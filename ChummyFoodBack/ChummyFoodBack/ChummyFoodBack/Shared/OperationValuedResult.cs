namespace ChummyFoodBack.Shared;


public class OperationValuedResult<TResulModel, TFailModel>: OperationResult<TFailModel>
{
    public TResulModel? Result { get; set; }
}