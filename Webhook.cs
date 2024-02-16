using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.ServiceModel;
using OvationCXMFilter.Constants;
using OvationCXMFilter.Helpers;
using OvationCXMFilter.Enums;
using OvationCXMFilter.Models;

namespace OvationCXMFilter.Plugins
{
    public class Webhook : IPlugin
    {
        /// <summary>
        /// Declare supported entity and webhook url
        /// </summary>
        public static class Constant
        {
            public static EntityModel entities = new EntityModel { Customer = "account", Contact = "contact", Case = "incident", CaseLog = "annotation" };
            /* 
             * Replace {{instanceId}} with actual Instance Id before deploy on production
             */
            public static string webhookUrl = "https://api.ovationcxm.market/v1/msdynamics/webhook/msdynamics/{{instanceId}}";
        }

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
                                var filter = new FilterHelper();
                                if (filter.IsValid(context, service))
                                {
                                    trace.Trace(TraceMessage.validPayload);
                                    // Trigger the webhook.
                                    var webhook = new TriggerWebhook();
                                    webhook.Webhook(context, trace, Constant.webhookUrl);
                                    trace.Trace(TraceMessage.webhookTriggered);
                                }
                            }
                            else
                            {
                                trace.Trace(TraceMessage.userNotFound);
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
    }
}
