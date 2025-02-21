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

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<Issue> Issues { get; set; }

    public virtual DbSet<IssueCategory> IssueCategories { get; set; }

    public virtual DbSet<IssueProduct> IssueProducts { get; set; }

    public virtual DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }

    public virtual DbSet<MaintenanceTask> MaintenanceTasks { get; set; }

    public virtual DbSet<Model3D> Model3Ds { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Otp> Otps { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<SetupPackage> SetupPackages { get; set; }

    public virtual DbSet<SetupPackageDetail> SetupPackageDetails { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<Solution> Solutions { get; set; }

    public virtual DbSet<SubCategory> SubCategories { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=137.59.106.46;database=FTSS;user=ftss_admin;password=admin@1234;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("ftss_admin");

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cart__3213E83F564EAB11");

            entity.ToTable("Cart", "dbo");

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
            entity.HasKey(e => e.Id).HasName("PK__CartItem__3213E83FDB4EF904");

            entity.ToTable("CartItem", "dbo");

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
                .HasConstraintName("FK__CartItem__cartId__0A9D95DB");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CartItem__produc__0B91BA14");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Category__3213E83FAD7360EB");

            entity.ToTable("Category", "dbo");

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
            entity.HasKey(e => e.Id).HasName("PK__Image__3213E83FDB3ABF5A");

            entity.ToTable("Image", "dbo");

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
            entity.HasKey(e => e.Id).HasName("PK__Issue__3213E83FBB9268A5");

            entity.ToTable("Issue", "dbo");

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

        modelBuilder.Entity<IssueCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__IssueCat__3213E83FF17EE3DC");

            entity.ToTable("IssueCategory", "dbo");

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

        modelBuilder.Entity<IssueProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__IssuePro__3213E83F1395F1F5");

            entity.ToTable("IssueProduct", "dbo");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IssueId).HasColumnName("issueId");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
            entity.Property(e => e.ProductId).HasColumnName("productId");

            entity.HasOne(d => d.Issue).WithMany(p => p.IssueProducts)
                .HasForeignKey(d => d.IssueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__IssueProd__issue__0E6E26BF");

            entity.HasOne(d => d.Product).WithMany(p => p.IssueProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__IssueProd__produ__0F624AF8");
        });

        modelBuilder.Entity<MaintenanceSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3213E83F5638FA89");

            entity.ToTable("MaintenanceSchedule", "dbo");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.ScheduleDate)
                .HasColumnType("datetime")
                .HasColumnName("scheduleDate");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("userId");

            entity.HasOne(d => d.User).WithMany(p => p.MaintenanceSchedules)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__userI__3E52440B");
        });

        modelBuilder.Entity<MaintenanceTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3213E83F1BC07285");

            entity.ToTable("MaintenanceTask", "dbo");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.MaintenanceScheduleId).HasColumnName("maintenanceScheduleId");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TaskDescription).HasColumnName("taskDescription");
            entity.Property(e => e.TaskName)
                .HasMaxLength(255)
                .HasColumnName("taskName");

            entity.HasOne(d => d.MaintenanceSchedule).WithMany(p => p.MaintenanceTasks)
                .HasForeignKey(d => d.MaintenanceScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__maint__114A936A");
        });

        modelBuilder.Entity<Model3D>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Model3D__3213E83FC6C2522F");

            entity.ToTable("Model3D", "dbo");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreateDate)
                .HasColumnType("datetime")
                .HasColumnName("createDate");
            entity.Property(e => e.IsDelete)
                .HasDefaultValue(false)
                .HasColumnName("isDelete");
            entity.Property(e => e.Link)
                .HasMaxLength(255)
                .HasColumnName("link");
            entity.Property(e => e.ModifyDate)
                .HasColumnType("datetime")
                .HasColumnName("modifyDate");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Order__3213E83F7F61BE3E");

            entity.ToTable("Order", "dbo");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(50)
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

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Order__userId__6383C8BA");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Orders)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK_Order_Voucher");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OrderDet__3213E83F0E3F7310");

            entity.ToTable("OrderDetail", "dbo");

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
            entity.ToTable("Otps", "dbo");

            entity.HasIndex(e => e.UserId, "IX_Otps_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.Otps).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payment__3213E83FE3F83109");

            entity.ToTable("Payment", "dbo");

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
            entity.Property(e => e.Status)
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

            entity.ToTable("Product", "dbo");

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
            entity.Property(e => e.Model3Did).HasColumnName("model3DId");
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

            entity.HasOne(d => d.Model3D).WithMany(p => p.Products)
                .HasForeignKey(d => d.Model3Did)
                .HasConstraintName("FK_Product_Model3D");

            entity.HasOne(d => d.SubCategory).WithMany(p => p.Products)
                .HasForeignKey(d => d.SubCategoryId)
                .HasConstraintName("FK_Product_SubCategory");
        });

        modelBuilder.Entity<SetupPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SetupPac__3213E83FBCABB90F");

            entity.ToTable("SetupPackage", "dbo");

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
            entity.Property(e => e.ModifyDate).HasColumnName("modifyDate");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.SetupName)
                .HasMaxLength(255)
                .HasColumnName("setupName");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.User).WithMany(p => p.SetupPackages)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("FK_SetupPackage_User");
        });

        modelBuilder.Entity<SetupPackageDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SetupPac__3213E83FBD9416A9");

            entity.ToTable("SetupPackageDetail", "dbo");

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
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SetupPack__setup__1AD3FDA4");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Shipment__3213E83F4E16A7E1");

            entity.ToTable("Shipment", "dbo");

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
            entity.HasKey(e => e.Id).HasName("PK__Solution__3213E83FEDED9B85");

            entity.ToTable("Solution", "dbo");

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
                .HasConstraintName("FK__Solution__issueI__1CBC4616");
        });

        modelBuilder.Entity<SubCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SubCateg__3213E83FF273CA78");

            entity.ToTable("SubCategory", "dbo");

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

            entity.ToTable("User", "dbo");

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

            entity.ToTable("Voucher", "dbo");

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
