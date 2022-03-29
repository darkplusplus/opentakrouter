using dpp.opentakrouter.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace dpp.opentakrouter
{
    public interface IDataPackageRepository
    {
        public IEnumerable<DataPackage> Search(string keywords = "");
        public DataPackage Get(string hash);
        public int Add(DataPackage dp);
        public int Add(IFormFile file, string hash, string filename, string submissionUser = "Anonymous", string creatorUid = "Anonymous", string keywords = "missionpackage", string visibility = "private");
        public int Update(DataPackage dp);
        public int Delete(string hash);
    }
}