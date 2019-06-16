using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiRegexSearcher
{
    public class Transition
    {
        public State TargetState { get; set; }
        public string Label { get; set; }

        internal Transition(State _targetState, string _label)
        {
            TargetState = _targetState;
            Label = _label;
        }

        public override string ToString()
        {
            return string.Format("->{0}({1})", TargetState.Label, Label);
        }

        public String GetDotRepresentation()
        {
            return String.Format(" -> {0} [ label = \"{1}\" ];", TargetState.Label, Label);
        }
    }
}
