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
            OnRaiseRoutedEvent(new RoutedEventArgs(e));
        }

        protected virtual void OnRaiseRoutedEvent(RoutedEventArgs e)
        {
            RaiseRoutedEvent?.Invoke(this, e);
        }
    }

    public class RoutedEventArgs : EventArgs
    {
        public Event Event { get; set; }
        public RoutedEventArgs(Event e)
        {
            this.Event = e;
        }
    }
}
