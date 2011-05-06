using System;
using System.Collections.Generic;
using System.Linq;

namespace TerraSim.Simulation
{
    using ActionData = Tuple<Agent, Action<object, Action<object>>>;
    using ActionSet = HashSet<Tuple<Agent, Action<object, Action<object>>>>;
    using Callback = Action<object>;

    public sealed class MessageBroadcaster
    {
        private Dictionary<string, ActionSet> subscriptions =
            new Dictionary<string, ActionSet>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Subscribes an action to listen for a message. 
        /// </summary>
        /// <param name="toMessage">A case-insensitive name of the message
        /// the action should be triggered by.</param>
        /// <param name="handler">The action to be triggered.</param>
        /// <param name="recipient">Agent receiving the action.</param>
        public void Subscribe(string toMessage, Callback handler, 
            Agent recipient)
        {
            if (handler == null)
            {
                throw new ArgumentException("Callback must not be null.", "callback");
            }
            ActionSet set;
            if (!subscriptions.TryGetValue(toMessage, out set))
            {
                set = new ActionSet();
                subscriptions[toMessage] = set;
            }
            set.Add(new ActionData(recipient, (a, c) => { handler(a); }));
        }

        /// <summary>
        /// Subscribes an action to listen for a message. 
        /// </summary>
        /// <param name="toMessage">A case-insensitive name of the message
        /// the action should be triggered by.</param>
        /// <param name="handler">The action to be triggered. Besides the argument,
        /// it also accepts a callback to send the results back to the caller.</param>
        /// <param name="recipient">Agent receiving the action.</param>
        public void SubscribeWithCallback(string toMessage, 
            Action<object, Callback> handler, Agent recipient)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("callback");
            }
            ActionSet set;
            if (!subscriptions.TryGetValue(toMessage, out set))
            {
                set = new ActionSet();
                subscriptions[toMessage] = set;
            }
            set.Add(new ActionData(recipient, handler));
        }

        /// <summary>
        /// Unregisters the recipient from the message broadcast.
        /// </summary>
        /// <param name="forMessage">A case-insensitive name of the message
        /// the recipient wishes to be unsubscribed from.</param>
        /// <param name="recipient">Recipient which wants to unsubscribe.</param>
        public void Unsubscribe(string fromMessage, Agent recipient)
        {
            ActionSet set;
            if (subscriptions.TryGetValue(fromMessage, out set))
            {
                ActionData data = set.FirstOrDefault((d) => { return d.Item1 == recipient; });
                set.Remove(data);
            }
        }

        /// <summary>
        /// Broadcasts a message to all subscribers.
        /// </summary>
        /// <param name="message">Name of the message being boradcast.</param>
        /// <param name="argument">Argument for the the message subscribers.</param>
        /// <param name="callback">[optional] Callback function to be 
        /// called by the subscriber.</param>
        /// <param name="sender">[optional] Sender of the message.</param>
        /// <param name="targetId">[optional] If provided, the message will only
        /// be handled by registered agents with this ID.</param>
        /// <remarks>If Callback is specified, it will be passed to each
        /// subscribed function.</remarks>
        public void Broadcast(string message, object argument,
            Agent sender, Callback callback = null, string targetName = null)
        {
            ActionSet set;
            if (subscriptions.TryGetValue(message, out set))
            {
                var validActions = from subscrip in set
                                   where (targetName == null)
                                    || (subscrip.Item1.Name == targetName)
                                   select subscrip.Item2;
                foreach (var action in validActions)
                {
                    action(argument, callback);
                }
            }
        }
    }
}
