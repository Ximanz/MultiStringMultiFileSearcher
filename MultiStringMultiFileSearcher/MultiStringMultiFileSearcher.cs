using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using MultiRegexSearcher;
using Neo4jClient;

namespace MultiStringMultiFileSearcher
{
    public class MultiStringMultiFileSearcher
    {
        private AppConfig _config;
        private readonly ILogger<MultiStringMultiFileSearcher> _logger;

        public MultiStringMultiFileSearcher(ILogger<MultiStringMultiFileSearcher> logger, AppConfig config)
        {
            _config = config;
            _logger = logger;
        }

        public void Execute()
        {
            try
            {
                if (!_config.Validate(_logger)) return;

                var outputFileName = Path.Combine(Path.GetDirectoryName(_config.InputFile),
                    $"results-{DateTime.Now:s}.csv");

                var searchStrings = File.ReadAllLines(_config.InputFile);
                var DFA = Utility.BuildDFA(searchStrings);

                var rootDirectory = new DirectoryInfo(_config.SearchDirectory);

                var searchDirectories = rootDirectory
                    .EnumerateDirectories("*", SearchOption.AllDirectories)
                    .Where(d => !_config.ExcludedFolders.Contains(d.Name));

                var searchFiles = searchDirectories
                    .Select(d => d
                        .EnumerateFiles("*.*", SearchOption.AllDirectories)
                        .Where(f => !_config.FileExtensions.Any() || _config.FileExtensions.Contains(f.Extension)))
                    .Aggregate((a, b) => a.Concat(b));


                using (var outputFile = new StreamWriter(outputFileName, false))
                {
                    _logger.LogInformation($"Writing search results to {outputFileName}.");

                    outputFile.WriteLine("Search Result,File Name,Line Number,Text");

                    foreach (var file in searchFiles)
                    {
                        _logger.LogInformation($"Searching {file.FullName}.");

                        var lines = File.ReadAllLines(file.FullName);

                        for (var i = 0; i < lines.Length; i++)
                        {
                            var matches = DFA.FindMatches(lines[i]);

                            foreach (var match in matches)
                            {
                                outputFile.WriteLine($"\"{match}\",\"{file.FullName}\",\"{i + 1}\",\"{lines[i]}\"");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"An unexpected error has occurred.");
                return;
            }
        }
    }
}
