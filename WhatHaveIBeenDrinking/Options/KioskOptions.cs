using System;

namespace WhatHaveIBeenDrinking.Options
{
    /// <summary>
    /// Options for the Kiosk Demo
    /// </summary>
    public class KioskOptions
    {
        public string CognitiveServicesCustomVisionApiKey { get; set; }

        public Guid CognitiveServicesCustomVisionProjectId { get; set; }

        public Guid CognitiveServicesCustomVisionIterationId { get; set; }

        public string AzureCosmosDbEndpoint { get; set; }

        public string AzureCosmosDbKey { get; set; }

        public string AzureCosmosDBName { get; set; }

        public string AzureCosmosDBCollectionName { get; set; }
    }
}
