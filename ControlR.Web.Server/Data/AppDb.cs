using ControlR.Web.Server.Authz.Roles;
using ControlR.Web.Server.Data.Configuration;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ControlR.Web.Server.Data;

public class AppDb : IdentityDbContext<AppUser, AppRole, Guid>, IDataProtectionKeyContext
{
  private readonly Guid? _tenantId;
  private readonly Guid? _userId;

  public AppDb(DbContextOptions<AppDb> options) : base(options)
  {
    var extension = options.FindExtension<ClaimsDbContextOptionsExtension>();
    _tenantId = extension?.Options.TenantId;
    _userId = extension?.Options.UserId;
  }

  public DbSet<AgentInstallerKeyUsage> AgentInstallerKeyUsages { get; init; }
  public DbSet<AgentInstallerKey> AgentInstallerKeys { get; init; }
  public DbSet<AlertRule> AlertRules { get; init; }
  public DbSet<Alert> Alerts { get; init; }
  public DbSet<AuditLog> AuditLogs { get; init; }
  public DbSet<ClientDeviceAssignment> ClientDeviceAssignments { get; init; }
  public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
  public DbSet<DeviceGroup> DeviceGroups { get; init; }
  public DbSet<DeviceMetricSnapshot> DeviceMetricSnapshots { get; init; }
  public DbSet<Device> Devices { get; init; }
  public DbSet<InstalledUpdate> InstalledUpdates { get; init; }
  public DbSet<PersonalAccessToken> PersonalAccessTokens { get; init; }
  public DbSet<SavedScript> SavedScripts { get; init; }
  public DbSet<ScheduledTaskExecution> ScheduledTaskExecutions { get; init; }
  public DbSet<ScheduledTask> ScheduledTasks { get; init; }
  public DbSet<ScriptExecutionResult> ScriptExecutionResults { get; init; }
  public DbSet<ScriptExecution> ScriptExecutions { get; init; }
  public DbSet<ServerAlert> ServerAlerts { get; init; }
  public DbSet<SoftwareInventoryItem> SoftwareInventoryItems { get; init; }
  public DbSet<Tag> Tags { get; init; }
  public DbSet<TenantInvite> TenantInvites { get; init; }
  public DbSet<TenantSetting> TenantSettings { get; init; }
  public DbSet<Tenant> Tenants { get; init; }
  public DbSet<UserPreference> UserPreferences { get; init; }
  public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; init; }
  public DbSet<WebhookSubscription> WebhookSubscriptions { get; init; }

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    base.ConfigureConventions(configurationBuilder);
    configurationBuilder.Conventions.Add(_ => new DateTimeOffsetConvention());
    configurationBuilder.Conventions.Add(_ => new EntityBaseConvention());
  }
  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    SeedDatabase(builder);

    ConfigureClientDeviceAssignments(builder);
    ConfigurePersonalAccessTokens(builder);
    ConfigureServerAlert(builder);
    ConfigureTenant(builder);
    ConfigureDeviceGroups(builder);
    ConfigureDevices(builder);
    ConfigureRoles(builder);
    ConfigureTags(builder);
    ConfigureTenantSettings(builder);
    ConfigureUsers(builder);
    ConfigureUserPreferences(builder);
    ConfigureTenantInvites(builder);
    ConfigureAgentInstallerKeys(builder);
    ConfigureAgentInstallerKeyUsages(builder);
    ConfigureAlertRules(builder);
    ConfigureAlerts(builder);
    ConfigureAuditLogs(builder);
    ConfigureDeviceMetricSnapshots(builder);
    ConfigureInstalledUpdates(builder);
    ConfigureSavedScripts(builder);
    ConfigureScheduledTaskExecutions(builder);
    ConfigureScheduledTasks(builder);
    ConfigureScriptExecutions(builder);
    ConfigureScriptExecutionResults(builder);
    ConfigureSoftwareInventoryItems(builder);
    ConfigureWebhookDeliveryLogs(builder);
    ConfigureWebhookSubscriptions(builder);
  }

  private static void ConfigureRoles(ModelBuilder builder)
  {
    builder
      .Entity<AppRole>()
      .HasMany(x => x.UserRoles)
      .WithOne()
      .HasForeignKey(x => x.RoleId);
  }
  private static void ConfigureServerAlert(ModelBuilder builder)
  {
    builder
      .Entity<ServerAlert>()
      .HasKey(x => x.Id);
  }
  private static void SeedDatabase(ModelBuilder builder)
  {
    var builtInRoles = RoleFactory.GetBuiltInRoles();

    builder
        .Entity<AppRole>()
        .HasData(builtInRoles);

    builder
        .Entity<ServerAlert>()
        .HasData(new ServerAlert
        {
          Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
          Message = string.Empty,
          Severity = MessageSeverity.Information,
          IsDismissable = true,
          IsSticky = false,
          IsEnabled = false
        });
  }

  private void ConfigureAgentInstallerKeyUsages(ModelBuilder builder)
  {
    builder
      .Entity<AgentInstallerKeyUsage>()
      .HasKey(x => x.Id);

    if (_tenantId is not null)
    {
      builder
        .Entity<AgentInstallerKeyUsage>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureAgentInstallerKeys(ModelBuilder builder)
  {
    builder
      .Entity<AgentInstallerKey>()
      .HasMany(x => x.Usages)
      .WithOne(x => x.AgentInstallerKey)
      .HasForeignKey(x => x.AgentInstallerKeyId)
      .OnDelete(DeleteBehavior.Cascade);

    if (_tenantId is not null)
    {
      builder
        .Entity<AgentInstallerKey>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureAlertRules(ModelBuilder builder)
  {
    builder
      .Entity<AlertRule>()
      .HasIndex(x => new { x.Name, x.TenantId })
      .IsUnique();

    if (_tenantId is not null)
    {
      builder
        .Entity<AlertRule>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureAlerts(ModelBuilder builder)
  {
    builder
      .Entity<Alert>()
      .HasOne(x => x.AlertRule)
      .WithMany()
      .HasForeignKey(x => x.AlertRuleId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .Entity<Alert>()
      .HasIndex(x => x.Status);

    builder
      .Entity<Alert>()
      .HasIndex(x => x.TriggeredAt);

    if (_tenantId is not null)
    {
      builder
        .Entity<Alert>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureAuditLogs(ModelBuilder builder)
  {
    builder
      .Entity<AuditLog>()
      .HasIndex(x => x.Timestamp);

    builder
      .Entity<AuditLog>()
      .HasIndex(x => x.EventType);

    builder
      .Entity<AuditLog>()
      .HasIndex(x => x.ActorUserId);

    builder
      .Entity<AuditLog>()
      .HasIndex(x => x.TargetDeviceId);

    if (_tenantId is not null)
    {
      builder
        .Entity<AuditLog>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureClientDeviceAssignments(ModelBuilder builder)
  {
    builder
      .Entity<ClientDeviceAssignment>()
      .HasIndex(x => new { x.ClientUserId, x.DeviceId })
      .IsUnique();

    builder
      .Entity<ClientDeviceAssignment>()
      .HasOne(x => x.ClientUser)
      .WithMany()
      .HasForeignKey(x => x.ClientUserId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .Entity<ClientDeviceAssignment>()
      .HasOne(x => x.Device)
      .WithMany()
      .HasForeignKey(x => x.DeviceId)
      .OnDelete(DeleteBehavior.Cascade);

    if (_tenantId is not null)
    {
      builder
        .Entity<ClientDeviceAssignment>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureDeviceGroups(ModelBuilder builder)
  {
    builder
      .Entity<DeviceGroup>()
      .HasIndex(x => new { x.Name, x.TenantId })
      .IsUnique();

    builder
      .Entity<DeviceGroup>()
      .HasOne(x => x.ParentGroup)
      .WithMany(x => x.SubGroups)
      .HasForeignKey(x => x.ParentGroupId)
      .OnDelete(DeleteBehavior.Restrict);

    builder
      .Entity<DeviceGroup>()
      .HasMany(x => x.Devices)
      .WithOne(x => x.DeviceGroup)
      .HasForeignKey(x => x.DeviceGroupId)
      .OnDelete(DeleteBehavior.SetNull);

    if (_tenantId is not null)
    {
      builder
        .Entity<DeviceGroup>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureDeviceMetricSnapshots(ModelBuilder builder)
  {
    builder
      .Entity<DeviceMetricSnapshot>()
      .HasIndex(x => new { x.DeviceId, x.Timestamp });

    if (_tenantId is not null)
    {
      builder
        .Entity<DeviceMetricSnapshot>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureDevices(ModelBuilder builder)
  {
    builder
      .Entity<Device>()
      .OwnsMany(x => x.Drives)
      .ToJson();

    if (_tenantId is not null)
    {
      builder
        .Entity<Device>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureInstalledUpdates(ModelBuilder builder)
  {
    builder
      .Entity<InstalledUpdate>()
      .HasIndex(x => new { x.DeviceId, x.UpdateId });

    if (_tenantId is not null)
    {
      builder
        .Entity<InstalledUpdate>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigurePersonalAccessTokens(ModelBuilder builder)
  {
    builder
      .Entity<PersonalAccessToken>()
      .HasIndex(x => x.HashedKey)
      .IsUnique();

    builder
      .Entity<PersonalAccessToken>()
      .Property(x => x.HashedKey)
      .IsRequired();

    builder
      .Entity<PersonalAccessToken>()
      .HasOne(x => x.User)
      .WithMany(x => x.PersonalAccessTokens)
      .HasForeignKey(x => x.UserId)
      .OnDelete(DeleteBehavior.Cascade);

    if (_userId is not null)
    {
      builder
        .Entity<PersonalAccessToken>()
        .HasQueryFilter(x => x.UserId == _userId);
    }
    else if (_tenantId is not null)
    {
      builder
        .Entity<PersonalAccessToken>()
        .HasQueryFilter(x => x.User != null && x.User.TenantId == _tenantId);
    }
  }
  private void ConfigureSavedScripts(ModelBuilder builder)
  {
    builder
      .Entity<SavedScript>()
      .HasIndex(x => new { x.Name, x.TenantId })
      .IsUnique();

    if (_tenantId is not null)
    {
      builder
        .Entity<SavedScript>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureScheduledTaskExecutions(ModelBuilder builder)
  {
    builder
      .Entity<ScheduledTaskExecution>()
      .HasOne(x => x.ScheduledTask)
      .WithMany()
      .HasForeignKey(x => x.ScheduledTaskId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .Entity<ScheduledTaskExecution>()
      .HasOne(x => x.ScriptExecution)
      .WithMany()
      .HasForeignKey(x => x.ScriptExecutionId)
      .OnDelete(DeleteBehavior.SetNull);

    if (_tenantId is not null)
    {
      builder
        .Entity<ScheduledTaskExecution>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureScheduledTasks(ModelBuilder builder)
  {
    builder
      .Entity<ScheduledTask>()
      .HasIndex(x => new { x.Name, x.TenantId })
      .IsUnique();

    builder
      .Entity<ScheduledTask>()
      .HasOne(x => x.Script)
      .WithMany()
      .HasForeignKey(x => x.ScriptId)
      .OnDelete(DeleteBehavior.SetNull);

    if (_tenantId is not null)
    {
      builder
        .Entity<ScheduledTask>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureScriptExecutionResults(ModelBuilder builder)
  {
    builder
      .Entity<ScriptExecutionResult>()
      .HasOne(x => x.ScriptExecution)
      .WithMany(x => x.Results)
      .HasForeignKey(x => x.ScriptExecutionId)
      .OnDelete(DeleteBehavior.Cascade);

    if (_tenantId is not null)
    {
      builder
        .Entity<ScriptExecutionResult>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureScriptExecutions(ModelBuilder builder)
  {
    builder
      .Entity<ScriptExecution>()
      .HasOne(x => x.Script)
      .WithMany()
      .HasForeignKey(x => x.ScriptId)
      .OnDelete(DeleteBehavior.SetNull);

    if (_tenantId is not null)
    {
      builder
        .Entity<ScriptExecution>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureSoftwareInventoryItems(ModelBuilder builder)
  {
    builder
      .Entity<SoftwareInventoryItem>()
      .HasIndex(x => new { x.DeviceId, x.Name });

    if (_tenantId is not null)
    {
      builder
        .Entity<SoftwareInventoryItem>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureTags(ModelBuilder builder)
  {
    builder
      .Entity<Tag>()
      .HasIndex(x => new { x.Name, x.TenantId })
      .IsUnique();

    if (_tenantId is not null)
    {
      builder
        .Entity<Tag>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureTenant(ModelBuilder builder)
  {
    // Configure cascade delete for all related entities
    builder.Entity<Tenant>()
      .HasMany(t => t.Devices)
      .WithOne(d => d.Tenant)
      .HasForeignKey(d => d.TenantId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<Tenant>()
      .HasMany(t => t.Tags)
      .WithOne(tag => tag.Tenant)
      .HasForeignKey(tag => tag.TenantId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<Tenant>()
      .HasMany(t => t.TenantSettings)
      .WithOne(setting => setting.Tenant)
      .HasForeignKey(setting => setting.TenantId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<Tenant>()
      .HasMany(t => t.Users)
      .WithOne(u => u.Tenant)
      .HasForeignKey(u => u.TenantId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.Entity<Tenant>()
      .HasMany(t => t.TenantInvites)
      .WithOne(invite => invite.Tenant)
      .HasForeignKey(invite => invite.TenantId)
      .OnDelete(DeleteBehavior.Cascade);
  }
  private void ConfigureTenantInvites(ModelBuilder builder)
  {
    builder
      .Entity<TenantInvite>()
      .HasIndex(x => x.ActivationCode);

    if (_tenantId is not null)
    {
      builder
        .Entity<TenantInvite>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureTenantSettings(ModelBuilder builder)
  {
    builder
      .Entity<TenantSetting>()
      .HasIndex(x => new { x.Name, x.TenantId })
      .IsUnique();

    if (_tenantId is not null)
    {
      builder
        .Entity<TenantSetting>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureUserPreferences(ModelBuilder builder)
  {
    builder
      .Entity<UserPreference>()
      .HasIndex(x => new { x.Name, x.UserId })
      .IsUnique();

    if (_userId is not null)
    {
      builder
        .Entity<UserPreference>()
        .HasQueryFilter(x => x.UserId == _userId);
    }
    else if (_tenantId is not null)
    {
      builder
        .Entity<UserPreference>()
        .HasQueryFilter(x => x.User != null && x.User.TenantId == _tenantId);
    }
  }
  private void ConfigureUsers(ModelBuilder builder)
  {
    builder
      .Entity<AppUser>()
      .Property(x => x.CreatedAt)
      .HasDefaultValueSql("CURRENT_TIMESTAMP");

    builder
      .Entity<AppUser>()
      .HasMany(x => x.UserRoles)
      .WithOne()
      .HasForeignKey(x => x.UserId);

    builder
      .Entity<AppUser>()
      .HasMany(x => x.UserPreferences)
      .WithOne(x => x.User)
      .HasForeignKey(x => x.UserId)
      .OnDelete(DeleteBehavior.Cascade);

    if (_tenantId is not null)
    {
      builder
        .Entity<AppUser>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureWebhookDeliveryLogs(ModelBuilder builder)
  {
    builder
      .Entity<WebhookDeliveryLog>()
      .HasOne(x => x.WebhookSubscription)
      .WithMany()
      .HasForeignKey(x => x.WebhookSubscriptionId)
      .OnDelete(DeleteBehavior.Cascade);

    builder
      .Entity<WebhookDeliveryLog>()
      .HasIndex(x => new { x.WebhookSubscriptionId, x.AttemptedAt });

    if (_tenantId is not null)
    {
      builder
        .Entity<WebhookDeliveryLog>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
  private void ConfigureWebhookSubscriptions(ModelBuilder builder)
  {
    builder
      .Entity<WebhookSubscription>()
      .HasIndex(x => new { x.Name, x.TenantId })
      .IsUnique();

    if (_tenantId is not null)
    {
      builder
        .Entity<WebhookSubscription>()
        .HasQueryFilter(x => x.TenantId == _tenantId);
    }
  }
}