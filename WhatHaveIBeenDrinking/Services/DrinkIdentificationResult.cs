using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatHaveIBeenDrinking.Entities;

namespace WhatHaveIBeenDrinking.Services
{
    public class DrinkIdentificationResult
    {
        public string IdentifiedTag;

        public double Probability;

        public Drink Drink;
    }
}
