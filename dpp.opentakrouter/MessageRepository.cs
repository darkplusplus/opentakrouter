using dpp.opentakrouter.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dpp.opentakrouter
{
    public class MessageRepository : IMessageRepository
    {
        private readonly OpenTakRouterDbContext _db;

        public MessageRepository(OpenTakRouterDbContext context)
        {
            _db = context;
            EvictExpired();
        }

        public int Add(StoredMessage msg)
        {
            _db.Messages.Add(msg);
            return _db.SaveChanges();
        }

        public int Delete(string q)
        {
            var m = Get(q);
            if (m == null)
            {
                return 0;
            }

            _db.Messages.Remove(m);
            return _db.SaveChanges();
        }

        public StoredMessage Get(string UID)
        {
            return _db.Messages.FirstOrDefault(m => m.Uid == UID);
        }

        public IEnumerable<StoredMessage> Search(string query)
        {
            var messages = _db.Messages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                messages = messages.Where(m => m.Data.Contains(query));
            }

            return messages.AsNoTracking().ToList();
        }

        public int Update(StoredMessage msg)
        {
            _db.Messages.Update(msg);
            return _db.SaveChanges();
        }

        public int Upsert(StoredMessage m)
        {
            var e = Get(m.Uid);
            if (e is null)
            {
                return Add(m);
            }

            e.Data = m.Data;
            e.Timestamp = m.Timestamp;
            e.Expiration = m.Expiration;
            return Update(e);
        }

        public int EvictExpired()
        {
            var expired = _db.Messages.Where(m => m.Expiration < DateTime.UtcNow).ToList();
            if (expired.Count == 0)
            {
                return 0;
            }

            _db.Messages.RemoveRange(expired);
            return _db.SaveChanges();
        }

        public IEnumerable<StoredMessage> GetActive()
        {
            return _db.Messages
                .Where(m => m.Expiration >= DateTime.UtcNow)
                .AsNoTracking()
                .ToList();
        }
    }
}
