using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OvationCXMFilter.Constants
{
    public static class TraceMessage
    {
        public static string validationStart = "Validating filter conditions...";
        public static string validPayload = "Payload validated and triggering webhook...";
        public static string webhookTriggered = "Webhook triggered !!!";
        public static string pluginFaultException = "An error occurred in OvationCXM Plugin.";
        public static string pluginError = "OvationCXM Plugin Exception: {0}";
    }
}
