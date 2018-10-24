using System;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace WhatHaveIBeenDrinking.Entities
{
    public class Item : DocumentEntity
    {
        // For some reason the default resolver isnt working for queries, using this to fix it for now
        [JsonProperty("tag")]

        public string Tag;

        public string Name;

        public string Description;

        public string FoodPairing;

        public string ImageUrl;
    }
}
