using MultiRegexSearcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiRegexSearcher
{
    public static class NFAToDFA
    {
        public static FSM ConvertNFAToDFA(FSM NFA)
        {
            FSM DFA = new FSM();

            Queue<PowerSetElement> statesToCheck = new Queue<PowerSetElement>();
            var startingState = new PowerSetElement(new List<State> { NFA.StartingState });
            startingState.OutputTransitions = NFA.StartingState.OutputTransitions;
            statesToCheck.Enqueue(EpsilonClosureOf(startingState));
            DFA.States.Add(statesToCheck.Peek());
            DFA.StartingState = statesToCheck.Peek();

            PowerSetElement active;
            while (statesToCheck.Count != 0)
            {
                active = statesToCheck.Dequeue();
                //Get all possible transitions from any of the combined states
                HashSet<string> alphabetReach = new HashSet<string>();
                active.CombinedStates.ForEach(
                    state 
                        => 
                    EpsilonClosureOf(state).CombinedStates.ForEach(
                        reachedState 
                            =>
                        reachedState.OutputTransitions.ConvertAll(
                            trans 
                                => 
                            alphabetReach.Add(trans.Label))));
                foreach (string transLabel in alphabetReach)
                {
                    //See where I go with a specific label from this state. For that state find the epsilon-closure
                    //and add it to the que. 
                    if (transLabel == string.Empty)
                        continue;
                    HashSet<State> newCombinedStates = new HashSet<State>();
                    active.CombinedStates
                        .ForEach(state => state.OutputTransitions
                            .Where(trans => trans.Label == transLabel).ToList()
                                .ForEach(trans => EpsilonClosureOf(trans.TargetState).CombinedStates
                                    .ForEach(stateInClosureList => newCombinedStates.Add(stateInClosureList))));

                    PowerSetElement newCandidateDFAState = new PowerSetElement(newCombinedStates.ToList());
                    PowerSetElement newDFAState = (PowerSetElement)DFA.States.FirstOrDefault(powerset => powerset.Equals(newCandidateDFAState));
                    if (newDFAState == null)
                        newDFAState = newCandidateDFAState;
                    active.OutputTransitions.Add(new Transition(newDFAState, transLabel));
                    if (!DFA.States.Contains(newDFAState))
                    {
                        DFA.States.Add(newDFAState);
                        statesToCheck.Enqueue(newDFAState);
                    }
                }
            }

            DFA.MatchingStates.AddRange(DFA.States.Where(state => state.Type == State.StateType.Terminal));

            return DFA;
        }

        private static PowerSetElement EpsilonClosureOf(State theState)
        {
            var statesInImmeadiateEpsilonReach = theState.OutputTransitions
                            .Where(trans => trans.Label == string.Empty)
                            .Select(trans => trans.TargetState).ToList();
            HashSet<State> finalList = new HashSet<State>();
            statesInImmeadiateEpsilonReach
                .ForEach(stateInImmediateEpsilonReach 
                            => 
                        EpsilonClosureOf(stateInImmediateEpsilonReach).CombinedStates
                            .ForEach(state => finalList.Add(state)));
            finalList.Add(theState);
            return new PowerSetElement(finalList.ToList());
        }
    }
}
