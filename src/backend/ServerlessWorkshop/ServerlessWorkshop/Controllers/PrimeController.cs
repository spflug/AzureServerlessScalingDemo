using Implementation;
using Microsoft.AspNetCore.Mvc;

namespace ServerlessWorkshop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrimeController : ControllerBase
    {
        [HttpGet]
        [Route("check/{i}")]
        public ActionResult CheckForPrime(int i)
            => Ok(DemoWorkload.CheckForPrime(i));

        [HttpGet]
        [Route("between/{from}/{to}")]
        public ActionResult ListPrimesBetween(int from, int to)
            => Ok(DemoWorkload.ListPrimesBetween(from, to));
    }
}