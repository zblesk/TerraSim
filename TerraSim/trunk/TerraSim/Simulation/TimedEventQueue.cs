using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace TerraSim.Simulation
{
    /// <summary>
    /// Encases the timer logic. Events will be fired at the given time or later. 
    /// </summary>
    /// <remarks>This naive implementation can be improved, should the need arise.</remarks>
    public class TimedEventQueue
    {    
        private LinkedList<WeakReference> timers = new LinkedList<WeakReference>();
        private bool paused = true;

        /// <summary>
        /// Calls the given function after the delay elapses.
        /// </summary>
        /// <param name="callback">A function to be called.</param>
        /// <param name="delay">Delay (in miliseconds).</param>
        /// <remarks>Use closures to pass parameters to the callback.</remarks>
        public void Enqueue(Action callback, double delay, bool autoReset = false)
        {
            Enqueue(new ElapsedEventHandler((o, e) => { callback(); }), delay, autoReset); 
        }
        
        /// <summary>
        /// Calls the given function after the delay elapses.
        /// </summary>
        /// <param name="callback">A function to be called.</param>
        /// <param name="delay">Delay (in miliseconds).</param>
        public void Enqueue(ElapsedEventHandler callback, double delay, bool autoReset = false)
        {
            Timer t = new Timer(delay);
            t.AutoReset = autoReset;
            t.Elapsed += callback;
            if (!paused)
            {
                t.Start();
            }
            timers.AddFirst(new WeakReference(t, false));
        }
        
        /// <summary>
        /// Pauses the execution of all timers.
        /// </summary>
        public void Pause()
        {
            Timer t;
            paused = true;
            var purge = new LinkedList<WeakReference>();
            foreach (var wr in timers)
            {
                t = wr.Target as Timer;
                if (t == null)
                {
                    purge.AddLast(wr);
                    //wr.IsAlive; zistit...???
                    continue;
                }
                t.Stop();
            }
            if (purge.Count > 0)
                foreach (var wr in purge)
                    timers.Remove(wr);
        }

        /// <summary>
        /// Starts the execution of all timers.
        /// </summary>
        public void Start()
        {
            Timer t;
            paused = false;
            foreach (var wr in timers)
            {
                t = wr.Target as Timer;
                if (t == null)
                {
                    continue;
                }
                t.Start();
            }
        }

        /// <summary>
        /// Purges the unnecessary (unused) timers.
        /// </summary>
        /// <returns></returns>
        public int Purge()
        {
            var purge = from wr in timers
                        where !wr.IsAlive
                        select wr;
            foreach (var wr in purge)
            {
                timers.Remove(wr);
            }
            return purge.Count();
        }

        /// <summary>
        /// Destroys all the timers.
        /// </summary>
        /// <returns></returns>
        public void Destroy()
        {
            Timer t;
            foreach (var w in timers)
            {
                if (w.Target != null)
                {
                    t = w.Target as Timer;
                    t.Stop();
                    t.Close();
                    t.Dispose();
                }
            }
            timers.Clear();
        }
    }
}
