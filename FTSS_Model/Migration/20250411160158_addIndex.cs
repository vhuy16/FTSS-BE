using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FTSS_Model.Context
{
    /// <inheritdoc />
    public partial class addIndex : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Indexes for Voucher
            migrationBuilder.CreateIndex(
                name: "idx_voucher_createdate",
                table: "Voucher",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_expirydate",
                table: "Voucher",
                column: "expiryDate");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_isdelete",
                table: "Voucher",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_modifydate",
                table: "Voucher",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_status",
                table: "Voucher",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_voucher_vouchercode",
                table: "Voucher",
                column: "voucherCode",
                unique: true);

            // Indexes for User
            migrationBuilder.CreateIndex(
                name: "idx_user_createdate",
                table: "User",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_user_isdelete",
                table: "User",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_user_modifydate",
                table: "User",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_user_role",
                table: "User",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "idx_user_status",
                table: "User",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_user_username",
                table: "User",
                column: "userName",
                unique: true);

            // Indexes for SubCategory
            migrationBuilder.CreateIndex(
                name: "idx_subcategory_createdate",
                table: "SubCategory",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_subcategory_isdelete",
                table: "SubCategory",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_subcategory_modifydate",
                table: "SubCategory",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_subcategory_subcategoryname",
                table: "SubCategory",
                column: "subCategoryName");

            // Indexes for Solution
            migrationBuilder.CreateIndex(
                name: "idx_solution_createdate",
                table: "Solution",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_solution_isdelete",
                table: "Solution",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_solution_modifieddate",
                table: "Solution",
                column: "modifiedDate");

            migrationBuilder.CreateIndex(
                name: "idx_solution_solutionname",
                table: "Solution",
                column: "solutionName");

           
       
          
        
            // Indexes for SetupPackage
            migrationBuilder.CreateIndex(
                name: "idx_setuppackage_createdate",
                table: "SetupPackage",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_setuppackage_isdelete",
                table: "SetupPackage",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_setuppackage_modifydate",
                table: "SetupPackage",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_setuppackage_status",
                table: "SetupPackage",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_setuppackage_userid",
                table: "SetupPackage",
                column: "userid");

            // Indexes for Product
            migrationBuilder.CreateIndex(
                name: "idx_product_createdate",
                table: "Product",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_product_isdelete",
                table: "Product",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_product_modifydate",
                table: "Product",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_product_productname",
                table: "Product",
                column: "productName");

            migrationBuilder.CreateIndex(
                name: "idx_product_status",
                table: "Product",
                column: "status");

            // Indexes for Payment
            migrationBuilder.CreateIndex(
                name: "idx_payment_bookingid",
                table: "Payment",
                column: "bookingId");

            migrationBuilder.CreateIndex(
                name: "idx_payment_ordercode",
                table: "Payment",
                column: "orderCode");

            migrationBuilder.CreateIndex(
                name: "idx_payment_paymentdate",
                table: "Payment",
                column: "paymentDate");

            migrationBuilder.CreateIndex(
                name: "idx_payment_paymentstatus",
                table: "Payment",
                column: "paymentStatus");

            // Indexes for Order
            migrationBuilder.CreateIndex(
                name: "idx_order_createdate",
                table: "Order",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_order_isdelete",
                table: "Order",
                column: "isDelete",
                filter: "IsDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_order_modifydate",
                table: "Order",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_order_ordercode",
                table: "Order",
                column: "orderCode",
                unique: true,
                filter: "[orderCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_order_setuppackageid",
                table: "Order",
                column: "setupPackageId");

            migrationBuilder.CreateIndex(
                name: "idx_order_status",
                table: "Order",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_order_userid_status",
                table: "Order",
                columns: new[] { "userId", "status" });

            // Indexes for IssueCategory
            migrationBuilder.CreateIndex(
                name: "idx_issuecategory_createdate",
                table: "IssueCategory",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_issuecategory_isdelete",
                table: "IssueCategory",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_issuecategory_modifydate",
                table: "IssueCategory",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_issuecategory_name",
                table: "IssueCategory",
                column: "issueCategoryName");

            // Indexes for Issue
            migrationBuilder.CreateIndex(
                name: "idx_issue_createdate",
                table: "Issue",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_issue_isdelete",
                table: "Issue",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_issue_issuename",
                table: "Issue",
                column: "issueName");

            migrationBuilder.CreateIndex(
                name: "idx_issue_modifieddate",
                table: "Issue",
                column: "modifiedDate");

            // Indexes for Image
            migrationBuilder.CreateIndex(
                name: "idx_image_createdate",
                table: "Image",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_image_isdelete",
                table: "Image",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_image_modifydate",
                table: "Image",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_image_status",
                table: "Image",
                column: "status");

            // Indexes for Category
            migrationBuilder.CreateIndex(
                name: "idx_category_categoryname",
                table: "Category",
                column: "categoryName");

            migrationBuilder.CreateIndex(
                name: "idx_category_createdate",
                table: "Category",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_category_isdelete",
                table: "Category",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_category_modifydate",
                table: "Category",
                column: "modifyDate");

            // Indexes for CartItem
            migrationBuilder.CreateIndex(
                name: "idx_cartitem_createdate",
                table: "CartItem",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_cartitem_isdelete",
                table: "CartItem",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_cartitem_modifydate",
                table: "CartItem",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_cartitem_status",
                table: "CartItem",
                column: "status");

            // Indexes for Cart
            migrationBuilder.CreateIndex(
                name: "idx_cart_createdate",
                table: "Cart",
                column: "createDate");

            migrationBuilder.CreateIndex(
                name: "idx_cart_isdelete",
                table: "Cart",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_cart_modifydate",
                table: "Cart",
                column: "modifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_cart_status",
                table: "Cart",
                column: "status");

            // Indexes for Booking
            migrationBuilder.CreateIndex(
                name: "idx_booking_bookingcode",
                table: "Booking",
                column: "bookingCode",
                unique: true,
                filter: "[bookingCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_booking_orderid",
                table: "Booking",
                column: "orderId");

            migrationBuilder.CreateIndex(
                name: "idx_booking_scheduledate",
                table: "Booking",
                column: "scheduleDate");

            migrationBuilder.CreateIndex(
                name: "idx_booking_status",
                table: "Booking",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_booking_userid",
                table: "Booking",
                column: "userId");

            // Indexes for BookingDetail
            migrationBuilder.CreateIndex(
                name: "idx_bookingdetail_bookingid",
                table: "BookingDetail",
                column: "bookingId");

            migrationBuilder.CreateIndex(
                name: "idx_bookingdetail_servicepackageid",
                table: "BookingDetail",
                column: "servicePackageId");

            // Indexes for Mission
            migrationBuilder.CreateIndex(
                name: "idx_mission_bookingid",
                table: "Mission",
                column: "bookingId");

            migrationBuilder.CreateIndex(
                name: "idx_mission_isdelete",
                table: "Mission",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_mission_missionschedule",
                table: "Mission",
                column: "missionSchedule");

            migrationBuilder.CreateIndex(
                name: "idx_mission_orderid",
                table: "Mission",
                column: "orderId");

            migrationBuilder.CreateIndex(
                name: "idx_mission_status",
                table: "Mission",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_mission_userid",
                table: "Mission",
                column: "userid");

            // Indexes for ServicePackage
            migrationBuilder.CreateIndex(
                name: "idx_servicepackage_isdelete",
                table: "ServicePackage",
                column: "isDelete",
                filter: "isDelete = 0");

            migrationBuilder.CreateIndex(
                name: "idx_servicepackage_servicename",
                table: "ServicePackage",
                column: "serviceName");

            migrationBuilder.CreateIndex(
                name: "idx_servicepackage_status",
                table: "ServicePackage",
                column: "status");

            // Indexes for SolutionProduct
            migrationBuilder.CreateIndex(
                name: "idx_solutionproduct_createdate",
                table: "SolutionProduct",
                column: "CreateDate");

            migrationBuilder.CreateIndex(
                name: "idx_solutionproduct_modifydate",
                table: "SolutionProduct",
                column: "ModifyDate");

            migrationBuilder.CreateIndex(
                name: "idx_solutionproduct_productid",
                table: "SolutionProduct",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "idx_solutionproduct_solutionid",
                table: "SolutionProduct",
                column: "SolutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes for Voucher
            migrationBuilder.DropIndex(
                name: "idx_voucher_createdate",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "idx_voucher_expirydate",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "idx_voucher_isdelete",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "idx_voucher_modifydate",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "idx_voucher_status",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "idx_voucher_vouchercode",
                table: "Voucher");

            // Drop indexes for User
            migrationBuilder.DropIndex(
                name: "idx_user_createdate",
                table: "User");

            migrationBuilder.DropIndex(
                name: "idx_user_isdelete",
                table: "User");

            migrationBuilder.DropIndex(
                name: "idx_user_modifydate",
                table: "User");

            migrationBuilder.DropIndex(
                name: "idx_user_role",
                table: "User");

            migrationBuilder.DropIndex(
                name: "idx_user_status",
                table: "User");

            migrationBuilder.DropIndex(
                name: "idx_user_username",
                table: "User");

            // Drop indexes for SubCategory
            migrationBuilder.DropIndex(
                name: "idx_subcategory_createdate",
                table: "SubCategory");

            migrationBuilder.DropIndex(
                name: "idx_subcategory_isdelete",
                table: "SubCategory");

            migrationBuilder.DropIndex(
                name: "idx_subcategory_modifydate",
                table: "SubCategory");

            migrationBuilder.DropIndex(
                name: "idx_subcategory_subcategoryname",
                table: "SubCategory");

            // Drop indexes for Solution
            migrationBuilder.DropIndex(
                name: "idx_solution_createdate",
                table: "Solution");

            migrationBuilder.DropIndex(
                name: "idx_solution_isdelete",
                table: "Solution");

            migrationBuilder.DropIndex(
                name: "idx_solution_modifieddate",
                table: "Solution");

            migrationBuilder.DropIndex(
                name: "idx_solution_solutionname",
                table: "Solution");

            
            // Drop indexes for SetupPackage
            migrationBuilder.DropIndex(
                name: "idx_setuppackage_createdate",
                table: "SetupPackage");

            migrationBuilder.DropIndex(
                name: "idx_setuppackage_isdelete",
                table: "SetupPackage");

            migrationBuilder.DropIndex(
                name: "idx_setuppackage_modifydate",
                table: "SetupPackage");

            migrationBuilder.DropIndex(
                name: "idx_setuppackage_status",
                table: "SetupPackage");

            migrationBuilder.DropIndex(
                name: "idx_setuppackage_userid",
                table: "SetupPackage");

            // Drop indexes for Product
            migrationBuilder.DropIndex(
                name: "idx_product_createdate",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "idx_product_isdelete",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "idx_product_modifydate",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "idx_product_productname",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "idx_product_status",
                table: "Product");

            // Drop indexes for Payment
            migrationBuilder.DropIndex(
                name: "idx_payment_bookingid",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "idx_payment_ordercode",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "idx_payment_paymentdate",
                table: "Payment");

            migrationBuilder.DropIndex(
                name: "idx_payment_paymentstatus",
                table: "Payment");

            // Drop indexes for Order
            migrationBuilder.DropIndex(
                name: "idx_order_createdate",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "idx_order_isdelete",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "idx_order_modifydate",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "idx_order_ordercode",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "idx_order_setuppackageid",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "idx_order_status",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "idx_order_userid_status",
                table: "Order");

            // Drop indexes for IssueCategory
            migrationBuilder.DropIndex(
                name: "idx_issuecategory_createdate",
                table: "IssueCategory");

            migrationBuilder.DropIndex(
                name: "idx_issuecategory_isdelete",
                table: "IssueCategory");

            migrationBuilder.DropIndex(
                name: "idx_issuecategory_modifydate",
                table: "IssueCategory");

            migrationBuilder.DropIndex(
                name: "idx_issuecategory_name",
                table: "IssueCategory");

            // Drop indexes for Issue
            migrationBuilder.DropIndex(
                name: "idx_issue_createdate",
                table: "Issue");

            migrationBuilder.DropIndex(
                name: "idx_issue_isdelete",
                table: "Issue");

            migrationBuilder.DropIndex(
                name: "idx_issue_issuename",
                table: "Issue");

            migrationBuilder.DropIndex(
                name: "idx_issue_modifieddate",
                table: "Issue");

            // Drop indexes for Image
            migrationBuilder.DropIndex(
                name: "idx_image_createdate",
                table: "Image");

            migrationBuilder.DropIndex(
                name: "idx_image_isdelete",
                table: "Image");

            migrationBuilder.DropIndex(
                name: "idx_image_modifydate",
                table: "Image");

            migrationBuilder.DropIndex(
                name: "idx_image_status",
                table: "Image");

            // Drop indexes for Category
            migrationBuilder.DropIndex(
                name: "idx_category_categoryname",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "idx_category_createdate",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "idx_category_isdelete",
                table: "Category");

            migrationBuilder.DropIndex(
                name: "idx_category_modifydate",
                table: "Category");

            // Drop indexes for CartItem
            migrationBuilder.DropIndex(
                name: "idx_cartitem_createdate",
                table: "CartItem");

            migrationBuilder.DropIndex(
                name: "idx_cartitem_isdelete",
                table: "CartItem");

            migrationBuilder.DropIndex(
                name: "idx_cartitem_modifydate",
                table: "CartItem");

            migrationBuilder.DropIndex(
                name: "idx_cartitem_status",
                table: "CartItem");

            // Drop indexes for Cart
            migrationBuilder.DropIndex(
                name: "idx_cart_createdate",
                table: "Cart");

            migrationBuilder.DropIndex(
                name: "idx_cart_isdelete",
                table: "Cart");

            migrationBuilder.DropIndex(
                name: "idx_cart_modifydate",
                table: "Cart");

            migrationBuilder.DropIndex(
                name: "idx_cart_status",
                table: "Cart");

            // Drop indexes for Booking
            migrationBuilder.DropIndex(
                name: "idx_booking_bookingcode",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "idx_booking_orderid",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "idx_booking_scheduledate",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "idx_booking_status",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "idx_booking_userid",
                table: "Booking");

            // Drop indexes for BookingDetail
            migrationBuilder.DropIndex(
                name: "idx_bookingdetail_bookingid",
                table: "BookingDetail");

            migrationBuilder.DropIndex(
                name: "idx_bookingdetail_servicepackageid",
                table: "BookingDetail");

            // Drop indexes for Mission
            migrationBuilder.DropIndex(
                name: "idx_mission_bookingid",
                table: "Mission");

            migrationBuilder.DropIndex(
                name: "idx_mission_isdelete",
                table: "Mission");

            migrationBuilder.DropIndex(
                name: "idx_mission_missionschedule",
                table: "Mission");

            migrationBuilder.DropIndex(
                name: "idx_mission_orderid",
                table: "Mission");

            migrationBuilder.DropIndex(
                name: "idx_mission_status",
                table: "Mission");

            migrationBuilder.DropIndex(
                name: "idx_mission_userid",
                table: "Mission");

            // Drop indexes for ServicePackage
            migrationBuilder.DropIndex(
                name: "idx_servicepackage_isdelete",
                table: "ServicePackage");

            migrationBuilder.DropIndex(
                name: "idx_servicepackage_servicename",
                table: "ServicePackage");

            migrationBuilder.DropIndex(
                name: "idx_servicepackage_status",
                table: "ServicePackage");

            // Drop indexes for SolutionProduct
            migrationBuilder.DropIndex(
                name: "idx_solutionproduct_createdate",
                table: "SolutionProduct");

            migrationBuilder.DropIndex(
                name: "idx_solutionproduct_modifydate",
                table: "SolutionProduct");

            migrationBuilder.DropIndex(
                name: "idx_solutionproduct_productid",
                table: "SolutionProduct");

            migrationBuilder.DropIndex(
                name: "idx_solutionproduct_solutionid",
                table: "SolutionProduct");
        }
    }
}