using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POST
{
    public class Context : DbContext
    {
        public DbSet<Job> Job { get; set; }
    }

    public class Job
    {
        public int Id { get; set; }
        public int Begin { get; set; }
        public int End { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
}
