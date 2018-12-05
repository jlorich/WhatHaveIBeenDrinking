using System;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace WhatHaveIBeenDrinking.Entities
{
    public class Content
    {
        public string Type;

        public string Title;

        public string Url;

        public string Description;

        public string IconType;
    }
}
