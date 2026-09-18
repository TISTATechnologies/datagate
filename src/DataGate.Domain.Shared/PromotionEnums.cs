namespace DataGate.Promotions;

/// <summary>Who is authorised to sign off, per the Delegation of Authority matrix.</summary>
public enum ApproverTier
{
    None = 0,          // no human needed
    DataSteward = 1,
    DataOwner = 2,
    Director = 3,
    ChiefDataOfficer = 4
}

public enum PromotionStatus
{
    Evaluating = 0,
    AutoPromoted = 1,
    AwaitingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Quarantined = 5    // SLA expired with no decision
}

/// <summary>Machine-readable reasons, so the audit trail explains itself.</summary>
public enum GateReason
{
    Clean = 0,
    RowCountDrift = 1,
    NullRateSpike = 2,
    NewColumn = 3,
    SensitiveDataDetected = 4,
    HighFinancialExposure = 5,
    RegulatoryReportingTable = 6
}
