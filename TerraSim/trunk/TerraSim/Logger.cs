using System;
using System.IO;

namespace TerraSim
{
    public class Logger : IDisposable
    {
        private StreamWriter output = null;

        public Logger(StreamWriter writer)
        {
            output = writer; 
        }

        public void Close()
        {
            output.Close();
        }

        public void WriteLine(string line)
        {
            output.WriteLine(DateTime.Now.TimeOfDay.ToString() + line);
        }

        #region IDisposable Members
        
        void IDisposable.Dispose()
        {
            output.Close();
            output.Dispose();
        }

        #endregion
    }
}
