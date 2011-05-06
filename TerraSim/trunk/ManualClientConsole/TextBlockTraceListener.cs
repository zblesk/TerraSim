using System.Diagnostics;
using System.Windows.Controls;

namespace ManualClientConsole
{
    class TextBlockTraceListener : TraceListener
    {
        private TextBlock output;

        public TextBlockTraceListener(TextBlock output)
        {
            this.Name = "Trace";
            this.output = output;
        }

        public override void Write(string message)
        {
                output.Text += message;            
        }

        public override void WriteLine(string message)
        {
            Write(message + '\n');
        }
    }

}
