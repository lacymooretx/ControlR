using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlR.Web.Server.Data.Migrations
{
  /// <inheritdoc />
  public partial class AddPatExpiryAndRevocation : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AddColumn<DateTimeOffset>(
        name: "ExpiresAt",
        table: "PersonalAccessTokens",
        type: "timestamp with time zone",
        nullable: true);

      migrationBuilder.AddColumn<DateTimeOffset>(
        name: "RevokedAt",
        table: "PersonalAccessTokens",
        type: "timestamp with time zone",
        nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropColumn(
        name: "ExpiresAt",
        table: "PersonalAccessTokens");

      migrationBuilder.DropColumn(
        name: "RevokedAt",
        table: "PersonalAccessTokens");
    }
  }
}
