using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }
        public DbSet<TaskAssignee> TaskAssignees { get; set; }
        public DbSet<TaskRelationship> TaskRelationships { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite key for TaskAssignee
            modelBuilder.Entity<TaskAssignee>()
                .HasKey(ta => new { ta.TaskId, ta.UserId });

            // Configure relationships for TaskAssignee
            modelBuilder.Entity<TaskAssignee>()
                .HasOne(ta => ta.Task)
                .WithMany(t => t.Assignees)
                .HasForeignKey(ta => ta.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskAssignee>()
                .HasOne(ta => ta.User)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(ta => ta.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure relationships for Task
            modelBuilder.Entity<Models.Task>()
                .HasOne(t => t.Creator)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deleting user if they created tasks, or set to Cascade/SetNull as per requirements

            // Configure relationships for TaskRelationship
            modelBuilder.Entity<TaskRelationship>()
                .HasKey(tr => tr.RelationshipId); // Explicitly define primary key if not done by convention
            
            modelBuilder.Entity<TaskRelationship>()
                .HasOne(tr => tr.ParentTask)
                .WithMany(t => t.ChildRelationships) // Task has many children relationships
                .HasForeignKey(tr => tr.ParentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskRelationship>()
                .HasOne(tr => tr.ChildTask)
                .WithMany(t => t.ParentRelationships) // Task has many parent relationships
                .HasForeignKey(tr => tr.ChildTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure Username is unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Ensure Email is unique if it's not nullable
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL"); // For SQLite, filter for unique index on nullable column
        }
    }
}

