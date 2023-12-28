using FluentResults;

namespace ChummyFoodBack.Feature.Statistics.Users
{
    public interface IUserStatisticsService
    {
        Task<Result<int>> GetNumberOfUserOrdersFinishedLastMonth(int userId);

        Task<Result<int>> GetNumberOfUserOrdersStartedLastMonth(int userId);

        Task<Result<int>> GetUserCount();

        Task<Result<int>> GetUserOrdersLastWeek(int userId);

        Task<Result<double>> GetAverageUsersSpendingsPerOrder(int userId);
    }
}
