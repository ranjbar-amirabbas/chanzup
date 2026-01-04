using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chanzup.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    SubscriptionTier = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TokenBalance = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdditionalData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Admins_UserId",
                        column: x => x.UserId,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DisputeResolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReporterType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReporterEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssignedTo = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeResolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeResolutions_Admins_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SuspiciousActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ActivityData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActionTaken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuspiciousActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuspiciousActivities_Admins_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SystemParameters",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    ValidationRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemParameters", x => x.Key);
                    table.ForeignKey(
                        name: "FK_SystemParameters_Admins_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BusinessApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    RequestedTier = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BusinessDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BusinessCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SocialMediaLinks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessApplications_Admins_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Admins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BusinessApplications_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BusinessLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false),
                    QRCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessLocations_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GameType = table.Column<int>(type: "int", nullable: false),
                    TokenCostPerSpin = table.Column<int>(type: "int", nullable: false),
                    MaxSpinsPerDay = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Targeting = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaigns_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staff_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QRSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerLatitude = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: true),
                    PlayerLongitude = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: true),
                    TokensEarned = table.Column<int>(type: "int", nullable: false),
                    SessionHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QRSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QRSessions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QRSessions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferrerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferredPlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferralCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TokensAwarded = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Referrals_Players_ReferredPlayerId",
                        column: x => x.ReferredPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Players_ReferrerId",
                        column: x => x.ReferrerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExternalShareId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TokensAwarded = table.Column<int>(type: "int", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialShares_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenTransactions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisputeMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisputeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SenderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenderType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisputeMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisputeMessages_DisputeResolutions_DisputeId",
                        column: x => x.DisputeId,
                        principalTable: "DisputeResolutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CampaignLocations",
                columns: table => new
                {
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignLocations", x => new { x.CampaignId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_CampaignLocations_BusinessLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "BusinessLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CampaignLocations_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prizes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Value = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true, defaultValue: "CAD"),
                    TotalQuantity = table.Column<int>(type: "int", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    WinProbability = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HasLocationSpecificInventory = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prizes_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffLocationAccess",
                columns: table => new
                {
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permissions = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffLocationAccess", x => new { x.StaffId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_StaffLocationAccess_BusinessLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "BusinessLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffLocationAccess_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerPrizes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RedemptionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsRedeemed = table.Column<bool>(type: "bit", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerPrizes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerPrizes_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerPrizes_Prizes_PrizeId",
                        column: x => x.PrizeId,
                        principalTable: "Prizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrizeLocationInventories",
                columns: table => new
                {
                    PrizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalQuantity = table.Column<int>(type: "int", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrizeLocationInventories", x => new { x.PrizeId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_PrizeLocationInventories_BusinessLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "BusinessLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PrizeLocationInventories_Prizes_PrizeId",
                        column: x => x.PrizeId,
                        principalTable: "Prizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WheelSpins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrizeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TokensSpent = table.Column<int>(type: "int", nullable: false),
                    SpinResult = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RandomSeed = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WheelSpins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WheelSpins_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WheelSpins_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WheelSpins_Prizes_PrizeId",
                        column: x => x.PrizeId,
                        principalTable: "Prizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admins_Email",
                table: "Admins",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_IsActive_Role",
                table: "Admins",
                columns: new[] { "IsActive", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_BusinessId_EventType_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "BusinessId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_CampaignId_EventType_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "CampaignId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EventType_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_PlayerId_EventType_CreatedAt",
                table: "AnalyticsEvents",
                columns: new[] { "PlayerId", "EventType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "Action", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserEmail_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserEmail", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserType_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserType", "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessApplications_BusinessId",
                table: "BusinessApplications",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessApplications_Email",
                table: "BusinessApplications",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessApplications_ReviewedBy_ReviewedAt",
                table: "BusinessApplications",
                columns: new[] { "ReviewedBy", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessApplications_Status_SubmittedAt",
                table: "BusinessApplications",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Email",
                table: "Businesses",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_IsActive_SubscriptionTier",
                table: "Businesses",
                columns: new[] { "IsActive", "SubscriptionTier" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocations_BusinessId_IsActive",
                table: "BusinessLocations",
                columns: new[] { "BusinessId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocations_QRCode",
                table: "BusinessLocations",
                column: "QRCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignLocations_CampaignId",
                table: "CampaignLocations",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignLocations_LocationId",
                table: "CampaignLocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_BusinessId_IsActive",
                table: "Campaigns",
                columns: new[] { "BusinessId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_IsActive_StartDate_EndDate",
                table: "Campaigns",
                columns: new[] { "IsActive", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_DisputeId_CreatedAt",
                table: "DisputeMessages",
                columns: new[] { "DisputeId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DisputeMessages_SenderId_SenderType",
                table: "DisputeMessages",
                columns: new[] { "SenderId", "SenderType" });

            migrationBuilder.CreateIndex(
                name: "IX_DisputeResolutions_AssignedTo_Status",
                table: "DisputeResolutions",
                columns: new[] { "AssignedTo", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DisputeResolutions_ReporterId_ReporterType",
                table: "DisputeResolutions",
                columns: new[] { "ReporterId", "ReporterType" });

            migrationBuilder.CreateIndex(
                name: "IX_DisputeResolutions_Status_Priority_CreatedAt",
                table: "DisputeResolutions",
                columns: new[] { "Status", "Priority", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DisputeResolutions_Type_Status",
                table: "DisputeResolutions",
                columns: new[] { "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPrizes_ExpiresAt_IsRedeemed",
                table: "PlayerPrizes",
                columns: new[] { "ExpiresAt", "IsRedeemed" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPrizes_PlayerId_IsRedeemed",
                table: "PlayerPrizes",
                columns: new[] { "PlayerId", "IsRedeemed" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPrizes_PrizeId",
                table: "PlayerPrizes",
                column: "PrizeId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerPrizes_RedemptionCode",
                table: "PlayerPrizes",
                column: "RedemptionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Email",
                table: "Players",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_IsActive",
                table: "Players",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeLocationInventories_LocationId",
                table: "PrizeLocationInventories",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrizeLocationInventories_LocationId_RemainingQuantity",
                table: "PrizeLocationInventories",
                columns: new[] { "LocationId", "RemainingQuantity" });

            migrationBuilder.CreateIndex(
                name: "IX_PrizeLocationInventories_PrizeId",
                table: "PrizeLocationInventories",
                column: "PrizeId");

            migrationBuilder.CreateIndex(
                name: "IX_Prizes_CampaignId_IsActive",
                table: "Prizes",
                columns: new[] { "CampaignId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Prizes_RemainingQuantity_IsActive",
                table: "Prizes",
                columns: new[] { "RemainingQuantity", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_QRSessions_BusinessId_CreatedAt",
                table: "QRSessions",
                columns: new[] { "BusinessId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QRSessions_PlayerId_CreatedAt",
                table: "QRSessions",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QRSessions_SessionHash",
                table: "QRSessions",
                column: "SessionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferralCode",
                table: "Referrals",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredPlayerId_IsCompleted",
                table: "Referrals",
                columns: new[] { "ReferredPlayerId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerId_IsCompleted",
                table: "Referrals",
                columns: new[] { "ReferrerId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "ExpiresAt", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_UserType_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "UserType", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialShares_ExternalShareId",
                table: "SocialShares",
                column: "ExternalShareId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialShares_Platform_IsVerified",
                table: "SocialShares",
                columns: new[] { "Platform", "IsVerified" });

            migrationBuilder.CreateIndex(
                name: "IX_SocialShares_PlayerId_CreatedAt",
                table: "SocialShares",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_BusinessId_Email",
                table: "Staff",
                columns: new[] { "BusinessId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staff_BusinessId_IsActive",
                table: "Staff",
                columns: new[] { "BusinessId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffLocationAccess_LocationId",
                table: "StaffLocationAccess",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffLocationAccess_LocationId_IsActive",
                table: "StaffLocationAccess",
                columns: new[] { "LocationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffLocationAccess_StaffId",
                table: "StaffLocationAccess",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffLocationAccess_StaffId_IsActive",
                table: "StaffLocationAccess",
                columns: new[] { "StaffId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivities_ActivityType_DetectedAt",
                table: "SuspiciousActivities",
                columns: new[] { "ActivityType", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivities_ReviewedBy_ReviewedAt",
                table: "SuspiciousActivities",
                columns: new[] { "ReviewedBy", "ReviewedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivities_Status_Severity_DetectedAt",
                table: "SuspiciousActivities",
                columns: new[] { "Status", "Severity", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SuspiciousActivities_UserId_UserType_DetectedAt",
                table: "SuspiciousActivities",
                columns: new[] { "UserId", "UserType", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemParameters_Category",
                table: "SystemParameters",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemParameters_Category_Key",
                table: "SystemParameters",
                columns: new[] { "Category", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemParameters_UpdatedBy",
                table: "SystemParameters",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_PlayerId_CreatedAt",
                table: "TokenTransactions",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TokenTransactions_Type_CreatedAt",
                table: "TokenTransactions",
                columns: new[] { "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WheelSpins_CampaignId_CreatedAt",
                table: "WheelSpins",
                columns: new[] { "CampaignId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WheelSpins_PlayerId_CreatedAt",
                table: "WheelSpins",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WheelSpins_PrizeId",
                table: "WheelSpins",
                column: "PrizeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsEvents");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BusinessApplications");

            migrationBuilder.DropTable(
                name: "CampaignLocations");

            migrationBuilder.DropTable(
                name: "DisputeMessages");

            migrationBuilder.DropTable(
                name: "PlayerPrizes");

            migrationBuilder.DropTable(
                name: "PrizeLocationInventories");

            migrationBuilder.DropTable(
                name: "QRSessions");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SocialShares");

            migrationBuilder.DropTable(
                name: "StaffLocationAccess");

            migrationBuilder.DropTable(
                name: "SuspiciousActivities");

            migrationBuilder.DropTable(
                name: "SystemParameters");

            migrationBuilder.DropTable(
                name: "TokenTransactions");

            migrationBuilder.DropTable(
                name: "WheelSpins");

            migrationBuilder.DropTable(
                name: "DisputeResolutions");

            migrationBuilder.DropTable(
                name: "BusinessLocations");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Prizes");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "Businesses");
        }
    }
}
