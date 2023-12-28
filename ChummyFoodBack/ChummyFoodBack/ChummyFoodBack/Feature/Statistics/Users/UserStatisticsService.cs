using ChummyFoodBack.Persistance;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Feature.Statistics.Users
{
    public class UserStatisticsService : IUserStatisticsService
    {
        private readonly CommerceContext _context;

        public UserStatisticsService(CommerceContext commerceContext)
        {
            _context = commerceContext;
        }

        public async Task<Result<double>> GetAverageUsersSpendingsPerOrder(int userId)
        {
            try
            {
                var orders = await _context.Payments.Where(p => p.IdentityId == userId).ToListAsync();

                var totalSpendings = 0.0;

                for (int i = 0; i < orders.Count; i++)
                {
                    totalSpendings += orders[i].PaymentAmount;
                }
                
                var count = orders.Count > 0 ? orders.Count : 1;
                return Result.Ok<double>(Math.Round(totalSpendings / count, 2));
            }
            catch
            {
                return Result.Fail<double>($"User with id: {userId} was not found.");
            }
        }

        public async Task<Result<int>> GetNumberOfUserOrdersFinishedLastMonth(int userId)
        {
            var lastMonthStartDate = DateTime.Today.AddMonths(-1);
            var ordersCountPerDay = await _context.Payments
                .Where(s => s.DateOfResolove >= lastMonthStartDate)
                .Where(s => s.IdentityId == userId)
                .GroupBy(s => s.DateOfResolove)
                .ToListAsync();

            return Result.Ok<int>(ordersCountPerDay.Count);
        }

        public async Task<Result<int>> GetNumberOfUserOrdersStartedLastMonth(int userId)
        {
            var lastMonthStartDate = DateTime.Today.AddMonths(-1);
            var ordersCountPerDay = await _context.Payments
                .Where(s => s.DateOfCreation >= lastMonthStartDate)
                .Where(s => s.IdentityId == userId)
                .GroupBy(s => s.DateOfCreation)
                .ToListAsync();

            return Result.Ok<int>(ordersCountPerDay.Count);
        }

        public async Task<Result<int>> GetUserCount()
        {
            return await _context.Identities.CountAsync();
        }

        public async Task<Result<int>> GetUserOrdersLastWeek(int userId)
        {
            var lastWeekStartDate = DateTime.Today.AddDays(-7);
            var shipmentCount = await _context.Payments.CountAsync(s => s.DateOfCreation >= lastWeekStartDate 
                                                          && s.IdentityId == userId);
            return Result.Ok(shipmentCount);
        }
    }
}
