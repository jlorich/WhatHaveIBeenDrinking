using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatHaveIBeenDrinking.Repositories;
using Windows.Graphics.Imaging;

namespace WhatHaveIBeenDrinking.Services
{
    public class KioskService
    {
        ImageClassificationService _ClassificationService;
        KioskRepository _KioskRepository;

        public KioskService(ImageClassificationService classificationService, KioskRepository repository)
        {
            _ClassificationService = classificationService;
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
    }
}
