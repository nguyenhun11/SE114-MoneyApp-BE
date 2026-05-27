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
        public DbSet<Transaction> Transactions { get; set; }

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
        }
    }
}
