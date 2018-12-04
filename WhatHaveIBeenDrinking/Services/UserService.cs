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
using Microsoft.WindowsAzure.Storage;

namespace WhatHaveIBeenDrinking.Services {

    public interface IUserService {

        Task<Models.User> IdentifyUserAsync(SoftwareBitmap bitmap, Guid correlationId);
    }

    public class UserService : IUserService {

        private readonly IFaceClient FaceClient;

        public UserService(IFaceClient client) {
            this.FaceClient = client;
        }

        public async Task<Models.User> IdentifyUserAsync(SoftwareBitmap bitmap, Guid correlationId) {

            using (var stream = new InMemoryRandomAccessStream()) {

                // Get the stream for usage
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.SetSoftwareBitmap(bitmap);
                await encoder.FlushAsync();

                // TODO: This probably isn't right
                var backupStream = stream.CloneStream();
                var imageStream = stream.CloneStream();

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

                    // Create the new User to be returned with all of the information
                    user = new Models.User {
                        //Name = detectedIdentity.PersonId.ToString() ?? "Beer Lover",
                        DetectedFaceId = face.FaceId,
                        //IdentifiedFaceId = detectedIdentities?.FirstOrDefault()?.FaceId,    // Do we really need this?
                        //IdentifiedPersonId = detectedIdentity.PersonId,
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

                    IList<IdentifyResult> detectedIdentities = await this.FaceClient.Face.IdentifyAsync(new List<Guid>() { face.FaceId.Value }, largePersonGroupId: "kioskusers");  // TODO: PersonGroupId is hard-coded

                    var detectedIdentity = detectedIdentities.FirstOrDefault().Candidates.Where(c => c.Confidence > .50).FirstOrDefault();

                    if (detectedIdentity != null) {

                        // Add the information to the User
                        user.Name = detectedIdentity.PersonId.ToString() ?? "Beer Lover";
                        user.IdentifiedFaceId = detectedIdentities?.FirstOrDefault()?.FaceId;    // Do we really need this?
                        user.IdentifiedPersonId = detectedIdentity.PersonId;
                    }
                    else {

                        // Create a new person and save the image for the face
                        var newPerson = await this.FaceClient.LargePersonGroupPerson.CreateAsync("kioskusers", "Beer Lover", $"Created automatically by the kiosk from CorrelationId '{correlationId}'.");
                        var newFace = await this.FaceClient.LargePersonGroupPerson.AddFaceFromStreamAsync("kioskusers", newPerson.PersonId, backupStream.AsStream());

                        // Add the information to the User
                        user.Name = "Beer Lover";
                        user.IdentifiedPersonId = newPerson.PersonId;
                        user.IdentifiedFaceId = newFace.PersistedFaceId;    // Do we really need this?

                        // Make sure to train the model - this probably needs to be done differently at some point
                        await this.FaceClient.LargePersonGroup.TrainAsync("kioskusers");
                    }
                }

                return user;
            }
        }
    }
}
