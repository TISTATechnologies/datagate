using DataGate.Promotions;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace DataGate.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class DataGateDbContext : AbpDbContext<DataGateDbContext>
{
    public DbSet<PromotionRequest> PromotionRequests => Set<PromotionRequest>();

    public DataGateDbContext(DbContextOptions<DataGateDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Ignore<ApprovalRecord>();

        builder.Entity<PromotionRequest>(b =>
        {
            b.ToTable("PromotionRequests");
            b.ConfigureByConvention();
            b.Ignore(x => x.Approvals);
            b.Property(x => x.RunId).IsRequired().HasMaxLength(128);
            b.Property(x => x.Table).IsRequired().HasMaxLength(256);
            b.Property(x => x.NewColumns).HasMaxLength(1000);
            b.Property(x => x.ReasonSummary).IsRequired().HasMaxLength(2000);
            b.Property(x => x.WorkflowInstanceId).HasMaxLength(64);
            b.HasIndex(x => x.RunId);
            b.HasIndex(x => x.Status);

            // ponytail: public Approvals is IReadOnlyCollection; persist private List via field
            b.OwnsMany<ApprovalRecord>("_approvals", a =>
            {
                a.ToTable("PromotionApprovals");
                a.WithOwner().HasForeignKey("PromotionRequestId");
                a.Property<Guid>("Id");
                a.HasKey("Id");
                a.Property(x => x.ApproverId).IsRequired();
                a.Property(x => x.Tier).IsRequired();
                a.Property(x => x.Comment).HasMaxLength(1000);
                a.Property(x => x.AtUtc).IsRequired();
            });
        });
    }
}
