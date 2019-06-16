using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiRegexSearcher
{
    public sealed class PowerSetElement : State
    {
        public List<State> CombinedStates { get; set; }

        public PowerSetElement(List<State> states)
            : base(StateType.NonTerminal, string.Join(",", states.ConvertAll(state => state.Label)))
        {
            CombinedStates = new List<State>();
            CombinedStates.AddRange(states);
            if (states.Any(state => state.Type == StateType.Terminal))
                this.Type = StateType.Terminal;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PowerSetElement))
                return false;
            if (((PowerSetElement)obj).CombinedStates.Count != this.CombinedStates.Count)
                return false;
            if (((PowerSetElement)obj).CombinedStates.Any(state => !this.CombinedStates.Contains(state)))
                return false;
            return true;
        }

        public override string Label
        {
            get
            {
                return base.Label;
                //if (CombinedStates.Count > 1)
                //    return string.Join("I", CombinedStates.ConvertAll(state => state.Label));
                //return CombinedStates[0].Label;
            }
        }

        public override string ToString()
        {
            return string.Join(",", CombinedStates.ConvertAll(state => state.Label));
        }
    }
}
