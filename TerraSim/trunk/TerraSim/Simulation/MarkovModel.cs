using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TerraSim.Simulation
{
    public class MarkovModel
    {
        private static Random rand; 
        private MarkovModelStateData[] stateData = null;
        private readonly int stateCount = 0;

        public int CurrentState {get; private set;} 
        public readonly string[] StateNames;

        static MarkovModel()
        {
            rand = new Random();
        }

        public MarkovModel(MarkovModelStateData[] modelData)
        {
            this.stateData = modelData;
            stateCount = modelData.Length;
            StateNames = new string[stateCount];
            CurrentState = 0;
            for (int i = 0; i < modelData.Length; i++)
            {
                StateNames[i] = modelData[i].Name;
            }
        }

        /// <summary>
        /// Set the current state. 
        /// </summary>
        /// <param name="id">ID (index in dhe StateNames array) of the state
        /// to be used.</param>
        /// <exception cref="IndexOutOfRangeException">Throws IndexOutOfRangeException, 
        /// if the index is invalid.</exception>
        public void SetState(int id)
        {
            if ((id < 0) || (id >= StateNames.Length))
            {
                throw new IndexOutOfRangeException();
            }
            CurrentState = id;
        }

        /// <summary>
        /// Moves the model to next state.
        /// </summary>
        /// <returns>Returns the state number.</returns>
        public int NextState()
        {
            double next = rand.NextDouble();
            int index = 0;
            double accumulator = stateData[CurrentState].TransitionProbabilities[0];
            while (accumulator < next)
            {
                index += 1;
                accumulator += stateData[CurrentState].TransitionProbabilities[index];
            }
            CurrentState = index;
            return CurrentState;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MarkovModelStateData d in stateData)
            {
                sb.AppendFormat("{0}\t", d.Name);
                foreach (double tp in d.TransitionProbabilities)
                {
                    sb.AppendFormat("{0}\t", tp);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static MarkovModel Load(StreamReader inputJson)
        {
            MarkovModelStateData[] data =
                JsonConvert.DeserializeObject<MarkovModelStateData[]>(inputJson.ReadToEnd());
            if (Validate(data))
            {
                return new MarkovModel(data);
            }
            else return null;
        }

        public void Save(StreamWriter output)
        {
            output.WriteLine(JsonConvert.SerializeObject(stateData, Formatting.Indented));
        }

        private static bool Validate(MarkovModelStateData[] data)
        {
            HashSet<string> names = new HashSet<string>();
            double sum = 0;
            long length = data[0].TransitionProbabilities.Length; // surely contains at least one item
            foreach (MarkovModelStateData d in data)
            {
                if (names.Contains(d.Name))
                {
                    throw new InvalidDataException(string.Format("Duplicit state name: {0}", d.Name));
                    //return false; // duplicit name
                }
                else
                {
                    names.Add(d.Name);
                }
                sum = 0;
                if (d.TransitionProbabilities.Length != length)
                {
                    throw new InvalidDataException(string.Format("Wrong array length for state: {0}", d.Name));
                    //return false; // invalid length
                }
                foreach (double v in d.TransitionProbabilities)
                {
                    sum += v;
                }
                if (sum != 1.0)
                {
                    throw new InvalidDataException(string.Format(
                        "For state: {0}, transition probabilities do not sum to 1.", d.Name));
                    //return false; // probabilities do not sum to 1
                }
            }
            return true;
        }
    }

    public struct MarkovModelStateData
    {
        public double[] TransitionProbabilities;
        public string Name;
    }
}
