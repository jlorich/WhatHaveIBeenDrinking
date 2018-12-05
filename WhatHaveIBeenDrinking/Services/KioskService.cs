using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatHaveIBeenDrinking.Repositories;
using Windows.Graphics.Imaging;

using Newtonsoft.Json;
using WhatHaveIBeenDrinking.Entities;

namespace WhatHaveIBeenDrinking.Services
{
    public class KioskService
    {
        private ImageClassificationService _ClassificationService;
        private KioskRepository _KioskRepository;

        public KioskService(
            ImageClassificationService classificationService, 
            KioskRepository repository
        )
        {
            _ClassificationService = classificationService;
            _KioskRepository = repository;
        }

        public async Task<DrinkIdentificationResult> IdentifyDrink(SoftwareBitmap bitmap)
        {
            var classificationResult = await _ClassificationService.ClassifyImage(bitmap);

            if (classificationResult == null || classificationResult.Probability < .25)
            {
                return null;
            }

            var drink = _KioskRepository.GetDrinkByTag(classificationResult.Tag);
            
            return new DrinkIdentificationResult
            {
                Drink = drink,
                Probability = classificationResult.Probability,
                IdentifiedTag = classificationResult.Tag
            };
        }
    }
}
