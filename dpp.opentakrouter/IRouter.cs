using dpp.cot;
using System;
using System.Collections.Generic;

namespace dpp.opentakrouter
{
    public interface IRouter
    {
        public event EventHandler<RoutedEventArgs> RaiseRoutedEvent;
        public void Route(CotMessageEnvelope envelope);
        IEnumerable<Event> GetActiveEvents();
    }
}
