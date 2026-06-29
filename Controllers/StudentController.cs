using Microsoft.AspNetCore.Mvc;

namespace OmrSheet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok(new { Message = "Student Controller Working" });
        }
    }
}