using dpp.opentakrouter.Models;
using System.Collections.Generic;

namespace dpp.opentakrouter
{
    public interface IMessageRepository
    {
        public IEnumerable<StoredMessage> Search(string keywords = "");
        public StoredMessage Get(string UID);
        public int Add(StoredMessage msg);
        public int Update(StoredMessage msg);
        public int Delete(string UID);
    }
}