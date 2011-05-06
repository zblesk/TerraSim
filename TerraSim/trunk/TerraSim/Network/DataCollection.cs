using System.Collections.Generic;

namespace TerraSim.Network
{
    public class DataCollection
    {
        private List<string[]> has_attribute = null, performs_action = null;

        public DataCollection()
        {
            has_attribute = new List<string[]>();
            performs_action = new List<string[]>();
        }

        public void AddAttribute(string target, string attributeName, string value)
        {
            has_attribute.Add(new string[] { target, attributeName, value });
        }

        public void AddAction(string actor, string action, string parameter1, string parameter2)
        {
            performs_action.Add(new string[] { actor, action, parameter1, parameter2 });
        }

        /// <summary>
        /// Clears all the data stored in this instance.
        /// </summary>
        public void Clear()
        {
            performs_action.Clear();
            has_attribute.Clear();
        }

        /// <summary>
        /// Adds data from the source collection into this collection.
        /// </summary>
        /// <param name="source">Source of the data do be added.</param>
        public void AddFrom(DataCollection source)
        {
            foreach (var att in source.has_attribute)
            {
                this.has_attribute.Add(att);
            }
            foreach (var act in source.performs_action)
            {
                this.performs_action.Add(act);
            }
        }

        /// <summary>
        /// Gets a read-only list of Attribute data stored in this instance.
        /// </summary>
        /// <returns>Returns an enumerator of the Attributes collection.</returns>
        /// <remarks>Attributes are added through the AddAttribute method.</remarks>
        public IEnumerable<string[]> AttributeData()
        {
            return has_attribute.AsReadOnly();
        }

        /// <summary>
        /// Gets a read-only list of Actions data stored in this instance.
        /// </summary>
        /// <returns>Returns a read-only list of the Actions collection.</returns>
        public IEnumerable<string[]> ActionData()
        {
            return performs_action.AsReadOnly();
        }
    }
}
