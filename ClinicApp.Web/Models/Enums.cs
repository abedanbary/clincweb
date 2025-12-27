namespace ClinicApp.Web.Models
{
    public enum PaymentMethod
    {
        Cash = 1,
        CreditCard = 2,
        BankTransfer = 3,
        Insurance = 4
    }

    public enum PaymentStatus
    {
        Paid = 1,
        Pending = 2,
        Refunded = 3
    }
       public enum AppointmentStatus
    {
        Scheduled = 1,   
        Completed = 2,   
        Cancelled = 3,   
        NoShow = 4       
    }
}
