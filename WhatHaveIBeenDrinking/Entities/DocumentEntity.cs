using System;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace WhatHaveIBeenDrinking.Entities
{
    public abstract class DocumentEntity
    {
        [JsonProperty("$type")]
        public string EntityType;

        public string Id;
    }
}
