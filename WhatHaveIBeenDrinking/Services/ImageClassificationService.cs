using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Prediction.Models;
using Microsoft.Extensions.Options;
using WhatHaveIBeenDrinking.Options;
using WhatHaveIBeenDrinking.Repositories;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace WhatHaveIBeenDrinking.Services
{
    public class ImageClassificationService
    {
        KioskOptions _Configuration;

        public ImageClassificationService(IOptions<KioskOptions> configuration) {
            _Configuration = configuration.Value;
        }

        public async Task<ImageClassificationResult> ClassifyImage(SoftwareBitmap bitmap)
        {
            var endpoint = new PredictionEndpoint()
            {
                ApiKey = _Configuration.CognitiveServicesCustomVisionApiKey
            };

            using (var stream = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                encoder.SetSoftwareBitmap(bitmap);

                await encoder.FlushAsync();

                var result = endpoint.PredictImage(
                    _Configuration.CognitiveServicesCustomVisionProjectId,
                    stream.AsStream()
                );

                double highestProbability = 0;
                ImageTagPredictionModel predictedModel = null;

                foreach (var prediction in result.Predictions)
                {
                    if (prediction.Probability > highestProbability)
                    {
                        highestProbability = prediction.Probability;
                        predictedModel = prediction;
                    }
                }

                return new ImageClassificationResult()
                {
                    Tag = predictedModel.Tag,
                    Probability = predictedModel.Probability
                };
            }
        }
    }
}
