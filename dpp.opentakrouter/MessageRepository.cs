using dpp.opentakrouter.Models;
using SQLite;
using System;
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

            EvictExpired();
        }

        public int Add(StoredMessage msg)
        {
            _db.BeginTransaction();
            var r = _db.Insert(msg);
            _db.Commit();

            return r;
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
            _db.BeginTransaction();
            var r = _db.Update(msg);
            _db.Commit();

            return r;
        }

        public int Upsert(StoredMessage m)
        {
            var e = Get(m.Uid);
            if (e is null)
            {
                return Add(m);
            }

            return Update(e);
        }

        public int EvictExpired()
        {
            return _db.Table<StoredMessage>().Where(m => m.Expiration < DateTime.Now).Delete();
        }

        public IEnumerable<StoredMessage> GetActive()
        {
            return _db.Table<StoredMessage>().Where(m => m.Expiration >= DateTime.Now);
        }
    }
}
