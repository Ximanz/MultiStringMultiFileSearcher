using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MultiRegexSearcher;
using Xunit;

namespace Tests
{
    public class DFATests
    {
        [Fact]
        public void TestFindMatches()
        {
            List<Regex> regexes = new List<Regex>();

            regexes.Add(new Regex("cats"));
            regexes.Add(new Regex("dogs"));

            FSM NFA = new FSM();
            regexes.ForEach(regex => NFA |= FSM.Parse(regex.ToString()));
            FSM DFA = NFAToDFA.ConvertNFAToDFA(NFA);

            var testString1 = "the cat is alive";
            var testString2 = "those cats are feral";
            var testString3 = "the truth about cats and dogs";
            var testString4 = "a string \\W separation characters like cats.dogs works";
            var testString5 = "a string with no separation like catsdogs will depend on the parameters used for matching";

            var matches1 = DFA.FindMatches(testString1).ToList();
            var matches2 = DFA.FindMatches(testString2).ToList();
            var matches3 = DFA.FindMatches(testString3).ToList();
            var matches4 = DFA.FindMatches(testString4).ToList();
            var matches5 = DFA.FindMatches(testString5).ToList();
            var matches6 = DFA.FindMatches(testString5, checkStarting: true).ToList();
            var matches7 = DFA.FindMatches(testString5, checkEnding: true).ToList();
            var matches8 = DFA.FindMatches(testString5, checkStarting: true, checkEnding: true).ToList();

            Assert.Empty(matches1);
            Assert.Single(matches2);
            Assert.Equal("cats", matches2.First());
            Assert.Equal(2, matches3.Count);
            Assert.Contains("cats", matches3);
            Assert.Contains("dogs", matches3);
            Assert.Equal(2, matches4.Count);
            Assert.Contains("cats", matches4);
            Assert.Contains("dogs", matches4);
            Assert.Equal(2, matches5.Count);
            Assert.Contains("cats", matches5);
            Assert.Single(matches6);
            Assert.Contains("cats", matches6);
            Assert.Single(matches7);
            Assert.Contains("dogs", matches7);
            Assert.Empty(matches8);
        }

        [Fact]
        public void TestBuildDFACaseInsensitive()
        {
            var totalSize = 100;
            var batchSize = 10;
            var testStringLength = 8;

            var testStrings = new HashSet<string>();

            while (testStrings.Count < totalSize)
            {
                testStrings.Add(GenerateRandomString(testStringLength));
            }

            var DFA = Utility.BuildDFA(testStrings, batchSize, false);
            var testString = string.Join(",", testStrings);
            var testStringLowercase = testString.ToLower();

            // Match the original string, should find all matches
            var matches = DFA.FindMatches(testString, false).Distinct().ToList();

            Assert.Equal(totalSize, matches.Count);

            foreach (var match in matches)
            {
                Assert.False(testStrings.Add(match));
            }

            // Match the lowercase version of the original string, should still find all matches
            matches = DFA.FindMatches(testStringLowercase, false).Distinct().ToList();

            Assert.Equal(totalSize, matches.Count);
        }

        [Fact]
        public void TestBuildDFACaseSensitive()
        {
            var totalSize = 100;
            var batchSize = 10;
            var testStringLength = 8;

            var testStrings = new HashSet<string>();

            while (testStrings.Count < totalSize)
            {
                testStrings.Add(GenerateRandomString(testStringLength));
            }

            var DFA = Utility.BuildDFA(testStrings, batchSize, true);
            var testString = string.Join(",", testStrings);
            var testStringLowercase = testString.ToLower();

            // Match the original string, should find all matches
            var matches = DFA.FindMatches(testString, true).Distinct().ToList();

            Assert.Equal(totalSize, matches.Count);

            foreach (var match in matches)
            {
                Assert.False(testStrings.Add(match));
            }

            // Match the lowercase version of the original string, should not find all matches
            matches = DFA.FindMatches(testStringLowercase, true).Distinct().ToList();

            Assert.True(totalSize > matches.Count);
        }


        private string GenerateRandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }
    }
}
