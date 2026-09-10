using Microsoft.EntityFrameworkCore;
using ReqLens.Domain;

namespace ReqLens.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<Ambiguity> Ambiguities => Set<Ambiguity>();
    public DbSet<AmbiguityQuestion> AmbiguityQuestions => Set<AmbiguityQuestion>();
    public DbSet<Contradiction> Contradictions => Set<Contradiction>();
    public DbSet<QualityScore> QualityScores => Set<QualityScore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var document = modelBuilder.Entity<Document>();
        document.ToTable("documents");
        document.HasKey(d => d.Id);
        document.Property(d => d.SourceType).HasConversion<string>().HasMaxLength(32);
        document.Property(d => d.FileName).HasMaxLength(255);
        document.Property(d => d.RawContent).IsRequired();
        document.Property(d => d.ExtractedText).IsRequired();
        document.Property(d => d.CreatedAt).IsRequired();
        document.HasMany(d => d.Requirements)
            .WithOne(r => r.Document)
            .HasForeignKey(r => r.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        var requirement = modelBuilder.Entity<Requirement>();
        requirement.ToTable("requirements");
        requirement.HasKey(r => r.Id);
        requirement.Property(r => r.RequirementId).HasMaxLength(32).IsRequired();
        requirement.Property(r => r.Text).IsRequired();
        requirement.HasIndex(r => new { r.DocumentId, r.RequirementId }).IsUnique();

        var ambiguity = modelBuilder.Entity<Ambiguity>();
        ambiguity.ToTable("ambiguities");
        ambiguity.HasKey(a => a.Id);
        ambiguity.Property(a => a.RequirementId).HasMaxLength(32).IsRequired();
        ambiguity.Property(a => a.Severity).HasConversion<string>().HasMaxLength(16);
        ambiguity.Property(a => a.Issue).IsRequired();
        ambiguity.Property(a => a.IsResolved).IsRequired().HasDefaultValue(false);
        ambiguity.Property(a => a.ResolvedSelections).HasMaxLength(1024).IsRequired().HasDefaultValue("");
        ambiguity.HasMany(a => a.Questions)
            .WithOne(q => q.Ambiguity)
            .HasForeignKey(q => q.AmbiguityId)
            .OnDelete(DeleteBehavior.Cascade);

        document.HasMany(d => d.Ambiguities)
            .WithOne(a => a.Document)
            .HasForeignKey(a => a.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        var question = modelBuilder.Entity<AmbiguityQuestion>();
        question.ToTable("ambiguity_questions");
        question.HasKey(q => q.Id);
        question.Property(q => q.Text).IsRequired();
        question.Property(q => q.OptionType).HasMaxLength(32).IsRequired();

        var contradiction = modelBuilder.Entity<Contradiction>();
        contradiction.ToTable("contradictions");
        contradiction.HasKey(c => c.Id);
        contradiction.Property(c => c.RequirementIdA).HasMaxLength(32).IsRequired();
        contradiction.Property(c => c.RequirementIdB).HasMaxLength(32).IsRequired();
        contradiction.Property(c => c.Description).IsRequired();
        contradiction.Property(c => c.ResolutionQuestion).IsRequired();
        contradiction.Property(c => c.IsResolved).IsRequired().HasDefaultValue(false);
        contradiction.Property(c => c.ChosenRequirementId).HasMaxLength(32).IsRequired().HasDefaultValue("");

        document.HasMany(d => d.Contradictions)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        var quality = modelBuilder.Entity<QualityScore>();
        quality.ToTable("quality_scores");
        quality.HasKey(s => s.Id);
        quality.HasIndex(s => s.DocumentId).IsUnique();
        document.HasOne(d => d.QualityScore)
            .WithOne(s => s.Document)
            .HasForeignKey<QualityScore>(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
