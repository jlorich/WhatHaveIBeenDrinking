using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WhatHaveIBeenDrinking.Models {

    public class User {

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "detectedFaceId")]
        public Guid? DetectedFaceId { get; set; }

        [JsonProperty(PropertyName = "identifiedFaceId")]
        public Guid? IdentifiedFaceId { get; set; }

        [JsonProperty(PropertyName = "identifiedPersonId")]
        public Guid? IdentifiedPersonId { get; set; }

        // Gets or sets age in years
        [JsonProperty(PropertyName = "age")]
        public double? Age { get; set; }

        // Gets or sets possible gender of the face. Possible values include: 'male', 'female', 'genderless'
        [JsonProperty(PropertyName = "gender")]
        public Gender? Gender { get; set; }

        // Gets or sets smile intensity, a number between [0,1]
        [JsonProperty(PropertyName = "smile")]
        public double? Smile { get; set; }

        [JsonProperty(PropertyName = "anger")]
        public double? Anger { get; set; }

        [JsonProperty(PropertyName = "contempt")]
        public double? Contempt { get; set; }

        [JsonProperty(PropertyName = "disgust")]
        public double? Disgust { get; set; }

        [JsonProperty(PropertyName = "fear")]
        public double? Fear { get; set; }

        [JsonProperty(PropertyName = "happiness")]
        public double? Happiness { get; set; }

        [JsonProperty(PropertyName = "neutral")]
        public double? Neutral { get; set; }

        [JsonProperty(PropertyName = "sadness")]
        public double? Sadness { get; set; }

        [JsonProperty(PropertyName = "surprise")]
        public double? Surprise { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Gender {
        Male = 0,
        Female = 1,
        Genderless = 2
    }
}
