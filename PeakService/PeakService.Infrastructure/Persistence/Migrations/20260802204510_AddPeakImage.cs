using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeakService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeakImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "peaks",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "peaks");
        }
    }
}
