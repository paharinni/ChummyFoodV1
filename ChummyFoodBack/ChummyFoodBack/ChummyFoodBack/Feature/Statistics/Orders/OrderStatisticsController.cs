using Microsoft.AspNetCore.Mvc;

namespace ChummyFoodBack.Feature.Statistics.Orders
{
    [ApiController]
    [Route("Api/[controller]")]
    public class OrderStatisticsController : Controller
    {
        private readonly IOrdersStatisticsService _orderStatisticService;

        public OrderStatisticsController(IOrdersStatisticsService orderStatisticService)
        {
            _orderStatisticService = orderStatisticService;
        }

        [HttpGet("AverageNumberOfOrdersPerDay")]
        public IActionResult GetAverageNumberOfOrdersPerDay()
        {
            var result = _orderStatisticService.GetAverageNumberOfOrdersPerDay().Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("AverageNumberOfProductInOrders")]
        public IActionResult GetAverageNumberOfProductInOrders()
        {
            var result = _orderStatisticService.GetAverageNumberOfProductInOrders().Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("AveragePriceOfOrder")]
        public IActionResult GetAveragePriceOfOrder()
        {
            var result = _orderStatisticService.GetAveragePriceOfOrder().Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("FinishedOrdersCountPerLastMonth")]
        public IActionResult GetFinishedOrdersCountPerLastMonth()
        {
            var result = _orderStatisticService.GetFinishedOrdersCountPerLastMonth().Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("StartedOrdersCountPerLastMonth")]
        public IActionResult GetStartedOrdersCountPerLastMonth()
        {
            var result = _orderStatisticService.GetStartedOrdersCountPerLastMonth().Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }
    }
}
