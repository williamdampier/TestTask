using Microsoft.AspNetCore.Mvc;
using TestTask.Models;
using TestTask.Validators;
using TestTask.Interfaces;

namespace TestTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly RequestModelValidator _validator;
        private readonly ITestService _testService;

        public TestController(RequestModelValidator validator, ITestService testService)
        {
            _validator = validator;
            _testService = testService;
        }
        [HttpPost]
        public async Task<IActionResult> Test(RequestModel request)
        {
            var validationResult = _validator.Validate(request);

            if (!validationResult.IsValid)
                return BadRequest(
                    new ResponseModel
                    {
                        IsError = 1,
                        ErrorCode = "INVALID_REQUEST",
                        ErrorMessage = validationResult.Errors[0].ErrorMessage
                    });

            var response = await _testService.Process(request);

            return Ok(response);
        }
    }
}
