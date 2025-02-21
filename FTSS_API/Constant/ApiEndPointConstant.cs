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
        public const string ResetPassword = UserEndPoint + "/reset-password";
        public const string VerifyForgotPassword = UserEndPoint + "/verify-forgot-password";
        public const string ChangePassword = UserEndPoint + "/change-password";
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
    public static class Payment
    {
        public const string PaymentEndPoint = ApiEndpoint + "/payment";
        public const string CreatePaymentUrl = PaymentEndPoint + "/create-url";
        public const string GetPaymentById = PaymentEndPoint + "/{paymentId}";
        public const string GetPaymentByOrderId = PaymentEndPoint + "/order/{orderId}";
        public const string GetPayments = PaymentEndPoint;
    }
    public static class Category
    {
        public const string CategoryEndPoint = ApiEndpoint + "/category";
        public const string CreateCategory = CategoryEndPoint;
        public const string GetAllCategory = CategoryEndPoint;
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
        public const string GetListProductsBySubCategoryId = ProductEndpoint +"/{id}" + "/subcategory";
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
    }

    public static class Cart
    {
        public const string CartEndPoint = ApiEndpoint + "/cart";
        public const string AddCartItem = CartEndPoint + "/item";
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
        public const string DeleteVoucher = VoucherEndPoint + "{id}";
    }
    public static class SetupPackage
    {
        public const string SetupPackageEndPoint = ApiEndpoint + "/setuppackage";
        public const string AddSetupPackage = SetupPackageEndPoint;
        public const string RemoveSetupPackage = SetupPackageEndPoint + "/{id}";
        public const string GetListSetupPackage = SetupPackageEndPoint;
        public const string GetListSetupPackageAllUser = SetupPackageEndPoint + "/all-users";
        public const string GetListSetupPackageShop = SetupPackageEndPoint + "/shop";
        public const string UpdateSetupPackage  = SetupPackageEndPoint + "/{id}";
        public const string GetSetUpById = SetupPackageEndPoint + "/{id}";
    }
    public static class MaintenanceSchedule
    {
        public const string MaintenanceScheduleEndPoint = ApiEndpoint + "/maintenanceschedule";
        public const string AssigningTechnician = MaintenanceScheduleEndPoint;
        public const string CancelTask = MaintenanceScheduleEndPoint + "{id}";
        public const string GetListTask = MaintenanceScheduleEndPoint;
        public const string GetListTaskTech = MaintenanceScheduleEndPoint + "/list-task-tech";
    }
}