using backend.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceiveController : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<ApiResult>> ReceiveIn()
        {
            return Ok();
        }
    }
}
