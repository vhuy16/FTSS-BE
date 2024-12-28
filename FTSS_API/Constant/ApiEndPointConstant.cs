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
        public const string Register = UserEndPoint + "/registers";
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
}