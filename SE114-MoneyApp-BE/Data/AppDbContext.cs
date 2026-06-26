using Microsoft.EntityFrameworkCore;
using SE114_MoneyApp_BE.Models;

namespace SE114_MoneyApp_BE.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        public DbSet<User> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<AdjustBalance> AdjustBalances { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryGroup> CategoryGroups { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<GoalRecord> GoalRecords { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<CityState> CityStates { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Quest> Quests { get; set; }
        public DbSet<UserQuest> UserQuests { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL");

            // Cấu hình bảng Transfer để ngắt Cascade Delete
            modelBuilder.Entity<Transfer>()
                .HasOne(t => t.Source)
                .WithMany()
                .HasForeignKey(t => t.SourceAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Transfer>()
                .HasOne(t => t.Destination)
                .WithMany()
                .HasForeignKey(t => t.DestinationAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            // BẢNG TRANSACTIONS
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Account)
                .WithMany() 
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade); // Cho phép xóa ví -> xóa giao dịch

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.NoAction); // CẮT ĐƯỜNG CASCADE CỦA CATEGORY

            // --- Cấu hình cho CategoryGroup ---
            modelBuilder.Entity<CategoryGroup>()
                .HasOne(cg => cg.User)
                .WithMany()
                .HasForeignKey(cg => cg.UserId)
                .OnDelete(DeleteBehavior.NoAction); // KHÔNG CASCADE từ User -> Group

            // --- Cấu hình cho Category ---
            modelBuilder.Entity<Category>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction); // KHÔNG CASCADE từ User -> Category

            modelBuilder.Entity<Category>()
                .HasOne(c => c.CategoryGroup)
                .WithMany(g => g.Categories)
                .HasForeignKey(c => c.CategoryGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
