using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MultiRegexSearcher
{
    public class FSM
    {
        public State StartingState { get; set; }

        List<State> states = new List<State>();
        public List<State> States
        {
            get
            {
                return states;
            }
            set
            {
                states = value;
            }
        }

        List<State> matchingStates = new List<State>();
        public List<State> MatchingStates
        {
            get { return matchingStates; }
            set { matchingStates = value; }
        }

        public static FSM operator +(FSM first, FSM second)
        {
            if (first == null)
                return second;
            if (second == null)
                return first;
            foreach (var state in first.matchingStates)
            {
                state.Type = State.StateType.NonTerminal;
                state.OutputTransitions.AddRange(second.StartingState.OutputTransitions);
            }

            first.MatchingStates = second.MatchingStates;
            second.States.Where(state => state != second.StartingState).ToList().ForEach(state => first.States.Add(state));

            return first;
        }

        public static FSM operator |(FSM first, FSM second)
        {
            if (first == null || second == null)
                return first ?? second;

            if (first.states.Count == 0)
                return second;
            if (second.states.Count == 0)
                return first;

            FSM newOredFSM = new FSM();
            newOredFSM.StartingState = new State(State.StateType.NonTerminal, "OrStart");
            newOredFSM.States.Add(newOredFSM.StartingState);

            newOredFSM.StartingState.OutputTransitions.Add(new Transition(first.StartingState, ""));
            newOredFSM.StartingState.OutputTransitions.Add(new Transition(second.StartingState, ""));

            newOredFSM.MatchingStates.Add(new State(State.StateType.Terminal, "OrEnd"));
            newOredFSM.States.Add(newOredFSM.MatchingStates[0]);

            foreach (var state in first.states)
            {
                newOredFSM.states.Add(state);
                if (state.Type == State.StateType.Terminal)
                {
                    state.Type = State.StateType.NonTerminal;
                    state.OutputTransitions.Add(new Transition(newOredFSM.MatchingStates[0], ""));
                }
            }

            foreach (var state in second.states)
            {
                newOredFSM.states.Add(state);
                if (state.Type == State.StateType.Terminal)
                {
                    state.Type = State.StateType.NonTerminal;
                    state.OutputTransitions.Add(new Transition(newOredFSM.MatchingStates[0], ""));
                }
            }

            return newOredFSM;
        }

        public static int FindMatchingClosingParanths(string str, int fromIndex)
        {
            if (fromIndex > str.Length - 1)
                throw new Exception("This should not happen!");

            int matchingParanthCount = 1;
            for (int i = fromIndex; i < str.Length; i++)
            {
                if (str[i] == '(')
                    matchingParanthCount++;
                if (str[i] == ')')
                    matchingParanthCount--;
                if (matchingParanthCount == 0)
                    return i;
            }

            throw new Exception("Malformed Regex. No Mathing \")\" was found");
        }

        public static FSM Parse(string regex)
        {
            Stack<FSM> operandsStack = new Stack<FSM>();
            Stack<Token> operatorsStack = new Stack<Token>();

            Token concatOperatorToken = new Token(TokenType.Operator, "concat");
            Token prevToken = null;
            int index = 0;
            for (; index < regex.Length; index++)
            {
                Token nextToken = Token.GetTokenAt(regex, ref index);
                if (nextToken.Type == TokenType.Operator)
                {
                    if (nextToken.Value == "(")
                    {
                        int closingIndex = FindMatchingClosingParanths(regex, index + 1);
                        FSM FSMInParanths = Parse(regex.Substring(index + 1, closingIndex - index - 1));
                        ConstructNConcat(operandsStack, operatorsStack, concatOperatorToken, prevToken);
                        operandsStack.Push(FSMInParanths);
                        index = closingIndex;
                        continue;
                    }
                    if (operatorsStack.Count == 0 || nextToken < operatorsStack.Peek())
                        operandsStack.Push(ConstructFSMUpToHere(operandsStack, operatorsStack));
                    operatorsStack.Push(nextToken);
                }
                else
                {
                    //There is a default operator "Concat" being pushed into the stack after any operand is added.
                    if (index > 0)
                        ConstructNConcat(operandsStack, operatorsStack, concatOperatorToken, prevToken);
                    operandsStack.Push(GetEmptyFSM(nextToken.Value));
                }
                prevToken = nextToken;
            }

            if (operandsStack.Count != 0 || operatorsStack.Count != 0)
                operandsStack.Push(ConstructFSMUpToHere(operandsStack, operatorsStack));

            return operandsStack.Pop();
        }

        private static void ConstructNConcat(Stack<FSM> operandsStack, Stack<Token> operatorsStack, Token concatOperatorToken, Token prevToken)
        {
            if (PrevTokenRepresentedCompleteExpression(prevToken))
            {
                if (operatorsStack.Count > 0 && concatOperatorToken < operatorsStack.Peek())
                    operandsStack.Push(ConstructFSMUpToHere(operandsStack, operatorsStack));
                operatorsStack.Push(concatOperatorToken);
            }
        }

        private static bool PrevTokenRepresentedCompleteExpression(Token prevToken)
        {
            /*If the previous token is a normal operand or it was a post-fix operator(*, ?, +), then we should
             add a concatenation.*/
            string[] unaryOperators = new[] { "*", "?", "+" };
            return (prevToken.Type == TokenType.Operand || unaryOperators.Contains(prevToken.Value));
        }

        private static FSM ConstructFSMUpToHere(Stack<FSM> operandsStack, Stack<Token> operatorStack)
        {
            Token operatorTok;
            while (operatorStack.Count != 0)
            {
                FSM partialFSM = null;
                operatorTok = operatorStack.Pop();
                switch (operatorTok.Value)
                {
                    case "*":
                        partialFSM = GetZeroOrMoreNFA(operandsStack.Pop(), true);
                        break;
                    case "+":
                        partialFSM = GetZeroOrMoreNFA(operandsStack.Pop(), false);
                        break;
                    case "concat":
                        if (operandsStack.Count == 0)
                            break;
                        FSM right = operandsStack.Pop();
                        if (operandsStack.Count == 0)
                            partialFSM = right;
                        else
                            partialFSM = operandsStack.Pop() + right;
                        break;
                    case "?":
                        partialFSM = GetOptionalNFA(operandsStack.Pop());
                        break;
                    case "|":
                        partialFSM = operandsStack.Pop() | operandsStack.Pop();
                        break;
                }
                operandsStack.Push(partialFSM);
            }
            return operandsStack.Pop();
        }

        private static FSM GetOptionalNFA(FSM middle)
        {
            FSM newFSM = new FSM();
            newFSM.StartingState = new State(State.StateType.NonTerminal, "optionalStart");
            newFSM.States.Add(newFSM.StartingState);
            newFSM.StartingState.OutputTransitions.Add(new Transition(middle.StartingState, ""));

            newFSM.MatchingStates.Add(new State(State.StateType.Terminal, "optionalEnd"));
            newFSM.MatchingStates[0].Type = State.StateType.Terminal;

            foreach (var state in middle.States)
            {
                if (state.Type == State.StateType.Terminal)
                    state.OutputTransitions.Add(new Transition(newFSM.MatchingStates[0], ""));
                newFSM.States.Add(state);
            }

            newFSM.StartingState.OutputTransitions.Add(new Transition(newFSM.MatchingStates[0], ""));
            return newFSM;
        }

        private static FSM GetZeroOrMoreNFA(FSM middle, bool zeroAllowed)
        {
            FSM newFSM = new FSM();

            State starStartingState = new State(State.StateType.NonTerminal, "recursionStart");
            State starMatchingState = new State(State.StateType.Terminal, "recursionEnd");
            starMatchingState.Type = State.StateType.Terminal;

            newFSM.StartingState = starStartingState;
            newFSM.States.Add(starStartingState);

            newFSM.StartingState.OutputTransitions.Add(new Transition(middle.StartingState, ""));
            newFSM.States.AddRange(middle.States);

            //Add arc from end to start of midle for recursion and clear the matching state status.
            middle.matchingStates[0].OutputTransitions.Add(new Transition(middle.StartingState, ""));
            middle.MatchingStates[0].OutputTransitions.Add(new Transition(starMatchingState, ""));
            middle.MatchingStates[0].Type = State.StateType.NonTerminal;

            //New matching state added.
            newFSM.MatchingStates = new List<State>();
            newFSM.MatchingStates.Add(starMatchingState);
            newFSM.States.Add(starMatchingState);

            //Add transition from start to end to bypass the whole thing
            if (zeroAllowed)
                starStartingState.OutputTransitions.Add(new Transition(starMatchingState, ""));

            return newFSM;
        }

        private static FSM GetEmptyFSM(string label)
        {
            FSM emptyFSM = new FSM();

            State startState = new State(State.StateType.NonTerminal, "1");
            State state2 = new State(State.StateType.Terminal, "2");

            emptyFSM.States.Add(startState);
            emptyFSM.States.Add(state2);

            emptyFSM.StartingState = startState;

            emptyFSM.StartingState.OutputTransitions.Add(new Transition(state2, label));
            emptyFSM.MatchingStates.Add(state2);

            return emptyFSM;
        }

        public static bool StringMatchesNFA(string input, FSM NFA) { return StringMatchesNFA(input, NFA, NFA.StartingState); }
        public static bool StringMatchesNFA(string input, FSM NFA, State startingState)
        {
            if (input == string.Empty && startingState.Type == State.StateType.Terminal)
                return true;

            bool matched = false;
            for (int i = 0; i < startingState.OutputTransitions.Count; i++)
            {
                if (startingState.OutputTransitions[i].Label == string.Empty) //epsilon
                    matched = StringMatchesNFA(input, NFA, startingState.OutputTransitions[i].TargetState);
                /*We need the input.Length > 0 here. Because the string may be exhausted but there may be epsilon transitions leading to 
                terminal states. We'd like to be able to follow the transitions in the if clause above. But if there are transitions in 
                the current state that are not epsilon and the string is exhausted, we would like to prevent an exception.*/
                else if (input.Length > 0 && Regex.IsMatch(input[i].ToString(), startingState.OutputTransitions[i].Label)) //match
                    matched = StringMatchesNFA(input.Substring(1 % input.Length, input.Length - 1), NFA, startingState.OutputTransitions[i].TargetState);

                if (matched)
                    return true;
            }

            return false;
        }

        //Used in Graphviz for representaton purposes.
        public string GetDotRepresentation()
        {
            return string.Format("digraph finite_state_machine {{\n" +
                                 "rankdir=LR;\n" +
                                 "size=\"{0},0\"\n" +
                                 "node [shape = doublecircle]; {1};\n" +
                                 "node [shape = circle];\n" +
                                 "{2}\n" +
                                 "}}",
            States.Count,
            string.Join(" ", this.matchingStates.ConvertAll(state => state.Label)),
            string.Join("\n", States.ConvertAll(state => string.Join("\n", state.OutputTransitions.ConvertAll(trans => state.Label + trans.GetDotRepresentation())))));
        }
    }
}
