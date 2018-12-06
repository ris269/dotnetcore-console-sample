using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;

namespace ConsoleGraphTest
{
    class Program
    {
        private static GraphServiceClient _graphServiceClient;
        private static HttpClient _httpClient;

        static void Main(string[] args)
        {
            // Load appsettings.json
            var config = LoadAppSettings();
            if (null == config)
            {
                Console.WriteLine("Missing or invalid appsettings.json file. Please see README.md for configuration instructions.");
                return;
            }

            //Query using Graph SDK (preferred when possible)
            GraphServiceClient graphClient = GetAuthenticatedGraphClient(config);
            // List<QueryOption> options = new List<QueryOption>
            // {
            //     new QueryOption("$top", "1")
            // };

            // var graphResult = graphClient.Users.Request(options).GetAsync().Result;
            // Console.WriteLine("Graph SDK Result");
            // Console.WriteLine(graphResult[0].DisplayName);

            //Direct query using HTTPClient (for beta endpoint calls or not available in Graph SDK)
            HttpClient httpClient = GetAuthenticatedHTTPClient(config);
            // Uri Uri = new Uri("https://graph.microsoft.com/v1.0/users?$top=1");
            // var httpResult = httpClient.GetStringAsync(Uri).Result;

            // Console.WriteLine("HTTP Result");
            // Console.WriteLine(httpResult);
            OneDriveHelperCall();
        }

        private static void OneDriveHelperCall()
        {
            const string smallFilePath = @"SampleFiles\SmallFile.txt";
            const string largeFilePath = @"SampleFiles\LargeFile.txt";

            // change this bool to false to upload to OneDrive site instead
            bool uploadToSharePoint = true;

            var oneDriveHelper = new OneDriveHelper(_graphServiceClient);
   
            var uploadedSmallFile = oneDriveHelper.UploadSmallFile(smallFilePath, uploadToSharePoint).GetAwaiter().GetResult();
            if(uploadedSmallFile != null)
            {
                Console.WriteLine($"Uploaded file {smallFilePath} to {uploadedSmallFile.WebUrl}.");
            }
            else
            {
                Console.WriteLine($"Failure uploading {smallFilePath}");
            }
            
            var uploadedLargeFile = oneDriveHelper.UploadLargeFile(largeFilePath, uploadToSharePoint).GetAwaiter().GetResult();
            if(uploadedLargeFile != null)
            {
                Console.WriteLine($"Uploaded file {largeFilePath} to {uploadedLargeFile.WebUrl}.");
            }
            else
            {
                Console.WriteLine($"Failure uploading {largeFilePath}");
            }
        }

        private static GraphServiceClient GetAuthenticatedGraphClient(IConfigurationRoot config)
        {
            var authenticationProvider = CreateAuthorizationProvider(config);
            _graphServiceClient = new GraphServiceClient(authenticationProvider);
            return _graphServiceClient;
        }

        private static HttpClient GetAuthenticatedHTTPClient(IConfigurationRoot config)
        {
            var authenticationProvider = CreateAuthorizationProvider(config);
            _httpClient = new HttpClient(new AuthHandler(authenticationProvider, new HttpClientHandler()));
            return _httpClient;
        }

        private static IAuthenticationProvider CreateAuthorizationProvider(IConfigurationRoot config)
        {
            var clientId = config["applicationId"];
            var clientSecret = config["applicationSecret"];
            var redirectUri = config["redirectUri"];
            var authority = $"https://login.microsoftonline.com/{config["tenantId"]}";

            List<string> scopes = new List<string>();
            scopes.Add("https://graph.microsoft.com/.default");

            var cca = new PublicClientApplication(clientId, authority);
            return new DeviceCodeFlowAuthorizationProvider(cca, scopes);
        }

        private static IConfigurationRoot LoadAppSettings()
        {
            try
            {
                var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .Build();

                // Validate required settings
                if (string.IsNullOrEmpty(config["applicationId"]) ||
                    string.IsNullOrEmpty(config["applicationSecret"]) ||
                    string.IsNullOrEmpty(config["redirectUri"]) ||
                    string.IsNullOrEmpty(config["tenantId"]))
                {
                    return null;
                }

                return config;
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
        }
    }
}