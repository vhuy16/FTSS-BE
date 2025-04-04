﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FTSS_Model.Entities;

public partial class FtssContext : DbContext
{
    public FtssContext()
    {
    }

    public FtssContext(DbContextOptions<FtssContext> options)
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

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Otp> Otps { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ServicePackage> ServicePackages { get; set; }

    public virtual DbSet<SetupPackage> SetupPackages { get; set; }

    public virtual DbSet<SetupPackageDetail> SetupPackageDetails { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<Solution> Solutions { get; set; }

    public virtual DbSet<SubCategory> SubCategories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=14.225.220.28;Database=FTSS;User Id=sa;Password=0363919179aN;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3213E83F2B701192");

            entity.ToTable("Booking");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
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
            entity.HasKey(e => e.Id).HasName("PK__Issue__3213E83F71FEC19D");

            entity.ToTable("Issue");

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
            entity.Property(e => e.IssueName)
                .HasMaxLength(255)
                .HasColumnName("issueName");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.IssueCategory).WithMany(p => p.Issues)
                .HasForeignKey(d => d.IssueCategoryId)
                .HasConstraintName("FK_Issue_IssueCategory");
        });

        modelBuilder.Entity<Mission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3213E83F3561400D");

            entity.ToTable("Mission");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.BookingId).HasColumnName("bookingId");
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

            entity.HasOne(d => d.User).WithMany(p => p.Missions)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("FK_MaintenanceTask_User");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Order__3213E83F7F61BE3E");

            entity.ToTable("Order");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
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

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.AmountPaid)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amountPaid");
            entity.Property(e => e.BankHolder)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bankHolder");
            entity.Property(e => e.BankName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("bankName");
            entity.Property(e => e.BankNumber).HasColumnName("bankNumber");
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
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payment__orderId__17036CC0");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Product__3213E83F4D0F84D3");

            entity.ToTable("Product");

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

        modelBuilder.Entity<ServicePackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ServiceP__3213E83FA4E5AED8");

            entity.ToTable("ServicePackage");

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

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Shipment__3213E83FF6F447A2");

            entity.ToTable("Shipment");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.DeliveryAt)
                .HasMaxLength(100)
                .HasColumnName("deliveryAt");
            entity.Property(e => e.DeliveryDate)
                .HasColumnType("datetime")
                .HasColumnName("deliveryDate");
            entity.Property(e => e.DeliveryStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("deliveryStatus");
            entity.Property(e => e.OrderId).HasColumnName("orderId");
            entity.Property(e => e.ShippingAddress)
                .HasMaxLength(255)
                .HasColumnName("shippingAddress");
            entity.Property(e => e.ShippingFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shippingFee");
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("trackingNumber");

            entity.HasOne(d => d.Order).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Shipment__orderI__1BC821DD");
        });

        modelBuilder.Entity<Solution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Solution__3213E83FE0CC4A1A");

            entity.ToTable("Solution");

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
                .HasConstraintName("FK__Solution__issueI__31B762FC");
        });

        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SubCateg__3213E83F011365E6");

            entity.ToTable("SubCategory");

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

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.CreateDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createDate");
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
