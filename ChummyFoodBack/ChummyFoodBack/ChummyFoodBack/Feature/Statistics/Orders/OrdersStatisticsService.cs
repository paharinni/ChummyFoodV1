using ChummyFoodBack.Persistance;
using ChummyFoodBack.Persistance.DAO;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Feature.Statistics.Orders
{
    public class OrdersStatisticsService : IOrdersStatisticsService
    {
        private readonly CommerceContext _context;

        public OrdersStatisticsService(CommerceContext context)
        {
            _context = context;
        }

        public async Task<Result<int>> GetAverageNumberOfOrdersPerDay()
        {
            var startDate = _context.Payments.Min(s => s.DateOfCreation).UtcDateTime;
            var endDate = DateTime.UtcNow;
            var totalDays = (endDate - startDate).TotalDays;

            if (totalDays <= 0)
            {
                return Result.Fail<int>("Invalid date range");
            }
            
            var averageShipmentsPerDay = (int)Math.Round(await _context.Payments.CountAsync() / totalDays);
            return Result.Ok(averageShipmentsPerDay);
        }

        public async Task<Result<int>> GetAverageNumberOfProductInOrders()
        {
            var orders = await _context.Payments.Include(payment => payment.RequestedProducts).ToListAsync();
            var totalProducts = 0.0;

            for (int i = 0; i < orders.Count; i++)
            {
                totalProducts += orders[i].RequestedProducts.Count();
            }

            var count = orders.Count > 0 ? orders.Count : 1;
            return Result.Ok((int)Math.Round(totalProducts / count));
        }

        public async Task<Result<double>> GetAveragePriceOfOrder()
        {
            var orders = await _context.Payments.ToListAsync();
            var totalPrice = 0.0;

            for (int i = 0; i < orders.Count; i++)
            {
                totalPrice += orders[i].PaymentAmount;
            }
            
            var count = orders.Count > 0 ? orders.Count : 1;
            return Result.Ok(Math.Round(totalPrice / count, 2));
        }

        public async Task<Result<int>> GetFinishedOrdersCountPerLastMonth()
        {
            var lastMonthStartDate = DateTime.Today.AddMonths(-1);
            var ordersCountPerDay = await _context.Payments
                .Where(s => s.DateOfResolove >= lastMonthStartDate)
                .GroupBy(s => s.DateOfResolove)
                .ToListAsync();

            return Result.Ok<int>(ordersCountPerDay.Count);
        }

        public async Task<Result<int>> GetStartedOrdersCountPerLastMonth()
        {
            var lastMonthStartDate = DateTime.Today.AddMonths(-1);
            var ordersCountPerDay = await _context.Payments
                .Where(s => s.DateOfCreation >= lastMonthStartDate)
                .GroupBy(s => s.DateOfCreation)
                .ToListAsync();

            return Result.Ok<int>(ordersCountPerDay.Count);
        }
    }
}
