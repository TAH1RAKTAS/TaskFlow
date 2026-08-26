using Microsoft.EntityFrameworkCore.Migrations; // Migration işlemleri

#nullable disable

namespace TaskFlow.Migrations;

public partial class InitialCreate : Migration // İlk migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable( // Tasks tablosunu oluştur
            name: "Tasks",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false) // Primary Key
                    .Annotation("SqlServer:Identity", "1, 1"), // Otomatik artan ID

                Title = table.Column<string>(type: "nvarchar(max)", nullable: false), // Başlık
                Description = table.Column<string>(type: "nvarchar(max)", nullable: false) // Açıklama
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tasks", x => x.Id); // Primary Key = Id
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Tasks"); // Tasks tablosunu sil
    }
}