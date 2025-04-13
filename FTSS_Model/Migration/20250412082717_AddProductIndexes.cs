using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTSS_Model.Context
{
    /// <inheritdoc />
    public partial class AddProductIndexes : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Product_CreateDate",
                table: "Product",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "IX_Product_IsDelete",
                table: "Product",
                column: "isDelete");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Price",
                table: "Product",
                column: "price");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ProductName",
                table: "Product",
                column: "productName");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Status",
                table: "Product",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_Product_SubCategoryId",
                table: "Product",
                column: "SubCategoryID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Product_CreateDate",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_IsDelete",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_Price",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_ProductName",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_Status",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_SubCategoryId",
                table: "Product");
        }
    }
}