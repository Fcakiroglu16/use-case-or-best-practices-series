using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "ProductsController is up and running." });
        }
    }
}
