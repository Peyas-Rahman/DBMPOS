using DBM.POS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DBM.POS.Infrastructure.Migrations;

[DbContext(typeof(POSDbContext))]
[Migration("20260927100000_AddProductAttributeDefaultValues")]
public partial class AddProductAttributeDefaultValues : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DefaultValues",
            table: "ProductAttributes",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DefaultValues", table: "ProductAttributes");
    }
}
