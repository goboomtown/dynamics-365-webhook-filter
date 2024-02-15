using Microsoft.Xrm.Sdk;
using System;
using System.Net.Http;


namespace OvationCXMFilter.Helpers
{
    /// <summary>
    /// Trigger a OvationCXM webhook 
    /// </summary>
    /// <param name="context">Execution context from the service provider.</param>
    /// <param name="trace">ITracingService object</param>
    /// <exception cref="InvalidPluginExecutionException">Throw exception on webhook trigger failure</exception>
    public class TriggerWebhook
    {
        public void Webhook(IPluginExecutionContext context, ITracingService trace, string webhookURL)
        {

            try
            {
                string requestName = context.MessageName.ToLower();
                Entity entity = (Entity)context.InputParameters["Target"];
                object contextCopy = context;

                using (HttpClient client = new HttpClient())
                {
                    var guid = Guid.NewGuid().ToString();
                    // Add headers if necessary.
                    client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", guid);
                    client.DefaultRequestHeaders.Add("x-ms-dynamics-request-name", requestName);
                    client.DefaultRequestHeaders.Add("x-ms-dynamics-entity-name", entity.LogicalName);
                    client.DefaultRequestHeaders.Add("x-ovationcxm-request-id", guid);

                    // Serialize contextCopy to JSON
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(contextCopy);

                    // You can customize the request (headers, payload, etc.) based on your webhook requirements.
                    StringContent content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                    // Send the POST request to the webhook endpoint.
                    HttpResponseMessage response = client.PostAsync(webhookURL, content).Result;

                    // Check the response if needed.
                    if (response.IsSuccessStatusCode)
                    {
                        trace.Trace(response.Content.ToString());
                    }
                    else
                    {
                        // Handle failure.
                        throw new Exception($"Webhook request failed. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions.
                throw new InvalidPluginExecutionException($"Error triggering webhook: {ex.Message}", ex);
            }
        }
    }
}