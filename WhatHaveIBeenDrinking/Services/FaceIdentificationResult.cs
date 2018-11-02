using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WhatHaveIBeenDrinking.Services {

    public class FaceIdentificationResult {

        public FaceIdentificationResult() {
            this.Emotion = new Emotion();
        }

        [JsonProperty(PropertyName = "faceId")]
        public Guid? FaceId { get; set; }
        // Summary:
        //     Gets or sets age in years
        public double? Age { get; set; }

        // Summary:
        //     Gets or sets possible gender of the face. Possible values include: 'male', 'female',
        //     'genderless'
        [JsonProperty(PropertyName = "gender")]
        public Gender? Gender { get; set; }

        // Summary:
        //     Gets or sets properties describing facial emotion in form of confidence ranging
        //     from 0 to 1.
        [JsonProperty(PropertyName = "emotion")]
        public Emotion Emotion { get; set; }

        // Summary:
        //     Gets or sets smile intensity, a number between [0,1]
        [JsonProperty(PropertyName = "smile")]
        public double? Smile { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Gender {
        Male = 0,
        Female = 1,
        Genderless = 2
    }

    public class Emotion {

        [JsonProperty(PropertyName = "anger")]
        public double Anger { get; set; }
        
        [JsonProperty(PropertyName = "contempt")]
        public double Contempt { get; set; }
        
        [JsonProperty(PropertyName = "disgust")]
        public double Disgust { get; set; }
        
        [JsonProperty(PropertyName = "fear")]
        public double Fear { get; set; }
        
        [JsonProperty(PropertyName = "happiness")]
        public double Happiness { get; set; }
        
        [JsonProperty(PropertyName = "neutral")]
        public double Neutral { get; set; }
        
        [JsonProperty(PropertyName = "sadness")]
        public double Sadness { get; set; }
        
        [JsonProperty(PropertyName = "surprise")]
        public double Surprise { get; set; }
    }
}
