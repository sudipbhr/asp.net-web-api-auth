using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Message = "You have access to protected endpoint" });
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("customer")]
        public IActionResult GetCustomer()
        {
            return Ok(new { Message = "This endpoint is only accessible to users with the Customer role" });
        }
    }
}