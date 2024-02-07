using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;

namespace OvationCXMFilter.Plugins
{
    public enum RequestName
    {
        create,
        update
    }

    public enum EntityName
    {
        account,
        contact
    }

    public static class Constant
    {
        public static string[] entities = { "account", "contact" };
        public static string webhookUrl = "https://webhook.site/4cafb5e7-e915-47f5-a01c-bcdf40962640";
        public static string filterUrl = "https://connector-msdynamics-api-pwgavvro5q-uc.a.run.app/api/filter?orgId=PPK1";
    }

    public static class TraceMessage
    {
        public static string validationStart = "Validating filter conditions...";
        public static string validPayload = "Payload validated and triggering webhook...";
        public static string webhookTriggered = "Webhook triggered !!!";
        public static string pluginFaultException = "An error occurred in OvationCXM Plugin.";
        public static string pluginError = "OvationCXM Plugin Exception: {0}";
    }

    public class Webhook : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            

            // Check if the plugin is executing in the post-operation stage of the "Create" or "Update" message.
            string requestName = context.MessageName.ToLower();
            if ((requestName.Equals(RequestName.create.ToString()) || requestName.Equals(RequestName.update.ToString())) && context.Stage == 40) // Post-operation stage
            {
                // Ensure the target entity is present in the context.
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    // Get the target entity from the context.
                    Entity entity = (Entity)context.InputParameters["Target"];
                    try
                    {
                        // Check if the entity is of the desired type (replace "contact" with your actual entity logical name).
                        if (Constant.entities.Contains(entity.LogicalName))
                        {
                            trace.Trace(TraceMessage.validationStart);
                            // Check the conditions before triggering the webhook.
                            if (context.UserId != null && CheckConditions(context, requestName))
                            {
                                trace.Trace(TraceMessage.validPayload);
                                // Trigger the webhook.
                                TriggerWebhook(context, requestName);
                                trace.Trace(TraceMessage.webhookTriggered);
                            }
                        }
                    }
                    catch (FaultException<OrganizationServiceFault> ex)
                    {
                        throw new InvalidPluginExecutionException(TraceMessage.pluginFaultException, ex);
                    }
                    catch (Exception ex)
                    {
                        trace.Trace(TraceMessage.pluginError, ex.ToString());
                        throw;
                    }
                }
            }
        }

        private bool CheckConditions(IPluginExecutionContext context, string requestName)
        {
            try
            {
                try
                {
                    Entity entity = (Entity)context.InputParameters["Target"];
                    var model = PayloadTransform(context);
                    if (context.PrimaryEntityName == EntityName.account.ToString())
                    {
                        if (Convert.ToString(model["name"]) == "KeyBank")
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
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
                    HttpResponseMessage response = client.PostAsync(Constant.webhookUrl, content).Result;

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

        private Dictionary<string, object> PayloadTransform(IPluginExecutionContext model)
        {

            var payload = new Dictionary<string, object>();
            var data = model.InputParameters.Values;
           
                foreach(Entity item in data)
                {
                    foreach(var element in item.Attributes.Values.Select((value, i) => new { i, value }))
                    {
                        var actualValue = element.i != null ? element.value : "";
                        payload[item.Attributes.Keys.ElementAt(element.i)] = element.value is object ? element.value : "";
                    }
                   
                    //element.value = element[item] != null ? element.value : "";
                    //payload[element.key] = element.value is object
                    //    ? element.value.id != null
                    //        ? element.value.id
                    //        : element.value.value
                    //    : element.value;
                

                }
            
            return payload;
        }

    }
}
