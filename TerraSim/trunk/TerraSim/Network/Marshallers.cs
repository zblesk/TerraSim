using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TerraSim.Network
{
    internal static class Marshallers
    {
        private static JsonSerializer serializer;

        static Marshallers()
        {
            serializer = new JsonSerializer();
        }

        /// <summary>
        /// Creates a Predicate-style representation of the data in the input collection.
        /// </summary>
        /// <param name="data">Data to be marshalled.</param>
        /// <returns>Returns a string containing the marshalled data, 
        /// encoded as predicates.</returns>
        public static string ToPredicates(DataCollection data)
        {
            StringBuilder sb = new StringBuilder();
            foreach(string[] line in data.ActionData())
            {
                sb.AppendLine(string.Format("performs_action ({0}, {1}, {2}, {3})",
                    line[0], line[1], line[2], line[3]));
            }
            foreach(string[] line in data.AttributeData())
            {
                sb.AppendLine(string.Format("has_attribute ({0}, {1}, {2})",
                    line[0], line[1], line[2]));
            }
            return sb.ToString();
        }

        public static string ToJSON(DataCollection data, bool indent = false)
        {
            var stream = new MemoryStream(); 
            var writer = new JsonTextWriter(new StreamWriter(stream));
            writer.Formatting = indent ? Formatting.Indented : Formatting.None;
            var reader = new StreamReader(stream);
            stream.SetLength(0);
            writer.WriteStartObject();
            writer.WritePropertyName("has_attribute");
            serializer.Serialize(writer, data.AttributeData());
            writer.WritePropertyName("performs_action");
            serializer.Serialize(writer, data.ActionData());
            writer.WriteEndObject();
            writer.Flush();
            stream.Position = 0;
            return reader.ReadToEnd(); 
        }
    }
    
}
