using Microsoft.EntityFrameworkCore;
using OmrSheet.Models;

namespace OmrSheet.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }

        public DbSet<OMRSheet> OMRSheets { get; set; }

        public DbSet<ExamAnswerKey> AnswerKeys { get; set; }
    }
}