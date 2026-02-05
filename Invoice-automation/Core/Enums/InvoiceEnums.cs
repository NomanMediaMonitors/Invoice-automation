namespace InvoiceAutomation.Web.Core.Enums;

/// <summary>
/// Invoice processing status
/// </summary>
public enum InvoiceStatus
{
    Draft = 0,
    PendingManagerReview = 1,
    RejectedByManager = 2,
    PendingAdminApproval = 3,
    RejectedByAdmin = 4,
    Approved = 5,
    PaymentPending = 6,
    PaymentProcessing = 7,
    Completed = 8
}

/// <summary>
/// How the expense account was matched
/// </summary>
public enum MatchType
{
    Manual = 0,
    VendorDefault = 1,
    AiMatch = 2
}

/// <summary>
/// Approval workflow level
/// </summary>
public enum ApprovalLevel
{
    Manager = 1,
    Admin = 2,
    CFO = 3
}

/// <summary>
/// Approval decision status
/// </summary>
public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Payment processing status
/// </summary>
public enum PaymentStatus
{
    Scheduled = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>
/// Supported accounting providers
/// </summary>
public enum AccountingProvider
{
    None = 0,
    Endraaj = 1,
    QuickBooks = 2
}

/// <summary>
/// User roles in the system
/// </summary>
public enum UserRole
{
    Viewer = 0,
    Accountant = 1,
    Approver = 2,
    Manager = 3,
    Admin = 4,
    SuperAdmin = 5
}

/// <summary>
/// Account types from external COA
/// </summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Revenue = 4,
    Expense = 5
}

/// <summary>
/// Account sub-types for filtering
/// </summary>
public enum AccountSubType
{
    Bank = 1,
    Cash = 2,
    AccountsReceivable = 3,
    AccountsPayable = 4,
    OtherCurrentAsset = 5,
    FixedAsset = 6,
    OtherAsset = 7,
    OtherCurrentLiability = 8,
    LongTermLiability = 9,
    OtherExpense = 10,
    CostOfGoodsSold = 11,
    Income = 12,
    OtherIncome = 13
}
