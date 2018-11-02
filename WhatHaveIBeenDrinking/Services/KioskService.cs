using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatHaveIBeenDrinking.Repositories;
using Windows.Graphics.Imaging;

using Newtonsoft.Json;

namespace WhatHaveIBeenDrinking.Services
{
    public class KioskService
    {
        private ImageClassificationService _ClassificationService;
        private FaceIdentificationService _FaceService;
        private KioskRepository _KioskRepository;

        public KioskService(
            ImageClassificationService classificationService, 
            FaceIdentificationService faceService,
            KioskRepository repository
        )
        {
            _ClassificationService = classificationService;
            _FaceService = faceService;
            _KioskRepository = repository;
        }

        public async Task<DrinkIdentificationResult> IdentifyDrink(SoftwareBitmap bitmap)
        {
            var classificationResult = await _ClassificationService.ClassifyImage(bitmap);

            if (classificationResult.Probability < .25)
            {
                return null;
            }

            var displayData = _KioskRepository.GetItemByTag(classificationResult.Tag);
            
            return new DrinkIdentificationResult
            {
                Name = displayData?.Name,
                Description = displayData?.Description,
                FoodPairing = displayData?.FoodPairing,
                ImageUrl = displayData?.ImageUrl,
                Probability = classificationResult.Probability,
                IdentifiedTag = classificationResult.Tag
            };
        }

        public async Task<FaceIdentificationResult> IdentifyFace(SoftwareBitmap bitmap) {

            var face = await _FaceService.AnalyzeImage(bitmap);

            return new FaceIdentificationResult {
                FaceId = face.FaceId,
                Emotion = JsonConvert.DeserializeObject<Emotion>(JsonConvert.SerializeObject(face.FaceAttributes.Emotion)),
                Age = face.FaceAttributes.Age,
                Gender = JsonConvert.DeserializeObject<Gender>(JsonConvert.SerializeObject(face.FaceAttributes.Gender)),
                Smile = face.FaceAttributes.Smile
            };
        }
    }
}
