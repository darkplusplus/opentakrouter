using dpp.cot;
using System;

namespace dpp.opentakrouter
{
    public interface IRouter
    {
        public event EventHandler<RoutedEventArgs> RaiseRoutedEvent;
        public void Send(Event e, byte[] data);
    }
}
