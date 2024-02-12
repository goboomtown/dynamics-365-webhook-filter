
namespace OvationCXMFilter.Constants
{
    public static class Constant
    {
        public static EntityModel entities = new EntityModel { Customer = "account", Contact = "contact", Case = "incident" };

        /* 
         * To test purpose replace this webhookUrl with test url as https://webhook.site/ 
         * Replace {{orgId}} with actual orgId before deploy on production
         */
        public static string webhookUrl = "https://connector-msdynamics-api-pwgavvro5q-uc.a.run.app/api/webhook/msdynamics?orgId={{orgId}}";
    }
    
}
