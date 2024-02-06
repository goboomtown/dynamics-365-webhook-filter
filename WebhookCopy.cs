using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Services;
using System.ServiceModel;
using System.Text;
using System.Web;

namespace OvationCXMFilter.Plugins
{
    public class WebhookCopy : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Check if the plugin is executing in the post-operation stage of the "Create" or "Update" message.
            string requestName = context.MessageName.ToLower();
            if ((requestName == "create" || requestName == "update") && context.Stage == 40) // Post-operation stage
            {
                // Ensure the target entity is present in the context.
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    // Get the target entity from the context.
                    Entity entity = (Entity)context.InputParameters["Target"];
                    try
                    {
                        // Check if the entity is of the desired type (replace "contact" with your actual entity logical name).
                        if (entity.LogicalName.Equals("account", StringComparison.OrdinalIgnoreCase))
                        {
                            trace.Trace("Validating filter conditions...");
                            // Check the conditions before triggering the webhook.
                            if (context.UserId != null && CheckConditions(entity, requestName))
                            {
                                trace.Trace("Payload validated and triggering webhook...");
                                // Trigger the webhook.
                                TriggerWebhook(context, requestName);
                                trace.Trace("Webhook triggered !!!");
                            }
                        }
                    }
                    catch (FaultException<OrganizationServiceFault> ex)
                    {
                        throw new InvalidPluginExecutionException("An error occurred in OvationCXM Plugin.", ex);
                    }
                    catch (Exception ex)
                    {
                        trace.Trace("OvationCXM Plugin Exception: {0}", ex.ToString());
                        throw;
                    }
                }
            }
        }

        private bool CheckConditions(Entity entity, string requestName)
        {
            try
            {
                // webhook endpoint URL.
                string webhookUrl = "https://connector-msdynamics-api-pwgavvro5q-uc.a.run.app/api/filter?orgId=PPK1";
                using (HttpClient client = new HttpClient())
                {
                    var guid = Guid.NewGuid().ToString();
                    // Add headers if necessary.
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                    client.DefaultRequestHeaders.Add("x-ovationcxm-request-id", guid);

                    // Send the GET request to the webhook endpoint.
                    HttpResponseMessage response = client.GetAsync(webhookUrl).Result;

                    // Check the response if needed.
                    if (response.IsSuccessStatusCode)
                    {
                        string responseData = response.Content.ReadAsStringAsync().Result;
                        return true;
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

        private void TriggerWebhook(IPluginExecutionContext context, string requestName)
        {
            try
            {
                Entity entity = (Entity)context.InputParameters["Target"];
                // webhook endpoint URL.
                string webhookUrl = "https://webhook.site/4ccd134a-67ad-450e-94fd-fa218d34e8d2";
                using (HttpClient client = new HttpClient())
                {
                    var guid = Guid.NewGuid().ToString();
                    // Add headers if necessary.
                    client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", guid);
                    client.DefaultRequestHeaders.Add("x-ms-dynamics-request-name", requestName);
                    client.DefaultRequestHeaders.Add("x-ms-dynamics-entity-name", entity.LogicalName);
                    client.DefaultRequestHeaders.Add("x-ovationcxm-request-id", guid);

                    // You can customize the request (headers, payload, etc.) based on your webhook requirements.
                    var payload = JsonConvert.SerializeObject(context);

                    // You can customize the request (headers, payload, etc.) based on your webhook requirements.
                    StringContent content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

                    // Send the POST request to the webhook endpoint.
                    HttpResponseMessage response = client.PostAsync(webhookUrl, content).Result;

                    // Check the response if needed.
                    if (response.IsSuccessStatusCode)
                    {
                        // Handle success.
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
