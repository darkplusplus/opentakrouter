using dpp.opentakrouter.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace dpp.opentakrouter
{
    public class DataPackageRepository : IDataPackageRepository
    {
        private readonly IDatabaseContext _context;
        private readonly SQLiteConnection _db;

        public DataPackageRepository(IDatabaseContext context)
        {
            _context = context;
            _db = _context.Database;
            _db.CreateTable<DataPackage>();
        }

        public int Add(DataPackage datapackage)
        {
            return _db.Insert(datapackage);
        }

        public int Add(IFormFile file, string hash, string filename, string? submissionUser="Anonymous", string? creatorUid="Anonymous", string? keywords="missionpackage", string? visibility="private")
        {
            // TODO: compute the SHA256 hash if it's null

            var name = Path.GetFileNameWithoutExtension(file.FileName);
            var isPrivate = visibility.Equals("private");

            byte[] content;
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms);
                content = ms.ToArray();
            }

            var dp = new DataPackage()
            {
                UID = filename,
                Name = name,
                Hash = hash,
                SubmissionDateTime = DateTime.Now,
                SubmissionUser = submissionUser,
                CreatorUid = creatorUid,
                Keywords = keywords,
                MIMEType = file.ContentType,
                Size = file.Length,
                IsPrivate = isPrivate,
                Content = content,
            };

            return Add(dp);
        }

        public int Delete(string hash)
        {
            return _db.Table<DataPackage>().Where(dp => dp.Hash == hash).Delete();
        }

        public DataPackage Get(string hash)
        {
            return _db.Table<DataPackage>().Where(dp => dp.Hash == hash).First();
        }

        public IEnumerable<DataPackage> Search(string keywords = "")
        {
            return _db.Table<DataPackage>().Where(dp => dp.Keywords.Contains(keywords));
        }

        public int Update(DataPackage dp)
        {
            return _db.Update(dp);
        }
    }
}
