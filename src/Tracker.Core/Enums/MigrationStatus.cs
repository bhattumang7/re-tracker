namespace Tracker.Core.Enums;

public enum MigrationStatus
{
    Pending     = 0,
    InProgress  = 1,
    NeedsReview = 2,
    Done        = 3,
    Skipped     = 4,
    Deferred    = 5
}
