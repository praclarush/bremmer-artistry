using Microsoft.EntityFrameworkCore;
using PotteryJournal.Infrastructure.Data.Entities;

namespace PotteryJournal.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core database context for the Pottery Journal database.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// The dedicated Postgres schema for this application's tables, per the "default schema
        /// must be the ecosystem name" convention (translated from the SQL Server "no dbo" rule).
        /// </summary>
        public const string Schema = "potteryjournal";

        /// <summary>
        /// Initializes a new instance of <see cref="AppDbContext"/>.
        /// </summary>
        /// <param name="options">The context options supplied by dependency injection.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Piece> Pieces => Set<Piece>();

        public DbSet<PieceNote> PieceNotes => Set<PieceNote>();

        public DbSet<PieceImage> PieceImages => Set<PieceImage>();

        public DbSet<GlazeApplication> GlazeApplications => Set<GlazeApplication>();

        public DbSet<ClayBody> ClayBodies => Set<ClayBody>();

        public DbSet<Glaze> Glazes => Set<Glaze>();

        public DbSet<Category> Categories => Set<Category>();

        public DbSet<Collection> Collections => Set<Collection>();

        public DbSet<Event> Events => Set<Event>();

        public DbSet<AllowedAdmin> AllowedAdmins => Set<AllowedAdmin>();

        /// <summary>
        /// Configures the EF Core model, applying every <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/>
        /// in this assembly.
        /// </summary>
        /// <param name="modelBuilder">The model builder supplied by EF Core.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }
    }
}
