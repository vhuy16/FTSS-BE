using System;
using System.Collections.Generic;
using FTSS_Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace FTSS_Model.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingDetail> BookingDetails { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Issue> Issues { get; set; }

    public virtual DbSet<IssueCategory> IssueCategories { get; set; }

    public virtual DbSet<Mission> Missions { get; set; }

    public virtual DbSet<MissionImage> MissionImages { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Otp> Otps { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ReturnRequest> ReturnRequests { get; set; }

    public virtual DbSet<ReturnRequestMedium> ReturnRequestMedia { get; set; }

    public virtual DbSet<ServicePackage> ServicePackages { get; set; }

    public virtual DbSet<SetupPackage> SetupPackages { get; set; }

    public virtual DbSet<SetupPackageDetail> SetupPackageDetails { get; set; }

    public virtual DbSet<Solution> Solutions { get; set; }

    public virtual DbSet<SolutionProduct> SolutionProducts { get; set; }

    public virtual DbSet<SubCategory> SubCategories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=14.225.220.28;Uid=sa;Pwd=0363919179aN;Database=FTSS;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3213E83F2B701192");

            entity.ToTable("Booking");

            entity.HasIndex(e => e.BookingCode, "idx_booking_bookingcode")
                .IsUnique()
                .HasFilter("([bookingCode] IS NOT NULL)");

            entity.HasIndex(e => e.OrderId, "idx_booking_orderid");

            entity.HasIndex(e => e.ScheduleDate, "idx_booking_scheduledate");

            entity.HasIndex(e => e.Status, "idx_booking_status");

            entity.HasIndex(e => e.UserId, "idx_booking_userid");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.BookingCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("bookingCode");
            entity.Property(e => e.BookingImage)
                .IsUnicode(false)
                .HasColumnName("bookingImage");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("fullName");
            entity.Property(e => e.IsAssigned).HasColumnName("isAssigned");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.ScheduleDate)
                .HasColumnType("datetime")
                .HasColumnName("scheduleDate");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("totalPrice");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.Order).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Booking_Order");

            entity.HasOne(d => d.User).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Booking_User");
        });

        modelBuilder.Entity<BookingDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BookingD__3213E83F8426CEBB");

            entity.ToTable("BookingDetail");

            entity.HasIndex(e => e.BookingId, "idx_bookingdetail_bookingid");

            entity.HasIndex(e => e.ServicePackageId, "idx_bookingdetail_servicepackageid");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("bookingId");
            entity.Property(e => e.ServicePackageId).HasColumnName("servicePackageId");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingDetail_Booking");

            entity.HasOne(d => d.ServicePackage).WithMany(p => p.BookingDetails)
                .HasForeignKey(d => d.ServicePackageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BookingDetail_ServicePackage");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cart__3213E83F39ED5C43");

            entity.ToTable("Cart");

            entity.HasIndex(e => e.CreateDate, "idx_cart_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_cart_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_cart_modifydate");

            entity.HasIndex(e => e.Status, "idx_cart_status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cart__userId__59063A47");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CartItem__3213E83F04624BAE");

            entity.ToTable("CartItem");

            entity.HasIndex(e => e.CreateDate, "idx_cartitem_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_cartitem_isdelete");

            entity.HasIndex(e => e.ModifyDate, "idx_cartitem_modifydate");

            entity.HasIndex(e => e.ProductId, "idx_cartitem_productid");

            entity.HasIndex(e => e.Status, "idx_cartitem_status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CartId).HasColumnName("cartId");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItem__cartId__1DB06A4F");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItem__produc__0B91BA14");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Category__3213E83F328E5121");

            entity.ToTable("Category");

            entity.HasIndex(e => e.CategoryName, "idx_category_categoryname");

            entity.HasIndex(e => e.CreateDate, "idx_category_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_category_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_category_modifydate");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(255)
                .HasColumnName("categoryName");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.IsFishTank)
                .HasDefaultValue(false)
                .HasColumnName("isFishTank");
            entity.Property(e => e.IsObligatory)
                .HasDefaultValue(false)
                .HasColumnName("isObligatory");
            entity.Property(e => e.IsSolution).HasColumnName("isSolution");
            entity.Property(e => e.LinkImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasDefaultValue("default_image_link")
                .HasColumnName("linkImage");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Image__3213E83F222143F7");

            entity.ToTable("Image");

            entity.HasIndex(e => new { e.ProductId, e.IsDelete, e.CreateDate }, "IX_Image_ProductId_IsDelete_CreateDate");

            entity.HasIndex(e => e.CreateDate, "idx_image_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_image_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_image_modifydate");

            entity.HasIndex(e => e.Status, "idx_image_status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.LinkImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("linkImage");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.HasOne(d => d.Product).WithMany(p => p.Images)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Image__productId__0C85DE4D");
        });

        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3213E83F8331A313");

            entity.ToTable("Issue");

            entity.HasIndex(e => e.CreateDate, "idx_issue_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_issue_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.IssueName, "idx_issue_issuename");

            entity.HasIndex(e => e.ModifiedDate, "idx_issue_modifieddate");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.IssueCategoryId).HasColumnName("issueCategoryId");
            entity.Property(e => e.IssueImage)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("issueImage");
            entity.Property(e => e.IssueName)
                .HasMaxLength(255)
                .HasColumnName("issueName");
            entity.Property(e => e.ModifiedDate).HasColumnName("modifiedDate");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.IssueCategory).WithMany(p => p.Issues)
                .HasForeignKey(d => d.IssueCategoryId)
                .HasConstraintName("FK_Issue_IssueCategory");
        });

        modelBuilder.Entity<IssueCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__IssueCat__3213E83F148E615E");

            entity.ToTable("IssueCategory");

            entity.HasIndex(e => e.CreateDate, "idx_issuecategory_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_issuecategory_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_issuecategory_modifydate");

            entity.HasIndex(e => e.IssueCategoryName, "idx_issuecategory_name");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.IssueCategoryName)
                .HasMaxLength(255)
                .HasColumnName("issueCategoryName");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
        });

        modelBuilder.Entity<Mission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3213E83F3561400D");

            entity.ToTable("Mission");

            entity.HasIndex(e => e.BookingId, "idx_mission_bookingid");

            entity.HasIndex(e => e.IsDelete, "idx_mission_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.MissionSchedule, "idx_mission_missionschedule");

            entity.HasIndex(e => e.OrderId, "idx_mission_orderid");

            entity.HasIndex(e => e.Status, "idx_mission_status");

            entity.HasIndex(e => e.Userid, "idx_mission_userid");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.BookingId).HasColumnName("bookingId");
            entity.Property(e => e.EndMissionSchedule)
                .HasColumnType("datetime")
                .HasColumnName("endMissionSchedule");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.MissionDescription).HasColumnName("missionDescription");
            entity.Property(e => e.MissionName)
                .HasMaxLength(255)
                .HasColumnName("missionName");
            entity.Property(e => e.MissionSchedule)
                .HasColumnType("datetime")
                .HasColumnName("missionSchedule");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.Booking).WithMany(p => p.Missions)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_Mission_Booking");

            entity.HasOne(d => d.Order).WithMany(p => p.Missions)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_Mission_Order");

            entity.HasOne(d => d.User).WithMany(p => p.Missions)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("FK_MaintenanceTask_User");
        });

        modelBuilder.Entity<MissionImage>(entity =>
        {
            entity.ToTable("MissionImage");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.IsDelete).HasColumnName("isDelete");
            entity.Property(e => e.LinkImage).HasColumnName("linkImage");
            entity.Property(e => e.MissionId).HasColumnName("missionId");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("status");

            entity.HasOne(d => d.Mission).WithMany(p => p.MissionImages)
                .HasForeignKey(d => d.MissionId)
                .HasConstraintName("FK_MissionImage_Mission");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Order__3213E83F7F61BE3E");

            entity.ToTable("Order");

            entity.HasIndex(e => e.CreateDate, "idx_order_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_order_isdelete").HasFilter("([IsDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_order_modifydate");

            entity.HasIndex(e => e.OrderCode, "idx_order_ordercode")
                .IsUnique()
                .HasFilter("([orderCode] IS NOT NULL)");

            entity.HasIndex(e => e.SetupPackageId, "idx_order_setuppackageid");

            entity.HasIndex(e => e.Status, "idx_order_status");

            entity.HasIndex(e => new { e.UserId, e.Status }, "idx_order_userid_status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.InstallationDate)
                .HasColumnType("datetime")
                .HasColumnName("installationDate");
            entity.Property(e => e.IsAssigned).HasColumnName("isAssigned");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.IsEligible).HasColumnName("isEligible");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("orderCode");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.RecipientName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("recipientName");
            entity.Property(e => e.SetupPackageId).HasColumnName("setupPackageId");
            entity.Property(e => e.Shipcost)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shipcost");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("totalPrice");
            entity.Property(e => e.UserId).HasColumnName("userId");
            entity.Property(e => e.VoucherId).HasColumnName("voucherId");

            entity.HasOne(d => d.SetupPackage).WithMany(p => p.Orders)
                .HasForeignKey(d => d.SetupPackageId)
                .HasConstraintName("FK_Order_SetupPackage");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Order__userId__6383C8BA");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Orders)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_Order_Voucher");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderDet__3213E83FB633A48D");

            entity.ToTable("OrderDetail");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__order__14270015");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__produ__151B244E");
        });

        modelBuilder.Entity<Otp>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Otps_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.Otps).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payment__3213E83F00EF1B30");

            entity.ToTable("Payment");

            entity.HasIndex(e => e.BookingId, "idx_payment_bookingid");

            entity.HasIndex(e => e.OrderCode, "idx_payment_ordercode");

            entity.HasIndex(e => e.PaymentDate, "idx_payment_paymentdate");

            entity.HasIndex(e => e.PaymentStatus, "idx_payment_paymentstatus");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.AmountPaid)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amountPaid");
            entity.Property(e => e.BankHolder)
                .HasMaxLength(50)
                .HasColumnName("bankHolder");
            entity.Property(e => e.BankName)
                .HasMaxLength(100)
                .HasColumnName("bankName");
            entity.Property(e => e.BankNumber)
                .HasMaxLength(50)
                .HasColumnName("bankNumber");
            entity.Property(e => e.BookingId).HasColumnName("bookingId");
            entity.Property(e => e.OrderCode).HasColumnName("orderCode");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.PaymentDate)
                .HasColumnType("datetime")
                .HasColumnName("paymentDate");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("paymentMethod");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("paymentStatus");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_Payment_Booking");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__Payment__orderId__17036CC0");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Product__3213E83F4D0F84D3");

            entity.ToTable("Product");

            entity.HasIndex(e => e.CreateDate, "IX_Product_CreateDate");

            entity.HasIndex(e => e.IsDelete, "IX_Product_IsDelete");

            entity.HasIndex(e => e.Price, "IX_Product_Price");

            entity.HasIndex(e => e.ProductName, "IX_Product_ProductName");

            entity.HasIndex(e => e.Status, "IX_Product_Status");

            entity.HasIndex(e => new { e.Status, e.IsDelete, e.CreateDate }, "IX_Product_Status_IsDelete_CreateDate");

            entity.HasIndex(e => e.SubCategoryId, "IX_Product_SubCategoryId");

            entity.HasIndex(e => e.CreateDate, "idx_product_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_product_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_product_modifydate");

            entity.HasIndex(e => e.ProductName, "idx_product_productname");

            entity.HasIndex(e => e.Status, "idx_product_status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("price");
            entity.Property(e => e.ProductName)
                .HasMaxLength(255)
                .HasColumnName("productName");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Size)
                .HasMaxLength(50)
                .HasColumnName("size");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.SubCategoryId).HasColumnName("SubCategoryID");

            entity.HasOne(d => d.SubCategory).WithMany(p => p.Products)
                .HasForeignKey(d => d.SubCategoryId)
                .HasConstraintName("FK_Product_SubCategory");
        });

        modelBuilder.Entity<ReturnRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReturnRe__3214EC07B6838DC5");

            entity.ToTable("ReturnRequest");

            entity.HasIndex(e => e.OrderId, "IX_ReturnRequest_OrderId");

            entity.HasIndex(e => e.UserId, "IX_ReturnRequest_UserId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_ReturnRequest_Order");

            entity.HasOne(d => d.User).WithMany(p => p.ReturnRequests)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_ReturnRequest_User");
        });

        modelBuilder.Entity<ReturnRequestMedium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReturnRe__3214EC07FA170968");

            entity.HasIndex(e => e.ReturnRequestId, "IX_ReturnRequestMedia_ReturnRequestId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.MediaLink).HasMaxLength(255);
            entity.Property(e => e.MediaType).HasMaxLength(50);

            entity.HasOne(d => d.ReturnRequest).WithMany(p => p.ReturnRequestMedia)
                .HasForeignKey(d => d.ReturnRequestId)
                .HasConstraintName("FK_ReturnRequestMedia_ReturnRequest");
        });

        modelBuilder.Entity<ServicePackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ServiceP__3213E83FA4E5AED8");

            entity.ToTable("ServicePackage");

            entity.HasIndex(e => e.IsDelete, "idx_servicepackage_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ServiceName, "idx_servicepackage_servicename");

            entity.HasIndex(e => e.Status, "idx_servicepackage_status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ServiceName)
                .HasMaxLength(255)
                .HasColumnName("serviceName");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
        });

        modelBuilder.Entity<SetupPackage>(entity =>
        {
            entity.ToTable("SetupPackage");

            entity.HasIndex(e => e.CreateDate, "idx_setuppackage_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_setuppackage_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_setuppackage_modifydate");

            entity.HasIndex(e => e.Status, "idx_setuppackage_status");

            entity.HasIndex(e => e.Userid, "idx_setuppackage_userid");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Image).HasColumnName("image");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.ModifyDate).HasColumnName("modifyDate");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SetupName)
                .HasMaxLength(255)
                .HasColumnName("setupName");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.User).WithMany(p => p.SetupPackages)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("FK_SetupPackage_User");
        });

        modelBuilder.Entity<SetupPackageDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SetupPac__3213E83F9D52720B");

            entity.ToTable("SetupPackageDetail");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId).HasColumnName("productId");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SetupPackageId).HasColumnName("setupPackageId");

            entity.HasOne(d => d.Product).WithMany(p => p.SetupPackageDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SetupPack__produ__19DFD96B");

            entity.HasOne(d => d.SetupPackage).WithMany(p => p.SetupPackageDetails)
                .HasForeignKey(d => d.SetupPackageId)
                .HasConstraintName("FK_SetupPackageDetail_SetupPackage");
        });

        modelBuilder.Entity<Solution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Solution__3213E83FE0CC4A1A");

            entity.ToTable("Solution");

            entity.HasIndex(e => e.CreateDate, "idx_solution_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_solution_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifiedDate, "idx_solution_modifieddate");

            entity.HasIndex(e => e.SolutionName, "idx_solution_solutionname");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.IssueId).HasColumnName("issueId");
            entity.Property(e => e.ModifiedDate)
                .HasColumnType("datetime")
                .HasColumnName("modifiedDate");
            entity.Property(e => e.SolutionName)
                .HasMaxLength(255)
                .HasColumnName("solutionName");

            entity.HasOne(d => d.Issue).WithMany(p => p.Solutions)
                .HasForeignKey(d => d.IssueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Solution__issueI__6BAEFA67");
        });

        modelBuilder.Entity<SolutionProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Solution__3214EC07976FE61E");

            entity.ToTable("SolutionProduct");

            entity.HasIndex(e => e.CreateDate, "idx_solutionproduct_createdate");

            entity.HasIndex(e => e.ModifyDate, "idx_solutionproduct_modifydate");

            entity.HasIndex(e => e.ProductId, "idx_solutionproduct_productid");

            entity.HasIndex(e => e.SolutionId, "idx_solutionproduct_solutionid");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifyDate).HasColumnType("datetime");

            entity.HasOne(d => d.Product).WithMany(p => p.SolutionProducts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__SolutionP__Produ__7DCDAAA2");

            entity.HasOne(d => d.Solution).WithMany(p => p.SolutionProducts)
                .HasForeignKey(d => d.SolutionId)
                .HasConstraintName("FK__SolutionP__Solut__7CD98669");
        });

        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SubCateg__3213E83F011365E6");

            entity.ToTable("SubCategory");

            entity.HasIndex(e => e.CreateDate, "idx_subcategory_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_subcategory_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_subcategory_modifydate");

            entity.HasIndex(e => e.SubCategoryName, "idx_subcategory_subcategoryname");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("categoryId");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.SubCategoryName)
                .HasMaxLength(255)
                .HasColumnName("subCategoryName");

            entity.HasOne(d => d.Category).WithMany(p => p.SubCategories)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubCategory_Category");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3213E83FF34271D4");

            entity.ToTable("User");

            entity.HasIndex(e => e.CreateDate, "idx_user_createdate");

            entity.HasIndex(e => e.IsDelete, "idx_user_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_user_modifydate");

            entity.HasIndex(e => e.Role, "idx_user_role");

            entity.HasIndex(e => e.Status, "idx_user_status");

            entity.HasIndex(e => e.UserName, "idx_user_username").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.BankHolder)
                .HasMaxLength(50)
                .HasColumnName("bankHolder");
            entity.Property(e => e.BankName).HasColumnName("bankName");
            entity.Property(e => e.BankNumber)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bankNumber");
            entity.Property(e => e.CityId)
                .HasMaxLength(50)
                .HasColumnName("cityId");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.DistrictId)
                .HasMaxLength(50)
                .HasColumnName("districtId");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasDefaultValue("")
                .HasColumnName("fullName");
            entity.Property(e => e.Gender)
                .HasDefaultValue("")
                .HasColumnName("gender");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phoneNumber");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.UserName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("userName");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Voucher__3213E83F2B4BD0F3");

            entity.ToTable("Voucher");

            entity.HasIndex(e => e.CreateDate, "idx_voucher_createdate");

            entity.HasIndex(e => e.ExpiryDate, "idx_voucher_expirydate");

            entity.HasIndex(e => e.IsDelete, "idx_voucher_isdelete").HasFilter("([isDelete]=(0))");

            entity.HasIndex(e => e.ModifyDate, "idx_voucher_modifydate");

            entity.HasIndex(e => e.Status, "idx_voucher_status");

            entity.HasIndex(e => e.VoucherCode, "idx_voucher_vouchercode").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasDefaultValue("No description");
            entity.Property(e => e.Discount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(10)
                .IsFixedLength()
                .HasColumnName("discountType");
            entity.Property(e => e.ExpiryDate)
                .HasColumnType("datetime")
                .HasColumnName("expiryDate");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.MaximumOrderValue)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("maximumOrderValue");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.VoucherCode)
                .HasMaxLength(50)
                .HasColumnName("voucherCode");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
