using dpp.opentakrouter.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace dpp.opentakrouter
{
    public class ClientRepository : IClientRepository
    {
        private readonly OpenTakRouterDbContext _db;

        public ClientRepository(OpenTakRouterDbContext context)
        {
            _db = context;
        }

        public int Add(Client c)
        {
            _db.Clients.Add(c);
            return _db.SaveChanges();
        }

        public int Delete(string q)
        {
            var c = Get(q);
            if (c == null)
            {
                return 0;
            }

            _db.Clients.Remove(c);
            return _db.SaveChanges();
        }

        public Client Get(string callsign)
        {
            return _db.Clients.FirstOrDefault(c => c.Callsign == callsign);
        }

        public IEnumerable<Client> Search(string query = "")
        {
            var clients = _db.Clients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                clients = clients.Where(c => c.Callsign.Contains(query));
            }

            return clients
                .OrderByDescending(c => c.LastSeen)
                .AsNoTracking()
                .ToList();
        }

        public int Update(Client c)
        {
            _db.Clients.Update(c);
            return _db.SaveChanges();
        }

        public int Upsert(Client c)
        {
            var e = Get(c.Callsign);
            if (e is null)
            {
                return Add(c);
            }

            e.Uid = c.Uid;
            e.LastSeen = c.LastSeen;
            e.LastStatus = c.LastStatus;
            e.Device = c.Device;
            e.Platform = c.Platform;
            e.Version = c.Version;
            return Update(e);
        }
    }
}
