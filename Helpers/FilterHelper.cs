using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using OvationCXMFilter.Constants;
using OvationCXMFilter.Enums;
using OvationCXMFilter.Helpers;
using System;

namespace OvationCXMFilter
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

                if (entityName == Constant.entities.Case)
                {
                    string accountId = String.Empty;
                    if (EventType.RequestName.update.ToString() == requestName)
                    {
                        Entity caseId = (Entity)context.InputParameters["Target"];

                        // Retrieving the customer id of the case using the case id
                        Entity caseRecord = service.Retrieve(entityName, caseId.Id, new ColumnSet("customerid"));

                        EntityReference customerRef = caseRecord.GetAttributeValue<EntityReference>("customerid");
                        accountId = customerRef.Id.ToString();
                    }

                    if (EventType.RequestName.create.ToString() == requestName)
                    {
                        // Extracting the customer ID from the provided model data.  
                        EntityReference customerId = (EntityReference)model["customerid"];
                        accountId = customerId.Id.ToString();
                    }

                    if (accountId == "6f82d1a5-a8c5-ee11-9079-00224827244c")
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                // Handle exceptions.
                throw new InvalidPluginExecutionException($"Filter error: {ex.Message}", ex);
            }
        }
    }

}
