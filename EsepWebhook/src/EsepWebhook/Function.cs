using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        /// <summary>
        /// </summary>
        /// <param name="input">The input event from GitHub.</param>
        /// <param name="context">The Lambda context.</param>
        /// <returns>A response message indicating success or failure.</returns>
        public async Task<string> FunctionHandler(object input, ILambdaContext context)
        {
            context.Logger.LogInformation($"FunctionHandler received: {input}");

            dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());
            string issueUrl = json?.issue?.html_url;
            string issueTitle = json?.issue?.title;

            if (string.IsNullOrEmpty(issueUrl) || string.IsNullOrEmpty(issueTitle))
            {
                context.Logger.LogInformation("No issue data found in the payload.");
                return "No issue data found in the payload.";
            }

            string payload = $"{{\"text\": \"Issue Created: {issueUrl}\"}}";

            using (var client = new HttpClient())
            {
                var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (string.IsNullOrEmpty(slackUrl))
                {
                    context.Logger.LogInformation("Slack URL environment variable is missing.");
                    return "Slack URL environment variable is missing.";
                }

                var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(webRequest);
                if (response.IsSuccessStatusCode)
                {
                    context.Logger.LogInformation("Message sent to Slack successfully.");
                    return "Message sent to Slack successfully.";
                }
                else
                {
                    context.Logger.LogInformation($"Failed to send message to Slack: {response.StatusCode}");
                    return $"Failed to send message to Slack: {response.StatusCode}";
                }
            }
        }
    }
}
