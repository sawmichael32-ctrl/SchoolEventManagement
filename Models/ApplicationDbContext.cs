using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SchoolEventManagement.Models;

namespace SchoolEventManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Event> Events { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Evaluation> Evaluations { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TeacherCourse> TeacherCourses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Teacher>()
                .HasIndex(t => t.Username)
                .IsUnique();

            builder.Entity<TeacherCourse>()
                .HasOne(c => c.Teacher)
                .WithMany(t => t.Courses)
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}