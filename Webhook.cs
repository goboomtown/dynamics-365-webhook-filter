using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel;

namespace OvationCXMFilter.Plugins
{

    /// <summary>
    /// Declare entity name enum
    /// </summary>
    public class EntityModel
    {
        public string Customer { get; set; }
        public string Contact { get; set; }
        public string Case { get; set; }
    }

    /// <summary>
    /// Declare entity event enum
    /// </summary>
    public enum RequestName
    {
        create,
        update
    }

    /// <summary>
    /// Declare required constants
    /// </summary>
    public static class Constant
    {
        public static EntityModel entities = new EntityModel { Customer = "account", Contact = "contact", Case = "incident" };

        /* 
         * To test purpose replace this webhookUrl with test url as https://webhook.site/ 
         * Replace {{orgId}} with actual orgId before deploy on production
         */
        public static string webhookUrl = "https://connector-msdynamics-api-pwgavvro5q-uc.a.run.app/api/webhook/msdynamics?orgId={{orgId}}";
    }

    /// <summary>
    /// Error messages for ITracingService service
    /// </summary>
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
            if ((requestName.Equals(RequestName.create.ToString()) || requestName.Equals(RequestName.update.ToString())) && context.Stage == 40) // Post-operation stage
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

        /// <summary>
        /// Implement filteration logic to trigger webhook
        /// </summary>
        /// <param name="context">Execution context from the service provider.</param>
        /// <param name="service">Instance of IOrganizationService</param>
        /// <returns>Return true/false based on filter conditions</returns>
        /// <exception cref="InvalidPluginExecutionException">Throw exception on filter conditions failure</exception>
        public bool CheckConditions(IPluginExecutionContext context, IOrganizationService service)
        {
            try
            {

                // ## You can add filter conditions over here sample is shown below

                //string requestName = context.MessageName.ToLower();
                //Entity entity = (Entity)context.InputParameters["Target"];
                //string entityName = entity.LogicalName;
                //var model = PayloadTransform(context);

                //if (entityName == Constant.entities.Case)
                //{
                //    string accountId = String.Empty;
                //    if (RequestName.update.ToString() == requestName)
                //    {
                //        Entity caseId = (Entity)context.InputParameters["Target"];

                //        // Retrieving the customer id of the case using the case id
                //        Entity caseRecord = service.Retrieve(entityName, caseId.Id, new ColumnSet("customerid"));

                //        EntityReference customerRef = caseRecord.GetAttributeValue<EntityReference>("customerid");
                //        accountId = customerRef.Id.ToString();
                //    }

                //    if (RequestName.create.ToString() == requestName)
                //    {
                //        // Extracting the customer ID from the provided model data.  
                //        EntityReference customerId = (EntityReference)model["customerid"];
                //        accountId = customerId.Id.ToString();
                //    }

                //    if (accountId == "6f82d1a5-a8c5-ee11-9079-00224827244c")
                //    {
                //        return true;
                //    }
                //}

                return false;
            }
            catch (Exception ex)
            {
                // Handle exceptions.
                throw new InvalidPluginExecutionException($"Filter error: {ex.Message}", ex);
            }
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
                    var payload = JsonConvert.SerializeObject(entity);

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

        /// <summary>
        /// Convert IPluginExecutionContext to readable object
        /// </summary>
        /// <param name="model">IPluginExecutionContext object</param>
        /// <returns>Returns the entity object</returns>
        private Dictionary<string, object> PayloadTransform(IPluginExecutionContext model)
        {
            var payload = new Dictionary<string, object>();

            // Retrieving the first entity object from the input parameters of the model,
            // assuming the model is holding a collection of entities.
            Entity data = (Entity)model.InputParameters.Values.FirstOrDefault();
            if (data != null)
            {
                foreach (var element in data.Attributes.Values.Select((value, i) => new { i, value }))
                {
                    var actualValue = element.i != null ? element.value : "";
                    payload[data.Attributes.Keys.ElementAt(element.i)] = element.value is object ? element.value : "";
                }
            }

            return payload;
        }

    }
}
