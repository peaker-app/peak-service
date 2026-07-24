using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using NpgsqlTypes;

#nullable disable

namespace PeakService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPeakSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.Sql(
                """
                CREATE OR REPLACE FUNCTION immutable_unaccent(text)
                RETURNS text
                LANGUAGE sql IMMUTABLE PARALLEL SAFE STRICT
                AS $$ SELECT unaccent('unaccent', $1) $$;
                """);

            migrationBuilder.CreateTable(
                name: "mountain_ranges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_range_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mountain_ranges", x => x.id);
                    table.ForeignKey(
                        name: "FK_mountain_ranges_mountain_ranges_parent_range_id",
                        column: x => x.parent_range_id,
                        principalTable: "mountain_ranges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "peaks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wikidata_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    altitude_m = table.Column<int>(type: "integer", nullable: false),
                    prominence_m = table.Column<int>(type: "integer", nullable: true),
                    location = table.Column<Point>(type: "geography (Point,4326)", nullable: false),
                    country_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: true),
                    region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    range_id = table.Column<Guid>(type: "uuid", nullable: true),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('simple', immutable_unaccent(name))", stored: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peaks", x => x.id);
                    table.ForeignKey(
                        name: "FK_peaks_mountain_ranges_range_id",
                        column: x => x.range_id,
                        principalTable: "mountain_ranges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "peak_names",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_official = table.Column<bool>(type: "boolean", nullable: false),
                    peak_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peak_names", x => x.id);
                    table.ForeignKey(
                        name: "FK_peak_names_peaks_peak_id",
                        column: x => x.peak_id,
                        principalTable: "peaks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mountain_ranges_parent_range_id",
                table: "mountain_ranges",
                column: "parent_range_id");

            migrationBuilder.CreateIndex(
                name: "ix_peak_names_peak",
                table: "peak_names",
                column: "peak_id");

            migrationBuilder.CreateIndex(
                name: "ux_peak_names_unique",
                table: "peak_names",
                columns: new[] { "peak_id", "language_code", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_peaks_altitude",
                table: "peaks",
                column: "altitude_m",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_peaks_country",
                table: "peaks",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "ix_peaks_location",
                table: "peaks",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_peaks_range_id",
                table: "peaks",
                column: "range_id");

            migrationBuilder.CreateIndex(
                name: "ix_peaks_search",
                table: "peaks",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ux_peaks_wikidata",
                table: "peaks",
                column: "wikidata_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "peak_names");

            migrationBuilder.DropTable(
                name: "peaks");

            migrationBuilder.DropTable(
                name: "mountain_ranges");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS immutable_unaccent(text);");
        }
    }
}
