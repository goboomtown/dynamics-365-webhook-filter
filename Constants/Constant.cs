namespace OvationCXMFilter.Constants
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
}
