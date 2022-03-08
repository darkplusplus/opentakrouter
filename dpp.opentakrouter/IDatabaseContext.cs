using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dpp.opentakrouter
{
    public interface IDatabaseContext
    {
        public SQLiteConnection Database { get; set; }
    }
}
