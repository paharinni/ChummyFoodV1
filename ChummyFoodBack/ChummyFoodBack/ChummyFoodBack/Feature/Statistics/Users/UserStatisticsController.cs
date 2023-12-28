using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Feature.Statistics.Users
{
    [ApiController]
    [Route("Api/[controller]")]
    public class UserStatisticsController : Controller
    {
        private readonly IUserStatisticsService _userStatisticsService;

        public UserStatisticsController(IUserStatisticsService userStatisticsService)
        {
            _userStatisticsService = userStatisticsService;
        }

        [HttpGet("AverageUsersSpendingsPerOrder")]
        public IActionResult GetAverageUsersSpendingsPerOrder(int userId)
        {
            var result = _userStatisticsService.GetAverageUsersSpendingsPerOrder(userId).Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("NumberOfUserOrdersFinishedLastMonth")]
        public IActionResult GetNumberOfUserOrdersFinishedLastMonth(int userId)
        {
            var result = _userStatisticsService.GetNumberOfUserOrdersFinishedLastMonth(userId).Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("NumberOfUserOrdersStartedLastMonth")]
        public IActionResult GetNumberOfUserOrdersStartedLastMonth(int userId)
        {
            var result = _userStatisticsService.GetNumberOfUserOrdersStartedLastMonth(userId).Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("UserCount")]
        public IActionResult GetUserCount()
        {
            var result = _userStatisticsService.GetUserCount().Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }

        [HttpGet("UserOrdersLastWeek")]
        public IActionResult GetUserOrdersLastWeek(int userId)
        {
            var result = _userStatisticsService.GetUserOrdersLastWeek(userId).Result;

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            return BadRequest(result.Errors);
        }
    }
}
