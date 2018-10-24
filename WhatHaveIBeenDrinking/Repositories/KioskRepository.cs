using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using WhatHaveIBeenDrinking.Options;
using WhatHaveIBeenDrinking.Entities;

namespace WhatHaveIBeenDrinking.Repositories
{
    public class KioskRepository
    {
        private KioskOptions Config { get; }

        private DocumentClient _Client;

        private Uri _CollectionUri;


        // Memoized CosmosDB DocumentClient
        private DocumentClient Client
        {
            get
            {
                if (_Client != null)
                {
                    return _Client;
                }

                var serializerSettings = new JsonSerializerSettings()
                {
                    // Enforce camel instead of pascal case to make objects compatible with the cosmos document ids
                    ContractResolver = new DefaultContractResolver()
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                };

                _Client = new DocumentClient(
                    new Uri(Config.AzureCosmosDbEndpoint),
                    Config.AzureCosmosDbKey,
                    serializerSettings: serializerSettings
                );

                return _Client;
            }
        }

        public KioskRepository(IOptions<KioskOptions> configuration)
        {
            Config = configuration.Value;

            _CollectionUri = UriFactory.CreateDocumentCollectionUri(
                Config.AzureCosmosDBName,
                Config.AzureCosmosDBCollectionName
            );
        }

        public Item GetItemByTag(string tag)
        {
            var feedOptions = new FeedOptions { EnableCrossPartitionQuery = true };

            var result = Client.CreateDocumentQuery<Item>(_CollectionUri, feedOptions)
                .Where(a => a.Tag == tag)
                .ToList()
                .FirstOrDefault();

            return result;
        }
    }
}
