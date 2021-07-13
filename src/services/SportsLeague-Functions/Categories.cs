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

namespace Contoso.App.SportsLeague.Functions
{
    public static class CategoryFunctions
    {
        private static TelemetryClient telemetryClient = new TelemetryClient(new TelemetryConfiguration(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")));
       
        [FunctionName("GetCategory")]
        public static IActionResult GetCategory([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString", Id = "{Query.CategoryId}", PartitionKey = "Category")] Category Category)
        {
            
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("GetCategory Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            string CategoryId = req.Query["CategoryId"];

            if (string.IsNullOrWhiteSpace(CategoryId))
                return new BadRequestObjectResult("Please provide a CategoryId on the querystring e.g. ?CategoryId=f44774dc-7982-4dee-81ef-cfb462ceac8d");
            
            if (Category is null)
                return new NotFoundObjectResult("Not found");

           return new OkObjectResult(Category);
        }

        [FunctionName("GetCategories")]
        public static IActionResult GetCategories([HttpTrigger(AuthorizationLevel.Function, "get",  Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString", SqlQuery = "SELECT * FROM c where c.DataType = 'Category' order by c.CategoryId asc")] IEnumerable<Category> Categories) {

            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("GetCategories Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            if (Categories.Count() == 0)
                return new NotFoundResult();
            else
                return new OkObjectResult(Categories);
        }        
    }
}