using Microsoft.Xrm.Sdk;
using System.Collections.Generic;
using System.Linq;

namespace OvationCXMFilter.Helpers
{
    /// <summary>
    /// Convert IPluginExecutionContext to readable object
    /// </summary>
    /// <param name="model">IPluginExecutionContext object</param>
    /// <returns>Returns the entity object</returns>
    public static class PayloadHelper
    {
        public static Dictionary<string, object> PayloadTransform(IPluginExecutionContext model)
        {
            var payload = new Dictionary<string, object>();
            // Retrieving the first entity object from the input parameters of the model,
            // assuming the model is holding a collection of entities.
            Entity data = (Entity)model.InputParameters.Values.FirstOrDefault();
            if (data != null)
            {
                foreach (var element in data.Attributes.Values.Select((value, i) => new { i, value }))
                {
                    payload[data.Attributes.Keys.ElementAt(element.i)] = element.value is object ? element.value : "";
                }
            }

            return payload;
        }
    }
}
