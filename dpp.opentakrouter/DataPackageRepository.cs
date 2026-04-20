using dpp.opentakrouter.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dpp.opentakrouter
{
    public class DataPackageRepository : IDataPackageRepository
    {
        private readonly OpenTakRouterDbContext _db;

        public DataPackageRepository(OpenTakRouterDbContext context)
        {
            _db = context;
        }

        public int Add(DataPackage datapackage)
        {
            _db.DataPackages.Add(datapackage);
            return _db.SaveChanges();
        }

        public int Add(IFormFile file, string hash, string filename, string submissionUser = "Anonymous", string creatorUid = "Anonymous", string keywords = "missionpackage", string visibility = "private")
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
                SubmissionDateTime = DateTime.UtcNow,
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
            var datapackage = Get(hash);
            if (datapackage == null)
            {
                return 0;
            }

            _db.DataPackages.Remove(datapackage);
            return _db.SaveChanges();
        }

        public DataPackage Get(string hash)
        {
            return _db.DataPackages.FirstOrDefault(dp => dp.Hash == hash);
        }

        public IEnumerable<DataPackage> Search(string keywords = "")
        {
            var packages = _db.DataPackages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                packages = packages.Where(dp => dp.Keywords.Contains(keywords));
            }

            return packages
                .OrderByDescending(dp => dp.SubmissionDateTime)
                .AsNoTracking()
                .ToList();
        }

        public int Update(DataPackage dp)
        {
            _db.DataPackages.Update(dp);
            return _db.SaveChanges();
        }
    }
}
