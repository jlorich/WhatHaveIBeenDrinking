using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace WhatHaveIBeenDrinking.Services
{
    public class FaceIdentificationService
    {
        private readonly IFaceClient FaceClient;

        public FaceIdentificationService(IFaceClient client) 
        {
            this.FaceClient = client;
        }

        public async Task<DetectedFace> AnalyzeImage(SoftwareBitmap bitmap)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                encoder.SetSoftwareBitmap(bitmap);

                await encoder.FlushAsync();

                var attributes = new List<FaceAttributeType> {
                    //FaceAttributeType.Accessories,
                    FaceAttributeType.Age,
                    //FaceAttributeType.Blur,
                    FaceAttributeType.Emotion,
                    //FaceAttributeType.Exposure,
                    //FaceAttributeType.FacialHair,
                    FaceAttributeType.Gender,
                    //FaceAttributeType.Glasses,
                    //FaceAttributeType.Hair,
                    //FaceAttributeType.HeadPose,
                    //FaceAttributeType.Makeup,
                    //FaceAttributeType.Noise,
                    //FaceAttributeType.Occlusion,
                    FaceAttributeType.Smile
                };

                var detectedFaces = await this.FaceClient.Face.DetectWithStreamAsync(stream.AsStream(), true, false, attributes);

                var topFace = detectedFaces?[0];

                return topFace;
            }
        }
    }
}
