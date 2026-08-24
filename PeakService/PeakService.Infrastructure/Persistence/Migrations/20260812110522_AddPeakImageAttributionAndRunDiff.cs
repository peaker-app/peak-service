using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeakService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeakImageAttributionAndRunDiff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_author",
                table: "peaks",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_credit_url",
                table: "peaks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_license",
                table: "peaks",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_license_url",
                table: "peaks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "peaks_unchanged",
                table: "peak_ingestion_runs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_author",
                table: "peaks");

            migrationBuilder.DropColumn(
                name: "image_credit_url",
                table: "peaks");

            migrationBuilder.DropColumn(
                name: "image_license",
                table: "peaks");

            migrationBuilder.DropColumn(
                name: "image_license_url",
                table: "peaks");

            migrationBuilder.DropColumn(
                name: "peaks_unchanged",
                table: "peak_ingestion_runs");
        }
    }
}
