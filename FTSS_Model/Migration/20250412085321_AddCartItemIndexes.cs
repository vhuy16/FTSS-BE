using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTSS_Model.Context
{
    /// <inheritdoc />
    public partial class AddCartItemIndexes : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_cartitem_isdelete",
                table: "CartItem");

            migrationBuilder.DropIndex(
                name: "idx_cartitem_modifydate",
                table: "CartItem");

            migrationBuilder.RenameIndex(
                name: "idx_cartitem_status",
                table: "CartItem",
                newName: "IX_CartItem_Status");

   
            migrationBuilder.RenameIndex(
                name: "idx_cartitem_createdate",
                table: "CartItem",
                newName: "IX_CartItem_CreateDate");

            migrationBuilder.RenameIndex(
                name: "idx_cartitem_cartid",
                table: "CartItem",
                newName: "IX_CartItem_CartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId_IsDelete_Status",
                table: "CartItem",
                columns: new[] { "cartId", "isDelete", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_IsDelete",
                table: "CartItem",
                column: "isDelete");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItem_CartId_IsDelete_Status",
                table: "CartItem");

            migrationBuilder.DropIndex(
                name: "IX_CartItem_IsDelete",
                table: "CartItem");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_Status",
                table: "CartItem",
                newName: "idx_cartitem_status");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_ProductId",
                table: "CartItem",
                newName: "idx_cartitem_productid");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_CreateDate",
                table: "CartItem",
                newName: "idx_cartitem_createdate");

            migrationBuilder.RenameIndex(
                name: "IX_CartItem_CartId",
                table: "CartItem",
                newName: "idx_cartitem_cartid");

            migrationBuilder.CreateIndex(
                name: "idx_cartitem_isdelete",
                table: "CartItem",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_cartitem_modifydate",
                table: "CartItem",
                column: "modifyDate");
        }
    }
}
