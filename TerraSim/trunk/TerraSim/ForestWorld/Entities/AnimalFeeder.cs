using System;
using System.Collections.Generic;
using System.Linq;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Entities
{
    public class AnimalFeeder : NamedObject
    {
        internal class Mediator
        {
            private readonly AnimalFeeder owner;

            internal Mediator(AnimalFeeder owner)
            {
                this.owner = owner;
            }

            /// <summary>
            /// Puts an item into the feeder.
            /// </summary>
            /// <param name="obj">The item to be placed into the feeder.</param>
            internal void Put(NamedObject obj)
            {
                owner.contents.Add(obj);
            }

            /// <summary>
            /// Removes an item from the feeder.
            /// </summary>
            /// <param name="obj">The item to be removed from the feeder.</param>
            internal void Take(NamedObject obj)
            {
                owner.contents.Remove(obj);
            }

            internal HashSet<NamedObject> TakeAll()
            {
                var result = owner.contents;
                owner.contents = new HashSet<NamedObject>();
                return result;
            }

            /// <summary>
            /// Returns the first contained object matching a predicate.
            /// </summary>
            /// <param name="predicate">)A predicate used to filter the contents.</param>
            /// <returns>Returns the first found object, or null if none was found.</returns>
            internal NamedObject Contains(Func<NamedObject, bool> predicate)
            {
                NamedObject result = null;
                try
                {
                    result = owner.contents.First(predicate);
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
                return result;
            }
        }

        internal static string TypeString = "animal_feeder";
        private HashSet<NamedObject> contents = new HashSet<NamedObject>();

        public AnimalFeeder(string name) : base(name) 
        {
            Type = TypeString;
            DispatchMediator = new Mediator(this);
        }

        public override void Update(WorldUpdateEventArgs args) { }

        public override void Marshal(DataCollection dataCollection)
        {
            base.Marshal(dataCollection);
            foreach (var item in contents)
            {
                dataCollection.AddAttribute(Name, "contains", item.Name);
            }
        }
    }
}
