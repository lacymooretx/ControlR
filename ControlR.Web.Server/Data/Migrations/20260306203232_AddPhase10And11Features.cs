using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlR.Web.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase10And11Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomationSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SuggestionType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SuggestedScriptId = table.Column<Guid>(type: "uuid", nullable: true),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AutomationSuggestions_SavedScripts_SuggestedScriptId",
                        column: x => x.SuggestedScriptId,
                        principalTable: "SavedScripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AutomationSuggestions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrandingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LogoFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    LogoStoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FaviconFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandingSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandingSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JitAdminAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JitAdminAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JitAdminAccounts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatchInstallations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InstalledCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchInstallations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatchInstallations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingPatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InstalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    IsImportant = table.Column<bool>(type: "boolean", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdateId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingPatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingPatches_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PluginRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AssemblyPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PluginTypeName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "text", nullable: true),
                    LastLoadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PluginRegistrations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRecordings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    FrameCount = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecorderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecorderUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SessionEndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StorageSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRecordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRecordings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupportSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AccessCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ClientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClientEmail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SessionEndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SessionStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportSessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketingIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EncryptedApiKey = table.Column<string>(type: "text", nullable: false),
                    DefaultProject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FieldMappingJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketingIntegrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketingIntegrations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ExternalTicketId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalTicketUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlertId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketLinks_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToolboxItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sha256Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DeploymentCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolboxItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolboxItems_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationSuggestions_DeviceId",
                table: "AutomationSuggestions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationSuggestions_Status",
                table: "AutomationSuggestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationSuggestions_SuggestedScriptId",
                table: "AutomationSuggestions",
                column: "SuggestedScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationSuggestions_TenantId",
                table: "AutomationSuggestions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BrandingSettings_TenantId",
                table: "BrandingSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JitAdminAccounts_DeviceId",
                table: "JitAdminAccounts",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_JitAdminAccounts_ExpiresAt",
                table: "JitAdminAccounts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_JitAdminAccounts_Status",
                table: "JitAdminAccounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JitAdminAccounts_TenantId",
                table: "JitAdminAccounts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PatchInstallations_DeviceId",
                table: "PatchInstallations",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PatchInstallations_Status",
                table: "PatchInstallations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PatchInstallations_TenantId",
                table: "PatchInstallations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPatches_DeviceId_UpdateId",
                table: "PendingPatches",
                columns: new[] { "DeviceId", "UpdateId" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingPatches_Status",
                table: "PendingPatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PendingPatches_TenantId",
                table: "PendingPatches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PluginRegistrations_Name_TenantId",
                table: "PluginRegistrations",
                columns: new[] { "Name", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PluginRegistrations_TenantId",
                table: "PluginRegistrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRecordings_DeviceId",
                table: "SessionRecordings",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRecordings_SessionId",
                table: "SessionRecordings",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRecordings_TenantId",
                table: "SessionRecordings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_AccessCode_TenantId",
                table: "SupportSessions",
                columns: new[] { "AccessCode", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_Status",
                table: "SupportSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_TenantId",
                table: "SupportSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketingIntegrations_Name_TenantId",
                table: "TicketingIntegrations",
                columns: new[] { "Name", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketingIntegrations_TenantId",
                table: "TicketingIntegrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketLinks_AlertId",
                table: "TicketLinks",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketLinks_DeviceId",
                table: "TicketLinks",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketLinks_TenantId",
                table: "TicketLinks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolboxItems_Name_TenantId",
                table: "ToolboxItems",
                columns: new[] { "Name", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolboxItems_TenantId",
                table: "ToolboxItems",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationSuggestions");

            migrationBuilder.DropTable(
                name: "BrandingSettings");

            migrationBuilder.DropTable(
                name: "JitAdminAccounts");

            migrationBuilder.DropTable(
                name: "PatchInstallations");

            migrationBuilder.DropTable(
                name: "PendingPatches");

            migrationBuilder.DropTable(
                name: "PluginRegistrations");

            migrationBuilder.DropTable(
                name: "SessionRecordings");

            migrationBuilder.DropTable(
                name: "SupportSessions");

            migrationBuilder.DropTable(
                name: "TicketingIntegrations");

            migrationBuilder.DropTable(
                name: "TicketLinks");

            migrationBuilder.DropTable(
                name: "ToolboxItems");
        }
    }
}
