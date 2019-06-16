using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultiRegexSearcher
{
    public static class Utility
    {
        private static Regex _nonWordRegex = null;
        private static Regex NonWordRegex => _nonWordRegex ?? (_nonWordRegex = new Regex(@"\W+", RegexOptions.Compiled));

        /// <summary>
        /// Find all matches for DFA in input string
        /// </summary>
        /// <param name="DFA">The DFA to use for searching</param>
        /// <param name="input">String to perform search on</param>
        /// <param name="caseSensitive">Whether the search should be case sensitive</param>
        /// <param name="checkStarting">Whether a match is only found if it's the beginning of a word</param>
        /// <param name="checkEnding">Whether a match is only found if it's the end of a word</param>
        /// <returns>Collection of matching search strings</returns>
        public static IEnumerable<string> FindMatches(this FSM DFA, string input, bool caseSensitive = false, bool checkStarting = false, bool checkEnding = false)
        {
            var searchInput = caseSensitive ? input : input.ToLower();

            for (int i = 0; i < searchInput.Length - 1; i++)
            {
                var length = FindMatch(searchInput.Substring(i, searchInput.Length - i), DFA, DFA.StartingState);
                if (length > 0)
                {
                    // Check that the match is the start of the word, and not the middle of a longer string
                    if (checkStarting)
                    {
                        if (i != 0 && !NonWordRegex.IsMatch(searchInput.Substring(i - 1, 1)))
                        {
                            continue;
                        }
                    }

                    // Check that the match is the end of the word, and doesn't continue as a longer string
                    if (checkEnding)
                    {
                        if ((searchInput.Length > i + length) && !NonWordRegex.IsMatch(searchInput.Substring(i + length - 1, 1)))
                        {
                            i += length - 2;
                            continue;
                        }
                    }

                    yield return input.Substring(i, length - 1);
                    i += length - 2;
                }
            }
        }

        private static int FindMatch(string input, FSM DFA, State startingState)
        {
            if (startingState.Type == State.StateType.Terminal)
                return 1;

            var length = 0;
            for (int i = 0; i < startingState.OutputTransitions.Count; i++)
            {
                if (startingState.OutputTransitions[i].Label == string.Empty) //epsilon
                    length = FindMatch(input, DFA, startingState.OutputTransitions[i].TargetState);
                /*We need the input.Length > 0 here. Because the string may be exhausted but there may be epsilon transitions leading to 
                terminal states. We'd like to be able to follow the transitions in the if clause above. But if there are transitions in 
                the current state that are not epsilon and the string is exhausted, we would like to prevent an exception.*/
                else if (input.Length > 0 && Regex.IsMatch(input[0].ToString(), startingState.OutputTransitions[i].Label)) //match
                    length = FindMatch(input.Substring(1 % input.Length, input.Length - 1), DFA, startingState.OutputTransitions[i].TargetState);

                if (length > 0)
                    return ++length;
            }

            return 0;
        }

        /// <summary>
        /// Builds a Deterministic Finite Automaton based on a collection of search strings
        /// </summary>
        /// <param name="searchStrings">Collection of search strings</param>
        /// <param name="batchSize">The number of search strings to process at a time while building the DFA</param>
        /// <returns></returns>
        public static FSM BuildDFA(IEnumerable<string> searchStrings, int batchSize = 100, bool caseSensitive = false)
        {
            
            var batches = GetBatches(searchStrings, batchSize);
            var DFA = new FSM();

            foreach (var batch in batches)
            {
                var regexCollection = batch.Select(k => new Regex(caseSensitive ? k : k.ToLower()));

                FSM NFA = new FSM();
                foreach (var regex in regexCollection)
                {
                    NFA |= FSM.Parse(regex.ToString());
                }

                DFA |= NFAToDFA.ConvertNFAToDFA(NFA);
            }

            return NFAToDFA.ConvertNFAToDFA(DFA);
        }

        private static IEnumerable<IEnumerable<TSource>> GetBatches<TSource>(IEnumerable<TSource> source, int size)
        {
            TSource[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                    bucket = new TSource[size];

                bucket[count++] = item;
                if (count != size)
                    continue;

                yield return bucket;

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
                yield return bucket.Take(count);
        }
    }
}