using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
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
    public static class CartFunctions
    {
        private static TelemetryClient telemetryClient = new TelemetryClient(new TelemetryConfiguration(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")));
       
        [FunctionName("CreateRandomCart")]
        public static IActionResult CreateRandomCart([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString")] out Cart cart)
        {
            cart = null;
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("CreateRandomCart Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            List<Product> products = Helpers.GetProducts(telemetryClient, accessToken);
            if(products.Count == 0) {
                cart = null;
                return new BadRequestObjectResult("No Products Found");
            }

            int cartItemsCounter = new Random().Next(1,products.Count);
            telemetryClient.TrackTrace(string.Format("CreateRandomCart Called - making a cart with {0} Random CartItems.", cartItemsCounter));
            List<CartItem> randomCartItems = new List<CartItem>(cartItemsCounter);
            
            for (int i = 0; i < cartItemsCounter; i++) {
                Product randomProduct = products.ToArray()[new Random().Next(1,products.Count)];
                randomCartItems.Add(new CartItem { CartItemId = Guid.NewGuid().ToString(), Quantity = new Random().Next(1,15), ProductId = randomProduct.id });
                bool removed = products.Remove(randomProduct);
            }

            cart = new Cart { DateCreated = DateTime.Now, id = Guid.NewGuid().ToString(), CartItems = randomCartItems };

            return new OkObjectResult(cart);
         }
         
        [FunctionName("DeleteCart")]
        public static async Task<IActionResult> DeleteCart([HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req, ILogger log, [CosmosDB(ConnectionStringSetting = "CosmosDBConnectionString")] DocumentClient client)
        {
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("DeleteCart Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            string cartId = req.Query["CartId"];
            if (string.IsNullOrWhiteSpace(cartId))
                return new BadRequestObjectResult("Please provide a CartId on the querystring e.g. ?CartId=f44774dc-7982-4dee-81ef-cfb462ceac8d");

            try {
                await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri("SportsLeague", "Data", cartId), new RequestOptions { PartitionKey = new PartitionKey("Cart") });
            } catch (DocumentClientException){
                return new NotFoundResult();
            } catch (Exception ex) {
                return new BadRequestObjectResult(ex.Message);
            }

            return new OkResult();
        }

        [FunctionName("CreateCart")]
        public static IActionResult CreateCart([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString")] out Cart cart)
        {
            cart = null;
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("CreateCart Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

             List<CartItem> cartItems = new List<CartItem>();

             string requestBody = new StreamReader(req.Body).ReadToEnd();
             if(!string.IsNullOrEmpty(requestBody)) {
                Cart inputCart = JsonConvert.DeserializeObject<Cart>(requestBody);
                if (string.IsNullOrWhiteSpace(inputCart.id))
                    inputCart.id = Guid.NewGuid().ToString();
                if(inputCart.DateCreated == null)
                    inputCart.DateCreated = DateTime.UtcNow;            
                cart = inputCart;
             } else {
                 cart = new Cart { DateCreated = DateTime.Now, id = Guid.NewGuid().ToString(), CartItems = cartItems };
             }

             return new OkObjectResult(cart);
         }

        [FunctionName("UpdateCart")]
        public static IActionResult UpdateCart([HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString")] out Cart cart)
        {
            cart = null;
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = Helpers.ValidateAccessToken(accessToken, log).GetAwaiter().GetResult();

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("UpdateCart Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();
            
             string requestBody = new StreamReader(req.Body).ReadToEnd();
             Cart inputCart = JsonConvert.DeserializeObject<Cart>(requestBody);

             if (string.IsNullOrWhiteSpace(inputCart.id))
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult("Please provide a Cart ID");
            
            if(inputCart.DateCreated == null)
                inputCart.DateCreated = DateTime.UtcNow;
            
             cart = inputCart;

             return new OkObjectResult(cart);
         }

        [FunctionName("GetCart")]
        public static async Task<IActionResult> GetCart([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString", Id = "{Query.CartId}", PartitionKey = "Cart")] Cart Cart)
        {
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = await Helpers.ValidateAccessToken(accessToken, log);

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("GetCart Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            string CartId = req.Query["CartId"];

            if (string.IsNullOrWhiteSpace(CartId))
                return new BadRequestObjectResult("Please provide a CartId on the querystring e.g. ?CartId=f44774dc-7982-4dee-81ef-cfb462ceac8d");
            
            if (Cart is null)
                return new NotFoundObjectResult("Not found");

           return new OkObjectResult(Cart);
        }

        [FunctionName("GetCarts")]
        public static async Task<IActionResult> GetCarts([HttpTrigger(AuthorizationLevel.Function, "get",  Route = null)] HttpRequest req, ILogger log, [CosmosDB("SportsLeague", "Data", ConnectionStringSetting = "CosmosDBConnectionString", SqlQuery = "SELECT * FROM c where c.DataType = 'Cart' order by c.DateCreated asc")] IEnumerable<Cart> carts) {
            var accessToken = Helpers.GetAccessToken(req);
            var claimsPrincipal = await Helpers.ValidateAccessToken(accessToken, log);

            if (claimsPrincipal != null)
                telemetryClient.TrackTrace(string.Format("GetCarts Called by {0}", claimsPrincipal.Identity.Name));
            else return (ActionResult)new UnauthorizedResult();

            return new OkObjectResult(carts);
        }  
    }
}