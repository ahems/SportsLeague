using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Contoso.App.SportsLeague.Functions.Models;
using System.Threading.Tasks;

namespace Contoso.App.SportsLeague.Functions
{
    public static class ProductFunctions
    {
        private static TelemetryClient telemetryClient = new TelemetryClient(new TelemetryConfiguration(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")));
       
        [FunctionName("GetProduct")]
        public static IActionResult GetProduct([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString", Id = "{Query.productId}", PartitionKey = "Product")] Product product)
        {
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("GetProduct Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            string productId = req.Query["productId"];

            if (string.IsNullOrWhiteSpace(productId))
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult("Please provide a productId on the querystring e.g. ?productId=f44774dc-7982-4dee-81ef-cfb462ceac8d");
            
            if (product is null)
                return new Microsoft.AspNetCore.Mvc.NotFoundObjectResult("Not found");

           return new OkObjectResult(product);
        }

        [FunctionName("GetProducts")]
        public static async Task<IActionResult> GetProducts([HttpTrigger(AuthorizationLevel.Function, "get",  Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString", SqlQuery = "SELECT * FROM c where c.DataType = 'Product' order by c.ProductId asc")] IEnumerable<Product> products) {

            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = await Helpers.ValidateAccessToken(accessToken, log);

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("GetProducts Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            if (products.Count() == 0)
                return new NotFoundResult();
            else
                return (ActionResult)new OkObjectResult(products);
        }
    }
}