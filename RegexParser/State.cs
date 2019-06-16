using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiRegexSearcher
{
    public class State
    {
        public virtual string Label { get; set; }
        public StateType Type { get; set; }
        private List<Transition> outputTransitions = new List<Transition>();
        private static Random randomizer = new Random();
        public List<Transition> OutputTransitions
        {
            get
            {
                return outputTransitions;
            }
            set
            {
                outputTransitions = value;
            }
        }

        public State(StateType type, string label)
        {
            this.Label = Math.Round(randomizer.NextDouble() * 1000).ToString();
            this.Type = type;
        }

        public enum StateType
        {
            Terminal, 
            NonTerminal
        }

        public override string ToString()
        {
            string str = string.Join(",", outputTransitions.ConvertAll(trans => this.Label + trans.ToString()));
            if (this.Type == StateType.Terminal && outputTransitions.Count == 0)
                str = this.Label + ":***";
            return str;
        }

    }
}
