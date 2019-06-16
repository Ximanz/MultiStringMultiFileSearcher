using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MultiStringMultiFileSearcher
{
    public class AppConfig
    {
        public AppConfig(IConfiguration configuration = null)
        {
            configuration?.GetSection(Key)?.Bind(this);
        }

        public const string Key = "AppConfig";

        public string SearchDirectory { get; set; }

        public string[] ExcludedFolders { get; set; }

        public string[] FileExtensions { get; set; }

        public string InputFile { get; set; }

        public bool Validate(ILogger logger)
        {
            if (!File.Exists(InputFile))
            {
                logger.LogError($"Input file {InputFile} does not exist.");
                return false;
            }

            if (!Directory.Exists(SearchDirectory))
            {
                logger.LogError($"Search directory {SearchDirectory} does not exist.");
                return false;
            }

            return true;
        }
    }
}
