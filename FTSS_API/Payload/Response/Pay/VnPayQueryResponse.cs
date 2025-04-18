namespace FTSS_API.Payload.Response.Pay;

public class VnPayQueryResponse
{
    public string vnp_TxnRef { get; set; }
    public string vnp_TransactionStatus { get; set; }
    public string vnp_ResponseCode { get; set; }
    public string vnp_Message { get; set; }
}