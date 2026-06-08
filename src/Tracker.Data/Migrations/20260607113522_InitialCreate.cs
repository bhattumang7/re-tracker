using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Tracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Extensions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanguageId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RootPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastScannedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastScannedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Files_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_Milestones_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Milestones_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartLine = table.Column<int>(type: "int", nullable: false),
                    StartColumn = table.Column<int>(type: "int", nullable: false),
                    EndLine = table.Column<int>(type: "int", nullable: false),
                    EndColumn = table.Column<int>(type: "int", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classes_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Methods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: true),
                    CurrentName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReturnType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartLine = table.Column<int>(type: "int", nullable: false),
                    StartColumn = table.Column<int>(type: "int", nullable: false),
                    EndLine = table.Column<int>(type: "int", nullable: false),
                    EndColumn = table.Column<int>(type: "int", nullable: false),
                    BodyStartLine = table.Column<int>(type: "int", nullable: false),
                    BodyStartColumn = table.Column<int>(type: "int", nullable: false),
                    BodyEndLine = table.Column<int>(type: "int", nullable: false),
                    BodyEndColumn = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Methods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Methods_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Methods_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MethodCalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallerMethodId = table.Column<int>(type: "int", nullable: false),
                    CalleeMethodId = table.Column<int>(type: "int", nullable: true),
                    RawCalleeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CallLine = table.Column<int>(type: "int", nullable: false),
                    CallColumn = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodCalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodCalls_Methods_CalleeMethodId",
                        column: x => x.CalleeMethodId,
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MethodCalls_Methods_CallerMethodId",
                        column: x => x.CallerMethodId,
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MethodParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    CurrentName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    StartLine = table.Column<int>(type: "int", nullable: false),
                    StartColumn = table.Column<int>(type: "int", nullable: false),
                    EndLine = table.Column<int>(type: "int", nullable: false),
                    EndColumn = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MethodParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MethodParameters_Methods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneMethods",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "int", nullable: false),
                    MethodId = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneMethods", x => new { x.MilestoneId, x.MethodId });
                    table.ForeignKey(
                        name: "FK_MilestoneMethods_Methods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "Methods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MilestoneMethods_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RenameHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MethodId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldStartLine = table.Column<int>(type: "int", nullable: true),
                    OldStartColumn = table.Column<int>(type: "int", nullable: true),
                    NewStartLine = table.Column<int>(type: "int", nullable: true),
                    NewStartColumn = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenameHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RenameHistories_Methods_MethodId",
                        column: x => x.MethodId,
                        principalTable: "Methods",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Id", "DisplayName", "Extensions", "Name" },
                values: new object[,]
                {
                    { 1, "C", ".c,.h", "c" },
                    { 2, "C#", ".cs", "csharp" },
                    { 3, "Java", ".java", "java" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_FileId",
                table: "Classes",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_Files_ProjectId_RelativePath",
                table: "Files",
                columns: new[] { "ProjectId", "RelativePath" },
                unique: true,
                filter: "[RemovedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MethodCalls_CalleeMethodId",
                table: "MethodCalls",
                column: "CalleeMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodCalls_CallerMethodId",
                table: "MethodCalls",
                column: "CallerMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_MethodParameters_MethodId",
                table: "MethodParameters",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Methods_ClassId",
                table: "Methods",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Methods_CurrentName",
                table: "Methods",
                column: "CurrentName");

            migrationBuilder.CreateIndex(
                name: "IX_Methods_FileId_OriginalName",
                table: "Methods",
                columns: new[] { "FileId", "OriginalName" },
                unique: true,
                filter: "[RemovedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Methods_Status",
                table: "Methods",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneMethods_MethodId",
                table: "MilestoneMethods",
                column: "MethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ParentId",
                table: "Milestones",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId",
                table: "Milestones",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_LanguageId",
                table: "Projects",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_RenameHistories_MethodId",
                table: "RenameHistories",
                column: "MethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MethodCalls");

            migrationBuilder.DropTable(
                name: "MethodParameters");

            migrationBuilder.DropTable(
                name: "MilestoneMethods");

            migrationBuilder.DropTable(
                name: "RenameHistories");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "Methods");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Languages");
        }
    }
}
