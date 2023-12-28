using FluentResults;

namespace ChummyFoodBack.Feature.Statistics.Orders
{
    public interface IOrdersStatisticsService
    {
        Task<Result<int>> GetAverageNumberOfProductInOrders();

        Task<Result<int>> GetStartedOrdersCountPerLastMonth();

        Task<Result<int>> GetFinishedOrdersCountPerLastMonth();

        Task<Result<int>> GetAverageNumberOfOrdersPerDay();

        Task<Result<double>> GetAveragePriceOfOrder();
    }
}
