namespace FTSS_Model.Enum;

public enum PaymentStatusEnum
{
   
    Completed,       // Payment has been successfully completed.
   
    Cancelled,        // Payment was canceled by the user or system.
  
    Refunding,// Payment was refunded to the customer.
    Processing,      // Payment is currently being processed.
}