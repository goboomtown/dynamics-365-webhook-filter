using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;
using OvationCXMFilter.Constants;
using OvationCXMFilter.Enums;

namespace OvationCXMFilter.Plugins
{
    public class Webhook : IPlugin
    {
        /// <summary>
        /// Execute the plugin on the basic on entity event
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <exception cref="InvalidPluginExecutionException"></exception>
        public void Execute(IServiceProvider serviceProvider)
        {

            ITracingService trace = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Retrieve the service factory responsible for creating instances of IOrganizationService.
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            // Create an instance of IOrganizationService using the retrieved service factory and the user context.
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // Check if the plugin is executing in the post-operation stage of the "Create" or "Update" message.
            string requestName = context.MessageName.ToLower();
            if ((requestName.Equals(EventType.RequestName.create.ToString()) || requestName.Equals(EventType.RequestName.update.ToString())) && context.Stage == 40) // Post-operation stage
            {
                // Ensure the target entity is present in the context.
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    // Get the target entity from the context.
                    Entity entity = (Entity)context.InputParameters["Target"];
                    try
                    {
                        string[] crmEntities = { Constant.entities.Customer, Constant.entities.Contact, Constant.entities.Case };

                        // Check if the entity is of the desired type (replace "contact" with your actual entity logical name).
                        if (crmEntities.Contains(entity.LogicalName))
                        {
                            trace.Trace(TraceMessage.validationStart);
                            if (context.UserId != null)
                            {
                                // Checking if the logical name of the entity is equal to the third element to perform the certain steps
                                if (CheckConditions(context, service))
                                {
                                    trace.Trace(TraceMessage.validPayload);

                                    // Trigger the webhook.
                                    TriggerWebhook(context, trace);

                                    trace.Trace(TraceMessage.webhookTriggered);
                                }
                            }
                            else
                            {
                                trace.Trace("User not found");
                            }
                        }
                    }
                    catch (FaultException<OrganizationServiceFault> ex)
                    {
                        trace.Trace(TraceMessage.pluginFaultException, ex.ToString());
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

        public bool CheckConditions(IPluginExecutionContext context, IOrganizationService service)
        {
            var filterInstance = new OvationCXMFilter.FilterHelper();
            return filterInstance.IsValid(context, service);
        }

        /// <summary>
        /// Trigger a OvationCXM webhook 
        /// </summary>
        /// <param name="context">Execution context from the service provider.</param>
        /// <param name="trace">ITracingService object</param>
        /// <exception cref="InvalidPluginExecutionException">Throw exception on webhook trigger failure</exception>
        private void TriggerWebhook(IPluginExecutionContext context, ITracingService trace)
        {
            try
            {
                string requestName = context.MessageName.ToLower();
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
