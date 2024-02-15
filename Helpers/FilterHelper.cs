using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using OvationCXMFilter.Enums;
using System;
using static OvationCXMFilter.Plugins.Webhook;

namespace OvationCXMFilter.Helpers
{
    /// <summary>
    /// Implement filteration logic to trigger webhook
    /// </summary>
    /// <param name="context">Execution context from the service provider.</param>
    /// <param name="service">Instance of IOrganizationService</param>
    /// <returns>Return true/false based on filter conditions</returns>
    /// <exception cref="InvalidPluginExecutionException">Throw exception on filter conditions failure</exception>
    public class FilterHelper
    {
        public bool IsValid(IPluginExecutionContext context, IOrganizationService service)
        {
            try
            {
                string requestName = context.MessageName.ToLower();
                Entity entity = (Entity)context.InputParameters["Target"];
                string entityName = entity.LogicalName;
                var model = PayloadHelper.PayloadTransform(context);

                // Sample Filter conditions added for case entity to filter data for a specific account
                //if (entityName == Constant.entities.Case)
                //{
                //    string accountId = String.Empty;
                //    // Case create envent, whole payload will be get in context to filter
                //    if (EventType.RequestName.create.ToString() == requestName)
                //    {
                //        // Extracting the customer ID from the provided model data.  
                //        EntityReference customerId = (EntityReference)model["customerid"]; // In create event account id will be get an customerid in context
                //        accountId = customerId.Id.ToString();
                //    }

                //    // Case update envent, only updated filters will be get in context to filter
                //    if (EventType.RequestName.update.ToString() == requestName)
                //    {
                //        Entity caseId = (Entity)context.InputParameters["Target"];

                //        // Retrieving the customer id of the case using the case id
                //        Entity caseRecord = service.Retrieve(entityName, caseId.Id, new ColumnSet("customerid")); // In update event, account id will not be a part of context so we need to fetch customerid

                //        EntityReference customerRef = caseRecord.GetAttributeValue<EntityReference>("customerid");
                //        accountId = customerRef.Id.ToString();
                //    }

                //    // Static account id should be replaced as per uses
                //    if (accountId == "2c762a57-ddcb-ee11-9079-00224827244c")
                //    {
                //        return true;
                //    }
                //}

                return true;
            }
            catch (Exception ex)
            {
                // Handle exceptions.
                throw new InvalidPluginExecutionException($"Filter error: {ex.Message}", ex);
            }
        }
    }
}