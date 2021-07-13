using System;
using System.Threading.Tasks;
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
using System.IO;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace Contoso.App.SportsLeague.Functions
{
    public static class OrderFunctions
    {
        private static TelemetryClient telemetryClient = new TelemetryClient(new TelemetryConfiguration(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")));
       
        [FunctionName("CreateRandomOrder")]
        public static IActionResult CreateRandomOrder([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString")] out Order Order)
        {
            Order = null;
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("CreateRandomOrder Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            List<Product> products = Helpers.GetProducts(telemetryClient, accessToken);
            if(products.Count == 0) {
                return new BadRequestObjectResult("No Products Found");
            }

            int orderDetailCount = new Random().Next(1,products.Count);
            telemetryClient.TrackTrace(string.Format("Making a random Order with {0} Random Order Detail.", orderDetailCount));
            
            ICollection<OrderDetail> orderDetails = new List<OrderDetail>(orderDetailCount);
            for (int i = 0; i < orderDetailCount; i++) {
                Product randomProduct = products.ToArray()[new Random().Next(1,products.Count)];
                orderDetails.Add(new OrderDetail() { LineItemNumber = i, ProductId = randomProduct.id, Quantity = new Random().Next(1,10), UnitPrice = new Random().Next(1,99) + new Random().NextDouble() });   
                bool removed = products.Remove(randomProduct);
            }
            
            Order order = new Order() { 
                Address = "Address Line 1",
                City = "City",
                Country = "Country",
                Email = "email@address.com",
                FirstName = "First Name",
                HasBeenShipped = true,
                id = Guid.NewGuid().ToString(),
                LastName = "Last Name", 
                OrderDate = DateTime.UtcNow,
                OrderDetails = orderDetails,
                PaymentTransdactionId = "PaymentTransdactionId",
                Phone = "(555) 123-4567",
                PostalCode = "PostalCode",
                ReceiptUrl = "http://www.ReceiptUrl.com",
                SMSOptIn = "true",
                SMSStatus = "SMSStatus",
                State = "State",
                Total = 123.45
            };

             Order = order;
             return new OkObjectResult(Order);
         }

        [FunctionName("CreateOrder")]
        public static IActionResult CreateOrder([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString")] out Order Order)
        {
            Order = null;
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("CreateOrder Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();
            
             string requestBody = new StreamReader(req.Body).ReadToEnd();
             Order inputOrder = JsonConvert.DeserializeObject<Order>(requestBody);

            if(inputOrder.OrderDetails.Count == 0)
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult("Order Details are required.");

            if(string.IsNullOrWhiteSpace(inputOrder.Address) || 
                string.IsNullOrWhiteSpace(inputOrder.City) ||
                string.IsNullOrWhiteSpace(inputOrder.Country) ||
                string.IsNullOrWhiteSpace(inputOrder.Email) ||
                string.IsNullOrWhiteSpace(inputOrder.FirstName) ||
                string.IsNullOrWhiteSpace(inputOrder.LastName) ||
                string.IsNullOrWhiteSpace(inputOrder.OrderDate.ToString()) ||
                string.IsNullOrWhiteSpace(inputOrder.Phone) ||
                string.IsNullOrWhiteSpace(inputOrder.PostalCode) ||
                string.IsNullOrWhiteSpace(inputOrder.State)
            ) 
            return new BadRequestObjectResult("Incomplete Order Data");
             
             if (string.IsNullOrWhiteSpace(inputOrder.id))
                inputOrder.id = Guid.NewGuid().ToString();

             Order = inputOrder;

             return new OkObjectResult(Order);
         }

        [FunctionName("UpdateOrder")]
        public static IActionResult UpdateOrder([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString")] out Order Order)
        {
            Order = null;
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("UpdateOrder Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();
            
             string requestBody = new StreamReader(req.Body).ReadToEnd();
             Order inputOrder = JsonConvert.DeserializeObject<Order>(requestBody);

             Order = inputOrder;

             return new OkObjectResult(Order);
        }

        [FunctionName("DeleteOrder")]
        public static async Task<IActionResult> DeleteOrder([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req, ILogger log, [CosmosDB(ConnectionStringSetting = "CosmosDBConnectionString")] DocumentClient client)
        {
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("DeleteOrder Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            string orderId = req.Query["OrderId"];
            if (string.IsNullOrWhiteSpace(orderId))
                return new BadRequestObjectResult("Please provide an OrderId on the querystring e.g. ?OrderId=f44774dc-7982-4dee-81ef-cfb462ceac8d");

            try {
                await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri("SportsLeague", "Data", orderId), new RequestOptions { PartitionKey = new PartitionKey("Order") });
            } catch (DocumentClientException){
                return new NotFoundResult();
            } catch (Exception ex) {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkResult();
        }

        [FunctionName("GetOrder")]
        public static IActionResult GetOrder([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString", Id = "{Query.OrderId}", PartitionKey = "Order")] Order Order)
        {
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("GetOrder Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            string OrderId = req.Query["OrderId"];

            if (string.IsNullOrWhiteSpace(OrderId))
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult("Please provide a OrderId on the querystring e.g. ?OrderId=f44774dc-7982-4dee-81ef-cfb462ceac8d");
            
            if (Order is null)
                return new Microsoft.AspNetCore.Mvc.NotFoundObjectResult("Not found");

           return new OkObjectResult(Order);
        }

        [FunctionName("GetOrders")]
        public static IActionResult GetOrders([HttpTrigger(AuthorizationLevel.Function, "get",  Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString", SqlQuery = "SELECT * FROM c where c.DataType = 'Order' order by c.OrderDate desc")] IEnumerable<Order> Orders) {

            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("GetOrders Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            if (Orders.Count() == 0)
                return new NotFoundResult();
            else
                return new OkObjectResult(Orders);
        }  
    }
}