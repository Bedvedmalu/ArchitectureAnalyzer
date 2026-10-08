using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CrossPlatform.Models;

namespace CrossPlatform.Data
{
    public class CrossPlatformContext : DbContext
    {
        public CrossPlatformContext(DbContextOptions<CrossPlatformContext> options)
            : base(options)
        {
        }

        public DbSet<CrossPlatform.Models.Movie> Movie { get; set; } = default!;
    }
}