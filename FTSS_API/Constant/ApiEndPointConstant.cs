namespace FTSS_API.Constant;

public static class ApiEndPointConstant
{
    static ApiEndPointConstant()
    {
    }

    public const string RootEndPoint = "/api";
    public const string ApiVersion = "/v1";
    public const string ApiEndpoint = RootEndPoint + ApiVersion;
    public static class User
    {
        public const string UserEndPoint = ApiEndpoint + "/user";
        public const string Register = UserEndPoint + "/register";
        public const string Login = UserEndPoint + "/login";
        public const string LoginCustomer = UserEndPoint + "/login-customer";
        public const string DeleteUser = UserEndPoint + "/{id}";
        public const string GetAllUser = UserEndPoint;
        public const string GetUserById = UserEndPoint + "/{id}";
        public const string GetUser = UserEndPoint + "token";
        public const string UpdateUser = UserEndPoint + "/{id}";
        public const string VerifyOtp = UserEndPoint + "/verify-otp";
        public const string ForgotPassword = UserEndPoint + "/forgot-password";
        public const string UpdateBankInfor = UserEndPoint + "/update-bank-infor";
        public const string ResetPassword = UserEndPoint + "/reset-password";
        public const string VerifyForgotPassword = UserEndPoint + "/verify-forgot-password";
        public const string ResendOtp = UserEndPoint + "/resend-otp";
        public const string ChangePassword = UserEndPoint + "/change-password";
        
    }
    public static class Issue
    {
        public const string IssueEndPoint = ApiEndpoint + "/issue";
        public const string CreateIssue = IssueEndPoint;
        public const string GetAllIssues = IssueEndPoint;
        public const string GetIssueById = IssueEndPoint + "/{id}";
        public const string UpdateIssue = IssueEndPoint + "/{id}";
        public const string DeleteIssue = IssueEndPoint + "/{id}";
        public const string EnableIssue = IssueEndPoint + "/{id}/enable";
    }

    public static class PaymentOS
    {
        public const string CreatePaymentUrl = "api/payment/create-url";
        public const string GetPaymentInfo = "api/payment/{paymentLinkId}";
    }
    public static class VNPay
    {
        public const string CreatePaymentUrl = "api/vnpay/create-payment-url";
        public const string ValidatePaymentResponse = "api/vnpay/validate-payment-response";
    }
    public static class IssueCategory
    {
        public const string IssueCategoryEndPoint = ApiEndpoint + "/issue-category";
        public const string CreateIssueCategory = IssueCategoryEndPoint;
        public const string GetAllIssueCategories = IssueCategoryEndPoint;
        public const string GetIssueCategoryById = IssueCategoryEndPoint + "/{id}";
        public const string UpdateIssueCategory = IssueCategoryEndPoint + "/{id}";
        public const string DeleteIssueCategory = IssueCategoryEndPoint + "/{id}";
        public const string EnableIssueCategory = IssueCategoryEndPoint + "/{id}/enable"; // Thêm endpoint mới
    }

    public static class Shipment
    {
        public const string ShipmentEndPoint = ApiEndpoint + "/shipment";
        public const string CreateShipment = ShipmentEndPoint;
        public const string GetAllShipments = ShipmentEndPoint;
        public const string GetShipmentById = ShipmentEndPoint + "/{id}";
        public const string UpdateShipment = ShipmentEndPoint + "/{id}";
        public const string DeleteShipment = ShipmentEndPoint + "/{id}";
    }
    public static class Payment
    {
        public const string PaymentEndPoint = ApiEndpoint + "/payment";
        public const string CreatePaymentUrl = PaymentEndPoint + "/create-url";
        public const string GetPaymentById = PaymentEndPoint + "/{paymentId}";
        public const string GetPaymentByOrderId = PaymentEndPoint + "/order/{orderId}";
        public const string GetPayments = PaymentEndPoint;
        public const string UpdatePaymentStatus =ApiEndpoint + "/update-status" + "/{id}";
        public const string UpdateBankInfor = PaymentEndPoint + "/update-bank-infor";
        public const string GetPaymentByStatus = PaymentEndPoint + "/payment-by-status";
    }
    public static class Category
    {
        public const string CategoryEndPoint = ApiEndpoint + "/category";
        public const string CreateCategory = CategoryEndPoint;
        public const string GetAllCategory = CategoryEndPoint;
        public const string GetListCategory = CategoryEndPoint + "/get-list-category";
        public const string GetCategory = CategoryEndPoint + "/{id}";
        public const string UpdateCategory = CategoryEndPoint + "/{id}";
        public const string DeleteCategory = CategoryEndPoint + "/{id}";
    }
    public static class SubCategory
    {
        public const string SubCategoryEndPoint = ApiEndpoint + "/subcategory";
        public const string CreateSubCategory = SubCategoryEndPoint;
        public const string GetAllSubCategories = SubCategoryEndPoint;
        public const string GetSubCategory = SubCategoryEndPoint + "/{id}";
        public const string UpdateSubCategory = SubCategoryEndPoint + "/{id}";
        public const string DeleteSubCategory = SubCategoryEndPoint + "/{id}";
    }
    public static class Product
    {
        public const string ProductEndpoint = ApiEndpoint + "/product";
        public const string CreateNewProduct = ProductEndpoint;
        public const string GetListProducts = ProductEndpoint;
        public const string GetAllProducts = ProductEndpoint + "/get-all-product";
        public const string GetProductById = ProductEndpoint + "/{id}";
        public const string GetListProductsBySubCategory = ProductEndpoint + "/subcategory";
        public const string UpdateProduct = ProductEndpoint + "/{id}";
        public const string EnableProduct = ProductEndpoint + "/enable-product" + "/{id}"  ;
        public const string DeleteProduct = ProductEndpoint + "/{id}";
        public const string UploadImg = "upload-img";
    }
    public static class GoogleAuthentication
    {
        public const string GoogleAuthenticationEndpoint = ApiEndpoint + "/google-auth";
        public const string GoogleLogin = GoogleAuthenticationEndpoint + "/login";
        public const string GoogleSignIn = GoogleAuthenticationEndpoint + "/signin-google/";

    }
    public static class Order
    {
        public const string OrderEndpoint = ApiEndpoint + "/order";
        public const string CreateNewOrder = OrderEndpoint;
        public const string GetListOrder = OrderEndpoint;
        public const string GetALLOrder = OrderEndpoint + "/get-all-order";
        public const string GetOrderById = OrderEndpoint + "/{id}";
        public const string UpdateOrder = OrderEndpoint + "/{id}";
        public const string CancelOrder = OrderEndpoint + "/cancel-order" + "/{id}";
        public const string CreateReturnRequest = OrderEndpoint + "/create-return-request";
        public const string GetReturnRequest = OrderEndpoint + "/get-return-request";
        public const string UpdateTime = OrderEndpoint+ "/update-time" + "/{id}";
    }

    public static class Cart
    {
        public const string CartEndPoint = ApiEndpoint + "/cart";
        public const string AddCartItem = CartEndPoint + "/item";
        public const string AddSetupPackageToCart = CartEndPoint + "/setup-package";
        public const string DeleteCartItem = CartEndPoint + "item" + "/{itemId}";
        public const string GetAllCart = CartEndPoint;
        public const string ClearCart = CartEndPoint + "/clear-all";
        public const string GetCartSummary = CartEndPoint + "/get-summary";
        public const string UpdateCartItem = CartEndPoint + "item" + "/{itemId}";
    }
    public static class Voucher
    {
        public const string VoucherEndPoint = ApiEndpoint + "/voucher";
        public const string AddVoucher = VoucherEndPoint;
        public const string GetListVoucher = VoucherEndPoint;
        public const string GetAllVoucher = VoucherEndPoint + "/get-all-voucher";
        public const string UpdateVoucher = VoucherEndPoint + "{id}";
        public const string UpdateStatusVoucher = VoucherEndPoint + "/update-status-voucher/{id}";
    }
    public static class SetupPackage
    {
        public const string SetupPackageEndPoint = ApiEndpoint + "/setuppackage";
        public const string AddSetupPackage = SetupPackageEndPoint;
        public const string CopySetupPackage = SetupPackageEndPoint + "copySetupPackage" + "/{setupPackageId}";
        public const string RemoveSetupPackage = SetupPackageEndPoint + "/{id}";
        public const string GetListSetupPackage = SetupPackageEndPoint;
        public const string GetListSetupPackageAllUser = SetupPackageEndPoint + "/all-users";
        public const string GetListSetupPackageAllShop = SetupPackageEndPoint + "/all-shop";
        public const string UpdateSetupPackage  = SetupPackageEndPoint + "/{setupPackageId}";
        public const string EnableSetupPackage  = SetupPackageEndPoint +"/enableSetupPackage"+ "/{setupPackageId}";
        public const string GetSetUpById = SetupPackageEndPoint + "/{id}";
    }
    public static class Booking
    {
        public const string BookingEndPoint = ApiEndpoint + "/booking";
        public const string AssigningTechnician = BookingEndPoint;
        public const string UpdateStatusMission = BookingEndPoint + "{id}";
        public const string GetListBookingForManager = BookingEndPoint;
        public const string GetListMissionTech = BookingEndPoint + "/list-mission-tech";
        public const string BookingSchedule = BookingEndPoint + "/booking-schedule";
        public const string AssigningTechnicianBooking = BookingEndPoint + "/assign-booking";
        public const string GetServicePackage = BookingEndPoint + "/servicepackage";
        public const string GetListTech = BookingEndPoint + "/list-tech";
        public const string GetListMissionForManager = BookingEndPoint + "/list-mission-manager";
        public const string GetDateUnavailable = BookingEndPoint + "/date-unavailable";
        public const string GetListBookingForUser = BookingEndPoint + "/list-booking-user";
        public const string GetBookingById = BookingEndPoint + "/{bookingid}";
        public const string UpdateMission = BookingEndPoint + "/update-mission/{missionid}";
        public const string GetMissionById = BookingEndPoint + "/get-mission-by-id/{missionid}";
        public const string UpdateBooking = BookingEndPoint + "/update-booking/{bookingid}";
        public const string CancelBooking = BookingEndPoint + "/cancel-booking/{bookingid}";
        public const string UpdateBookingStatus = BookingEndPoint + "/update-booking-status/{bookingid}";
        public const string GetHistoryOrder = BookingEndPoint + "/get-history-order/{orderid}";
        public const string Confirm = BookingEndPoint + "/user-confirm";
    }
}