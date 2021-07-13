using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights;
using Contoso.App.SportsLeague.Functions.Models;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

namespace Contoso.App.SportsLeague.Functions
{
    internal static class Constants
    {
        internal static string audience = Environment.GetEnvironmentVariable("audience"); // Get this value from the expose an api, audience uri section example https://appname.tenantname.onmicrosoft.com
        internal static string clientID = Environment.GetEnvironmentVariable("clientID"); // this is the client id, also known as AppID. This is not the ObjectID
        internal static string tenant = string.Format("{0}.onmicrosoft.com", Environment.GetEnvironmentVariable("tenantname")); // this is your tenant name
        internal static string tenantid = Environment.GetEnvironmentVariable("tenantid"); // this is your tenant id (GUID)

        // rest of the values below can be left as is in most circumstances
        internal static string aadInstance = "https://login.microsoftonline.com/{0}/v2.0";
        internal static string authority = string.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
        internal static List<string> validIssuers = new List<string>()
            {
                $"https://login.microsoftonline.com/{tenant}/",
                $"https://login.microsoftonline.com/{tenant}/v2.0",
                $"https://login.windows.net/{tenant}/",
                $"https://login.microsoft.com/{tenant}/",
                $"https://sts.windows.net/{tenantid}/"
            };
    }

    public static class Helpers
    
    {
        public static string GetAccessToken(HttpRequest req)
        {
            var authorizationHeader = req.Headers?["Authorization"];
            string[] parts = authorizationHeader?.ToString().Split(null) ?? new string[0];
            if (parts.Length == 2 && parts[0].Equals("Bearer"))
                return parts[1];
            return null;
        }

        public static async Task<ClaimsPrincipal> ValidateAccessToken(string accessToken, ILogger log)
        {
            var audience = Constants.audience;
            if(string.IsNullOrEmpty(audience)) throw new Exception("Missing 'audience' Environment Variable");
            var clientID = Constants.clientID;
            if(string.IsNullOrEmpty(clientID)) throw new Exception("Missing 'clientID' Environment Variable");
            var tenant = Constants.tenant;
            if(tenant == ".onmicrosoft.com") throw new Exception("Missing 'tenant' Environment Variable");
            var tenantid = Constants.tenantid;
            if(string.IsNullOrEmpty(tenantid)) throw new Exception("Missing 'tenantid' Environment Variable");
            var aadInstance = Constants.aadInstance;
            var authority = Constants.authority;
            var validIssuers = Constants.validIssuers;

            // Debugging purposes only, set this to false for production
            // Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            ConfigurationManager<OpenIdConnectConfiguration> configManager =
                new ConfigurationManager<OpenIdConnectConfiguration>(
                    $"{authority}/.well-known/openid-configuration",
                    new OpenIdConnectConfigurationRetriever());

            OpenIdConnectConfiguration config = null;
            config = await configManager.GetConfigurationAsync();

            ISecurityTokenValidator tokenValidator = new JwtSecurityTokenHandler();

            // Initialize the token validation parameters
            TokenValidationParameters validationParameters = new TokenValidationParameters
            {
                // App Id URI and AppId of this service application are both valid audiences.
                ValidAudiences = new[] { audience, clientID },

                // Support Azure AD V1 and V2 endpoints.
                ValidIssuers = validIssuers,
                IssuerSigningKeys = config.SigningKeys
            };

            try
            {
                SecurityToken securityToken;
                var claimsPrincipal = tokenValidator.ValidateToken(accessToken, validationParameters, out securityToken);
                return claimsPrincipal;
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
            }
            return null;
        }
        public static async Task<string> GetSentimentAsync(string userNotes, string key)
        {
            string sentimentAnalysisUrl = "https://eastus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            var body = new JObject() as dynamic;
            body.documents = new JArray() as dynamic;

            dynamic doc = new JObject();
            doc.language = "en";
            doc.id = "1";
            doc.text = userNotes;

            body.documents.Add(doc);

            string postBody = JsonConvert.SerializeObject(body);

            HttpRequestMessage msg = new HttpRequestMessage
            {
                Content = new StringContent(postBody, System.Text.Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = new Uri(sentimentAnalysisUrl)
            };
            var responseMessage = await httpClient.SendAsync(msg);
            responseMessage.EnsureSuccessStatusCode();

            var result = await responseMessage.Content.ReadAsAsync<dynamic>();

            return result.documents[0].score;
        }
        public static List<Product> GetProducts(TelemetryClient telemetryClient, string accessToken) {

            if(string.IsNullOrEmpty(accessToken))
                throw new Exception("Missing accessToken");

            List<Product> products = new List<Product>();
            string key = Environment.GetEnvironmentVariable("SportsLeagueAPIKey");
            if(string.IsNullOrEmpty(key))
                throw new Exception("Missing 'SportsLeagueAPIKey' Environment Variable");

            HttpClient productClient = new HttpClient();
            productClient.DefaultRequestHeaders.Clear();
            productClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            productClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
            productClient.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", accessToken));

            productClient.BaseAddress = new Uri("https://sportsleague.azure-api.net/api/");

            try {
                HttpResponseMessage getProductResponse = productClient.GetAsync("GetProducts").Result;
                var getProductResponseAsJson = getProductResponse.Content.ReadAsStringAsync().Result;            
                products = JsonConvert.DeserializeObject<List<Product>>(getProductResponseAsJson);
            } catch (Exception ex) {
                telemetryClient.TrackException(ex);
            }
            return products;
         }
    }
}