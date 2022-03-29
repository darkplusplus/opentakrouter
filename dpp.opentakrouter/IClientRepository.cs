using dpp.opentakrouter.Models;
using System.Collections.Generic;

namespace dpp.opentakrouter
{
    public interface IClientRepository
    {
        public IEnumerable<Client> Search(string query = "");
        public Client Get(string callsign);
        public int Add(Client c);
        public int Update(Client c);
        public int Delete(string c);
        public int Upsert(Client c);
    }
}
