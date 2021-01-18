using System.Diagnostics.CodeAnalysis;
using Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace FunctionApp
{
    public static class FunctionApi
    {
        [FunctionName("CheckForPrime")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "http trigger")]
        public static ActionResult CheckForPrime(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "check/{i}")]
            HttpRequest request, int i)
            => new OkObjectResult(DemoWorkload.CheckForPrime(i));

        [FunctionName("ListPrimesBetween")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "http trigger")]
        public static ActionResult ListPrimesBetween(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "between/{from}/{to}")]
            HttpRequest request, int from, int to)
            => new OkObjectResult(DemoWorkload.ListPrimesBetween(from, to));
    }
}