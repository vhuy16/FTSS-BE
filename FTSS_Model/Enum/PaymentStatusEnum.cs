namespace FTSS_Model.Enum;

public enum PaymentStatusEnum
{
    Pending,         // Payment has been initiated but not completed.
    Completed,       // Payment has been successfully completed.
    Failed,         // Payment attempt failed.
    Canceled,        // Payment was canceled by the user or system.
    Refunded,   
    Refunding,// Payment was refunded to the customer.
    Processing,      // Payment is currently being processed.
    Declined     // Payment was declined by the payment provider.
}