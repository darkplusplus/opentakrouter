using dpp.opentakrouter.Models;
using SQLite;
using System.Collections.Generic;

namespace dpp.opentakrouter
{
    public class MessageRepository : IMessageRepository
    {
        private readonly IDatabaseContext _context;
        private readonly SQLiteConnection _db;

        public MessageRepository(IDatabaseContext context)
        {
            _context = context;
            _db = _context.Database;
            _db.CreateTable<StoredMessage>();
        }

        public int Add(StoredMessage msg)
        {
            return _db.Insert(msg);
        }

        public int Delete(string q)
        {
            var m = Get(q);
            return _db.Delete(m);
        }

        public StoredMessage Get(string UID)
        {
            return _db.Table<StoredMessage>().Where(m => m.Uid == UID).FirstOrDefault();
        }

        public IEnumerable<StoredMessage> Search(string query)
        {
            return _db.Table<StoredMessage>().Where(m => m.Data.Contains(query));
        }

        public int Update(StoredMessage msg)
        {
            return _db.Update(msg);
        }
    }
}
