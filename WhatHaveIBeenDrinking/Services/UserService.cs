using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

using Newtonsoft.Json;

using Models = WhatHaveIBeenDrinking.Models;

namespace WhatHaveIBeenDrinking.Services {

    public interface IUserService {

        Task<Models.User> IdentifyUserAsync(SoftwareBitmap bitmap);
    }

    public class UserService : IUserService {

        private readonly IFaceClient FaceClient;

        public UserService(IFaceClient client) {
            this.FaceClient = client;
        }

        public async Task<Models.User> IdentifyUserAsync(SoftwareBitmap bitmap) {

            using (var stream = new InMemoryRandomAccessStream()) {

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

                // TODO: Everything below this is slammed together and probably needs some overhaul and better checking
                // TODO: Make sure this logic is right and that the confidence threshold is high enough

                // Detect the Faces
                Models.User user = null;

                var detectedFaces = await this.FaceClient.Face.DetectWithStreamAsync(stream.AsStream(), true, false, attributes);

                if (detectedFaces?.Count > 0) {

                    var face = detectedFaces.First();
                    IList<IdentifyResult> detectedIdentity = await this.FaceClient.Face.IdentifyAsync(new List<Guid>() { face.FaceId.Value }, "kioskusers");  // TODO: PersonGroupId is hard-coded

                    // Create the new User to be returned with all of the information
                    user = new Models.User {
                        Name = "Beer Lover", // detectedIdentity?.FirstOrDefault()?.Candidates?.FirstOrDefault()?.PersonId.ToString() ?? "Beer Lover",
                        DetectedFaceId = face.FaceId,
                        IdentifiedFaceId = detectedIdentity?.FirstOrDefault()?.FaceId,
                        IdentifiedPersonId = detectedIdentity?.FirstOrDefault()?.Candidates?.FirstOrDefault().PersonId,
                        Age = face.FaceAttributes.Age,
                        Gender = JsonConvert.DeserializeObject<Models.Gender>(JsonConvert.SerializeObject(face.FaceAttributes.Gender)),
                        Smile = face.FaceAttributes.Smile,
                        Anger = face.FaceAttributes.Emotion.Anger,
                        Contempt = face.FaceAttributes.Emotion.Contempt,
                        Disgust = face.FaceAttributes.Emotion.Disgust,
                        Fear = face.FaceAttributes.Emotion.Fear,
                        Happiness = face.FaceAttributes.Emotion.Happiness,
                        Neutral = face.FaceAttributes.Emotion.Neutral,
                        Sadness = face.FaceAttributes.Emotion.Sadness,
                        Surprise = face.FaceAttributes.Emotion.Surprise,
                    };
                }

                return user;
            }
        }
    }
}
