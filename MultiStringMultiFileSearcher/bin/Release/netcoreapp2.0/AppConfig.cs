using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace GenerateGraphForStreams
{
    public class AppConfig
    {
        public AppConfig(IConfiguration configuration = null)
        {
            configuration?.GetSection(Key)?.Bind(this);
        }

        public const string Key = "AppConfig";

        public string GraphDbUrl { get; set; }

        public string GraphDbUsername { get; set; }

        public string GraphDbPassword { get; set; }

        public string RootDirectory { get; set; }

        public bool SearchDbObjects { get; set; }

        public bool SearchWebServices { get; set; }

        public bool SearchSoftwareProjects { get; set; }

        public bool SearchHistoricalServer { get; set; }    

        public bool SearchBi { get; set; }
    }
}
