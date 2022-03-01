using dpp.cot;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dpp.opentakrouter
{
    public class Router
    {
        public event EventHandler<RoutedEventArgs> RaiseRoutedEvent;

        public void Send(Event e)                     
        {
            Send(e, null);
        }

        public void Send(Event e, byte[] r)
        {
            OnRaiseRoutedEvent(new RoutedEventArgs(e, r));
        }

        protected virtual void OnRaiseRoutedEvent(RoutedEventArgs e)
        {
            RaiseRoutedEvent?.Invoke(this, e);
        }
    }

    public class RoutedEventArgs : EventArgs
    {
        public Event Event { get; set; }
        public byte[] Raw { get; set; }
        public RoutedEventArgs(Event e, Byte[] r)
        {
            this.Event = e;
            this.Raw = r;
        }

        public RoutedEventArgs(Event e)
        {
            this.Event = e;
        }
    }
}
