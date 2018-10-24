using System;

namespace KioskApi.Options
{
    /// <summary>
    /// Options for the Kiosk Demo
    /// </summary>
    public class KioskOptions
    {
        public string AzureCosmosDbEndpoint { get; set; }

        public string AzureCosmosDbKey { get; set; }

        public string AzureCosmosDBName { get; set; }

        public string AzureCosmosDBCollectionName { get; set; }
    }
}
