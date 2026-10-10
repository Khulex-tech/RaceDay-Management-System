using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    EventTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.EventTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserId = table.Column<int>(type: "int", nullable: false),
                    EventTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.CheckConstraint("CK_Events_DistanceKm", "[DistanceKm] > 0");
                    table.ForeignKey(
                        name: "FK_Events_EventType",
                        column: x => x.EventTypeId,
                        principalTable: "EventTypes",
                        principalColumn: "EventTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Organiser",
                        column: x => x.OrganiserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EntryFee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MinAge = table.Column<int>(type: "int", nullable: false),
                    MaxAge = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                    table.CheckConstraint("CK_Categories_AgeRange", "[MaxAge] >= [MinAge]");
                    table.CheckConstraint("CK_Categories_EntryFee", "[EntryFee] >= 0");
                    table.ForeignKey(
                        name: "FK_Categories_Event",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantId = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    EnrolmentStatus = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentId);
                    table.CheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Pending', 'Confirmed', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Enrolments_Category",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Event",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Participant",
                        column: x => x.ParticipantId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentId = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeSpan>(type: "time(0)", nullable: false),
                    FinishingPosition = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.CheckConstraint("CK_Results_FinishingPosition", "[FinishingPosition] > 0");
                    table.ForeignKey(
                        name: "FK_Results_Enrolment",
                        column: x => x.EnrolmentId,
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "EventTypes",
                columns: new[] { "EventTypeId", "Name" },
                values: new object[,]
                {
                    { 1, "Run" },
                    { 2, "Walk" },
                    { 3, "Cycle" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "Email", "FirstName", "LastName", "PasswordHash", "PhoneNumber", "ProfilePictureUrl", "Role" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "thabo.mokoena@raceday.co.za", "Thabo", "Mokoena", "phJIUg5kdMetJqiOOLuXeg==:cM/visUxQ7Fk85QIadyVmECBnFckBj9aJaK3pxtIM1I=", "0821234567", null, "Organiser" },
                    { 2, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "ayesha.patel@raceday.co.za", "Ayesha", "Patel", "lU1ZVyM42Gonr2+7wqoUog==:PnPxctHVy67EYkpAImK3Dg4uttnbMwflgUew+nNd6jM=", "0837654321", null, "Organiser" },
                    { 3, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "sipho.ndlovu@gmail.com", "Sipho", "Ndlovu", "Xw11JNrUbh0bnTvT1encFw==:Oy3IR3eFdGdLM5urNrY0/0KyhRbCUR2nDPWtvYopKCI=", "0725558899", null, "Participant" },
                    { 4, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "lerato.dlamini@outlook.com", "Lerato", "Dlamini", "fSq5ZvYxJtXrlNDmK3XQeg==:RL7cKqQeSfLIPegWFEfPJoQMHLEabsn7MCpVCS1cl/U=", "0794443322", null, "Participant" },
                    { 5, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "johan.vanwyk@webmail.co.za", "Johan", "van Wyk", "EnjxIFJO6OcQNrT25KjbHg==:2oHPW913wqyLnSNnrUvUjr2rBUOwGBWdoqyiZE38h68=", "0836661177", null, "Participant" },
                    { 6, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "nomvula.zulu@gmail.com", "Nomvula", "Zulu", "UJtpXX988YAxVJIzVcqv1Q==:RUnx7r2W9rxWm01vJGHiCrLctxwSRBDLdD869DZrjL4=", "0713338844", null, "Participant" }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "EventId", "BannerImageUrl", "CreatedAt", "Description", "DistanceKm", "EventDate", "EventTypeId", "Location", "Name", "OrganiserId" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "A tough marathon through the streets of Soweto, passing the Orlando Towers and Vilakazi Street.", 42.20m, new DateTime(2026, 6, 7, 6, 0, 0, 0, DateTimeKind.Unspecified), 1, "Soweto, Johannesburg", "Soweto Marathon 2026", 1 },
                    { 2, null, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "A 109 km cycle race around the Cape Peninsula, including Chapman's Peak and Suikerbossie.", 109.00m, new DateTime(2027, 3, 14, 5, 30, 0, 0, DateTimeKind.Unspecified), 3, "Cape Town CBD", "Cape Town Cycle Tour 2027", 2 },
                    { 3, null, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "A family charity walk along the Golden Mile raising funds for school sports programmes.", 10.00m, new DateTime(2027, 5, 2, 7, 0, 0, 0, DateTimeKind.Unspecified), 2, "uShaka Marine World, Durban", "Durban Beachfront Charity Walk 2027", 1 }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CreatedAt", "Description", "EntryFee", "EventId", "MaxAge", "MinAge", "Name" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Full marathon distance for experienced runners.", 450.00m, 1, 59, 18, "42.2km Marathon" },
                    { 2, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Half marathon distance.", 350.00m, 1, 59, 16, "21.1km Half" },
                    { 3, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Age-group category for young runners.", 250.00m, 1, 19, 12, "Under 20" },
                    { 4, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Age-group category for adult runners.", 400.00m, 1, 39, 20, "Senior" },
                    { 5, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Full Cape Town Cycle Tour route.", 780.00m, 2, 59, 18, "109km Full Route" },
                    { 6, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Shorter scenic cycle route.", 450.00m, 2, 59, 16, "42km Fun Ride" },
                    { 7, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Age-group category for veteran cyclists.", 600.00m, 2, 100, 50, "Veteran" },
                    { 8, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Full charity walk distance.", 120.00m, 3, 59, 12, "10km Walk" },
                    { 9, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Shorter family-friendly walk.", 80.00m, 3, 59, 6, "5km Family Walk" },
                    { 10, new DateTime(2026, 1, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Age-group category for young walkers.", 60.00m, 3, 15, 6, "Youth" }
                });

            migrationBuilder.InsertData(
                table: "Enrolments",
                columns: new[] { "EnrolmentId", "CategoryId", "EnrolledAt", "EnrolmentStatus", "EventId", "ParticipantId" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed", 1, 3 },
                    { 2, 2, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed", 1, 4 },
                    { 3, 1, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed", 1, 5 },
                    { 4, 5, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed", 2, 5 },
                    { 5, 6, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Pending", 2, 3 },
                    { 6, 10, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Confirmed", 3, 6 },
                    { 7, 8, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Pending", 3, 4 },
                    { 8, 9, new DateTime(2026, 2, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Cancelled", 3, 3 }
                });

            migrationBuilder.InsertData(
                table: "Results",
                columns: new[] { "ResultId", "EnrolmentId", "FinishTime", "FinishingPosition", "PublishedAt" },
                values: new object[,]
                {
                    { 1, 1, new TimeSpan(0, 3, 41, 18, 0), 47, new DateTime(2026, 6, 7, 10, 22, 0, 0, DateTimeKind.Unspecified) },
                    { 2, 2, new TimeSpan(0, 1, 58, 4, 0), 129, new DateTime(2026, 6, 7, 9, 58, 0, 0, DateTimeKind.Unspecified) },
                    { 3, 3, new TimeSpan(0, 4, 52, 37, 0), 208, new DateTime(2026, 6, 7, 11, 45, 0, 0, DateTimeKind.Unspecified) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_EventId",
                table: "Categories",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_CategoryId",
                table: "Enrolments",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_ParticipantId",
                table: "Enrolments",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "UQ_Enrolments_Event_Participant",
                table: "Enrolments",
                columns: new[] { "EventId", "ParticipantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventTypeId",
                table: "Events",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserId",
                table: "Events",
                column: "OrganiserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTypes_Name",
                table: "EventTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Results_EnrolmentId",
                table: "Results",
                column: "EnrolmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "EventTypes");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
