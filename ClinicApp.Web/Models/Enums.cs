namespace ClinicApp.Web.Models
{
    public enum PatientFileCategory
    {
        IntraoralPhoto = 0,
        PanoramicXray = 1,
        BitewingXray = 2,
        PeriapicalXray = 3,
        CbctScan = 4,
        IntraoralScan = 5,
        MedicalReport = 6,
        Prescription = 7,
        Invoice = 8,
        Other = 9
    }

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
