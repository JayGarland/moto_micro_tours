using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using moto_micro_tours.Domain.Entities;

namespace moto_micro_tours
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MotardProfile> MotardProfiles { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<TourMotard> TourMotards { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Payout> Payouts { get; set; }
        public DbSet<PreBookingConversation> PreBookingConversations { get; set; }
        public DbSet<BookingConversation> BookingConversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewReply> ReviewReplies { get; set; }
        public DbSet<UserRole> AppUserRoles { get; set; }
        public DbSet<RateLimitCounter> RateLimitCounters { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TourMotard: Unique index on (TourId, MotardUserId) and FK to AppUser
            modelBuilder.Entity<TourMotard>()
                .HasIndex(x => new { x.TourId, x.MotardUserId }).IsUnique();

            modelBuilder.Entity<TourMotard>()
                .HasOne(tm => tm.Motard)
                .WithMany()
                .HasForeignKey(tm => tm.MotardUserId)
                .IsRequired();

            // Booking: FKs, 1:1 with Payment and Payout
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId)
                .IsRequired();
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Payout)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payout>(p => p.BookingId)
                .IsRequired();

            // PreBookingConversation: Unique (TourId, MotardUserId, ParticipantUserId)
            modelBuilder.Entity<PreBookingConversation>()
                .HasIndex(x => new { x.TourId, x.MotardUserId, x.ParticipantUserId }).IsUnique();

            // PreBookingConversation: Configure FK relationships explicitly
            modelBuilder.Entity<PreBookingConversation>()
                .HasOne(c => c.Motard)
                .WithMany()
                .HasForeignKey(c => c.MotardUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PreBookingConversation>()
                .HasOne(c => c.Participant)
                .WithMany()
                .HasForeignKey(c => c.ParticipantUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // BookingConversation: Unique (BookingId)
            modelBuilder.Entity<BookingConversation>()
                .HasIndex(x => x.BookingId).IsUnique();

            // Message: XOR constraint (enforce in app/service layer if DB doesn't support)
            // Two optional FKs: PreBookingConversationId, BookingConversationId
            // DB-level CHECK constraint for XOR (MySQL 8+)
            modelBuilder.Entity<Message>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Message_XOR",
                    "((PreBookingConversationId IS NOT NULL AND BookingConversationId IS NULL) OR (PreBookingConversationId IS NULL AND BookingConversationId IS NOT NULL))"
                ));
            // If migration fails, enforce in service layer and leave a TODO.

            // Review: Unique constraint on (BookingId, ReviewerUserId, TargetType, TargetId)
            // Prevents duplicate reviews for same target on same booking
            modelBuilder.Entity<Review>()
                .HasIndex(x => new { x.BookingId, x.ReviewerUserId, x.TargetType, x.TargetId })
                .IsUnique();

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Booking)
                .WithMany(b => b.Reviews)
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Reviewer)
                .WithMany()
                .HasForeignKey(r => r.ReviewerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .Property(r => r.Rating)
                .IsRequired();

            modelBuilder.Entity<Review>()
                .Property(r => r.TargetId)
                .HasMaxLength(450); // Same as UserId max length

            // ReviewReply: Only author of original review can add replies
            modelBuilder.Entity<ReviewReply>()
                .HasOne(rr => rr.Review)
                .WithMany(r => r.Replies)
                .HasForeignKey(rr => rr.ReviewId)
                .OnDelete(DeleteBehavior.Cascade); // Delete replies when review is deleted

            modelBuilder.Entity<ReviewReply>()
                .HasOne(rr => rr.Author)
                .WithMany()
                .HasForeignKey(rr => rr.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReviewReply>()
                .Property(rr => rr.Message)
                .IsRequired()
                .HasMaxLength(1000);

            // Booking FK configuration (explicit for Participant relationship)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Participant)
                .WithMany()
                .HasForeignKey(b => b.ParticipantUserId)
                .IsRequired();

            // RateLimitCounter: Unique (ParticipantUserId, Date)
            modelBuilder.Entity<RateLimitCounter>()
                .HasIndex(x => new { x.ParticipantUserId, x.Date }).IsUnique();

            // Money columns as decimal(10,2)
            modelBuilder.Entity<TourMotard>()
                .Property(x => x.PricePerSeat)
                .HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Payment>()
                .Property(x => x.Amount)
                .HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Payout>()
                .Property(x => x.PlatformShare)
                .HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Payout>()
                .Property(x => x.MotardShare)
                .HasColumnType("decimal(10,2)");

            // Restrict delete behavior for all FKs (no cascade)
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var fk in entity.GetForeignKeys())
                {
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }

            // IdempotencyKey entity configuration
            modelBuilder.Entity<IdempotencyKey>(b =>
            {
                b.HasKey(i => i.Key);
                b.HasIndex(i => i.Key).IsUnique();
                b.Property(i => i.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
