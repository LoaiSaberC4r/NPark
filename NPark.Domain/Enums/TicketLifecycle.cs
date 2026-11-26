namespace NPark.Domain.Enums;

public enum EntryConfirmationStatus { Legacy, Pending, Confirmed }
public enum BasePaymentStatus { Legacy, Unpaid, Paid, NoCharge }
public enum SanctionPaymentStatus { Legacy, Assessed, Confirmed }
public enum SanctionClassification { Ordinary, Overnight, Mixed }
public enum GateOperationType { Entry = 1, Exit = 2, Manual = 3, LockCardButton = 4 }
public enum GateOperationCompletionMode { Hardware = 0, Software = 1 }
public enum GateOperationStatus { Pending = 1, Published, Acknowledged, Completed, Failed, TimedOut, Unknown }
