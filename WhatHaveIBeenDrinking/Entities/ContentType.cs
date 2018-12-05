using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace WhatHaveIBeenDrinking.Entities
{
    static class ContentType
    {
        public const string TextContent = "Text";
        public const string VideoContent = "Video";

        public static List<string> AllContent = new List<string>()
        {
            TextContent,
            VideoContent
        };
    }
}
