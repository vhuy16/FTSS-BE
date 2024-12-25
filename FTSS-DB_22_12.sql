USE [master]
GO
/****** Object:  Database [FTSS]    Script Date: 12/22/2024 1:58:12 PM ******/
CREATE DATABASE [FTSS]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'FTSS', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.QHUY\MSSQL\DATA\FTSS.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'FTSS_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.QHUY\MSSQL\DATA\FTSS_log.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [FTSS] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [FTSS].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [FTSS] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [FTSS] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [FTSS] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [FTSS] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [FTSS] SET ARITHABORT OFF 
GO
ALTER DATABASE [FTSS] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [FTSS] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [FTSS] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [FTSS] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [FTSS] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [FTSS] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [FTSS] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [FTSS] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [FTSS] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [FTSS] SET  ENABLE_BROKER 
GO
ALTER DATABASE [FTSS] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [FTSS] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [FTSS] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [FTSS] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [FTSS] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [FTSS] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [FTSS] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [FTSS] SET RECOVERY FULL 
GO
ALTER DATABASE [FTSS] SET  MULTI_USER 
GO
ALTER DATABASE [FTSS] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [FTSS] SET DB_CHAINING OFF 
GO
ALTER DATABASE [FTSS] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [FTSS] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [FTSS] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [FTSS] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
EXEC sys.sp_db_vardecimal_storage_format N'FTSS', N'ON'
GO
ALTER DATABASE [FTSS] SET QUERY_STORE = ON
GO
ALTER DATABASE [FTSS] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [FTSS]
GO
/****** Object:  Table [dbo].[Cart]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cart](
	[id] [uniqueidentifier] NOT NULL,
	[userId] [uniqueidentifier] NOT NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[status] [varchar](50) NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CartItem]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CartItem](
	[id] [uniqueidentifier] NOT NULL,
	[productId] [uniqueidentifier] NOT NULL,
	[cartId] [uniqueidentifier] NOT NULL,
	[quantity] [int] NOT NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[status] [varchar](50) NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Category]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Category](
	[id] [uniqueidentifier] NOT NULL,
	[categoryName] [nvarchar](255) NOT NULL,
	[description] [nvarchar](max) NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Image]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Image](
	[id] [uniqueidentifier] NOT NULL,
	[linkImage] [varchar](255) NOT NULL,
	[productId] [uniqueidentifier] NOT NULL,
	[status] [nvarchar](50) NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Issue]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Issue](
	[id] [uniqueidentifier] NOT NULL,
	[issueName] [nvarchar](255) NULL,
	[title] [nvarchar](255) NOT NULL,
	[description] [nvarchar](max) NULL,
	[createDate] [datetime2](7) NULL,
	[isDelete] [bit] NULL,
	[issueCategoryId] [uniqueidentifier] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[IssueCategory]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[IssueCategory](
	[id] [uniqueidentifier] NOT NULL,
	[issueCategoryName] [nvarchar](255) NOT NULL,
	[description] [nvarchar](max) NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[IssueProduct]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[IssueProduct](
	[id] [uniqueidentifier] NOT NULL,
	[issueId] [uniqueidentifier] NOT NULL,
	[productId] [uniqueidentifier] NOT NULL,
	[description] [nvarchar](max) NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MaintenanceSchedule]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MaintenanceSchedule](
	[id] [uniqueidentifier] NOT NULL,
	[userId] [uniqueidentifier] NOT NULL,
	[scheduleDate] [datetime] NULL,
	[status] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MaintenanceTask]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MaintenanceTask](
	[id] [uniqueidentifier] NOT NULL,
	[maintenanceScheduleId] [uniqueidentifier] NOT NULL,
	[taskName] [nvarchar](255) NOT NULL,
	[taskDescription] [nvarchar](max) NULL,
	[assignedTo] [nvarchar](255) NULL,
	[status] [varchar](50) NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Model3D]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Model3D](
	[id] [uniqueidentifier] NOT NULL,
	[link] [nvarchar](255) NOT NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Order]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Order](
	[id] [uniqueidentifier] NOT NULL,
	[userId] [uniqueidentifier] NOT NULL,
	[totalPrice] [decimal](10, 2) NOT NULL,
	[status] [varchar](50) NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
	[voucherId] [uniqueidentifier] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[OrderDetail]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrderDetail](
	[id] [uniqueidentifier] NOT NULL,
	[orderId] [uniqueidentifier] NOT NULL,
	[productId] [uniqueidentifier] NOT NULL,
	[quantity] [int] NOT NULL,
	[price] [decimal](10, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Payment]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Payment](
	[id] [uniqueidentifier] NOT NULL,
	[orderId] [uniqueidentifier] NOT NULL,
	[paymentMethod] [nvarchar](50) NULL,
	[amountPaid] [decimal](10, 2) NULL,
	[paymentDate] [datetime] NULL,
	[paymentStatus] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product](
	[id] [uniqueidentifier] NOT NULL,
	[productName] [nvarchar](255) NOT NULL,
	[size] [nvarchar](50) NULL,
	[description] [nvarchar](max) NULL,
	[quantity] [int] NULL,
	[categoryId] [uniqueidentifier] NOT NULL,
	[model3DId] [uniqueidentifier] NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
	[status] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SetupPackage]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SetupPackage](
	[id] [uniqueidentifier] NOT NULL,
	[setupName] [nvarchar](255) NOT NULL,
	[description] [nvarchar](max) NULL,
	[price] [decimal](10, 2) NULL,
	[createDate] [datetime2](7) NULL,
	[modifyDate] [datetime2](7) NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SetupPackageDetail]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SetupPackageDetail](
	[id] [uniqueidentifier] NOT NULL,
	[productId] [uniqueidentifier] NOT NULL,
	[setupPackageId] [uniqueidentifier] NOT NULL,
	[quantity] [int] NULL,
	[price] [decimal](10, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Shipment]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Shipment](
	[id] [uniqueidentifier] NOT NULL,
	[orderId] [uniqueidentifier] NOT NULL,
	[shippingAddress] [nvarchar](255) NULL,
	[shippingFee] [decimal](10, 2) NULL,
	[deliveryStatus] [varchar](50) NULL,
	[trackingNumber] [varchar](100) NULL,
	[deliveryDate] [datetime] NULL,
	[deliveryAt] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Solution]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Solution](
	[id] [uniqueidentifier] NOT NULL,
	[solutionName] [nvarchar](255) NOT NULL,
	[description] [nvarchar](max) NULL,
	[createDate] [datetime] NULL,
	[modifiedDate] [datetime] NULL,
	[isDelete] [bit] NULL,
	[issueId] [uniqueidentifier] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SubCategory]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SubCategory](
	[id] [uniqueidentifier] NOT NULL,
	[subCategoryName] [nvarchar](255) NOT NULL,
	[categoryId] [uniqueidentifier] NOT NULL,
	[description] [nvarchar](max) NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[id] [uniqueidentifier] NOT NULL,
	[userName] [varchar](255) NOT NULL,
	[email] [varchar](255) NOT NULL,
	[password] [varchar](255) NOT NULL,
	[address] [nvarchar](255) NULL,
	[phoneNumber] [varchar](20) NULL,
	[role] [varchar](50) NULL,
	[status] [varchar](50) NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Voucher]    Script Date: 12/22/2024 1:58:12 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Voucher](
	[id] [uniqueidentifier] NOT NULL,
	[voucherCode] [nvarchar](50) NOT NULL,
	[price] [decimal](10, 2) NULL,
	[createDate] [datetime] NULL,
	[modifyDate] [datetime] NULL,
	[isDelete] [bit] NULL,
	[status] [varchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Cart] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Cart] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[CartItem] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[CartItem] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[Category] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Category] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[Image] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Image] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[Issue] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Issue] ADD  DEFAULT (getdate()) FOR [createDate]
GO
ALTER TABLE [dbo].[Issue] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[IssueCategory] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[IssueCategory] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[IssueProduct] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[MaintenanceSchedule] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[MaintenanceTask] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[MaintenanceTask] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[Model3D] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Model3D] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[Order] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Order] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[OrderDetail] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Payment] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Product] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Product] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[SetupPackage] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[SetupPackage] ADD  DEFAULT (getdate()) FOR [createDate]
GO
ALTER TABLE [dbo].[SetupPackage] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[SetupPackageDetail] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Shipment] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Solution] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Solution] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[SubCategory] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[SubCategory] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT (getdate()) FOR [createDate]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[Voucher] ADD  DEFAULT (newid()) FOR [id]
GO
ALTER TABLE [dbo].[Voucher] ADD  DEFAULT ((0)) FOR [isDelete]
GO
ALTER TABLE [dbo].[Cart]  WITH CHECK ADD FOREIGN KEY([userId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[CartItem]  WITH CHECK ADD FOREIGN KEY([cartId])
REFERENCES [dbo].[Cart] ([id])
GO
ALTER TABLE [dbo].[CartItem]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[Image]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[Issue]  WITH CHECK ADD  CONSTRAINT [FK_Issue_IssueCategory] FOREIGN KEY([issueCategoryId])
REFERENCES [dbo].[IssueCategory] ([id])
GO
ALTER TABLE [dbo].[Issue] CHECK CONSTRAINT [FK_Issue_IssueCategory]
GO
ALTER TABLE [dbo].[IssueProduct]  WITH CHECK ADD FOREIGN KEY([issueId])
REFERENCES [dbo].[Issue] ([id])
GO
ALTER TABLE [dbo].[IssueProduct]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[MaintenanceSchedule]  WITH CHECK ADD FOREIGN KEY([userId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[MaintenanceTask]  WITH CHECK ADD FOREIGN KEY([maintenanceScheduleId])
REFERENCES [dbo].[MaintenanceSchedule] ([id])
GO
ALTER TABLE [dbo].[Order]  WITH CHECK ADD FOREIGN KEY([userId])
REFERENCES [dbo].[User] ([id])
GO
ALTER TABLE [dbo].[Order]  WITH CHECK ADD  CONSTRAINT [FK_Order_Voucher] FOREIGN KEY([voucherId])
REFERENCES [dbo].[Voucher] ([id])
GO
ALTER TABLE [dbo].[Order] CHECK CONSTRAINT [FK_Order_Voucher]
GO
ALTER TABLE [dbo].[OrderDetail]  WITH CHECK ADD FOREIGN KEY([orderId])
REFERENCES [dbo].[Order] ([id])
GO
ALTER TABLE [dbo].[OrderDetail]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[Payment]  WITH CHECK ADD FOREIGN KEY([orderId])
REFERENCES [dbo].[Order] ([id])
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD FOREIGN KEY([categoryId])
REFERENCES [dbo].[Category] ([id])
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_Model3D] FOREIGN KEY([model3DId])
REFERENCES [dbo].[Model3D] ([id])
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [FK_Product_Model3D]
GO
ALTER TABLE [dbo].[SetupPackageDetail]  WITH CHECK ADD FOREIGN KEY([productId])
REFERENCES [dbo].[Product] ([id])
GO
ALTER TABLE [dbo].[SetupPackageDetail]  WITH CHECK ADD FOREIGN KEY([setupPackageId])
REFERENCES [dbo].[SetupPackage] ([id])
GO
ALTER TABLE [dbo].[Shipment]  WITH CHECK ADD FOREIGN KEY([orderId])
REFERENCES [dbo].[Order] ([id])
GO
ALTER TABLE [dbo].[Solution]  WITH CHECK ADD FOREIGN KEY([issueId])
REFERENCES [dbo].[Issue] ([id])
GO
ALTER TABLE [dbo].[SubCategory]  WITH CHECK ADD FOREIGN KEY([categoryId])
REFERENCES [dbo].[Category] ([id])
GO
USE [master]
GO
ALTER DATABASE [FTSS] SET  READ_WRITE 
GO
