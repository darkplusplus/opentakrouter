using dpp.opentakrouter.Models;
using SQLite;
using System.Collections.Generic;

namespace dpp.opentakrouter
{
    public class ClientRepository : IClientRepository
    {
        private readonly IDatabaseContext _context;
        private readonly SQLiteConnection _db;

        public ClientRepository(IDatabaseContext context)
        {
            _context = context;
            _db = _context.Database;
            _db.CreateTable<Client>();
        }

        public int Add(Client c)
        {
            return _db.Insert(c);
        }

        public int Delete(string q)
        {
            var c = Get(q);
            return _db.Delete(c);
        }

        public Client Get(string callsign)
        {
            return _db.Table<Client>().Where(c => c.Callsign == callsign).FirstOrDefault();
        }

        public IEnumerable<Client> Search(string query = "")
        {
            // TODO: clean up how client data is enumerated
            return _db.Table<Client>().Where(c => c.Callsign == c.Callsign);
        }

        public int Update(Client c)
        {
            return _db.Update(c);
        }

        public int Upsert(Client c)
        {
            var e = Get(c.Callsign);
            if (e is null)
            {
                return Add(c);
            }

            return Update(e);
        }
    }
}
