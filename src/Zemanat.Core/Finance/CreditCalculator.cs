namespace Zemanat.Core.Finance;

/// <summary>Whether a request fits inside the employee's credit ceiling.</summary>
public enum CreditClass
{
    /// <summary>The request fits inside the remaining ceiling.</summary>
    Normal = 1,

    /// <summary>The request goes above the remaining ceiling; the applicant must accept a commitment and an approver must approve it.</summary>
    Surplus = 2
}

/// <summary>The result of checking a new request against the employee's ceiling. All amounts are in Rials.</summary>
/// <param name="Cap">The employee's total ceiling (net pay × allowed percent × months).</param>
/// <param name="ActiveTotal">The sum of the employee's active documents (pending approval and issued).</param>
/// <param name="Remaining">What is left of the ceiling, never negative.</param>
/// <param name="Requested">The amount of the new request.</param>
/// <param name="Class">Normal or surplus.</param>
/// <param name="ExcessAmount">The part of the request that is above the remaining ceiling (zero for a normal request).</param>
public sealed record CreditAssessment(
    long Cap,
    long ActiveTotal,
    long Remaining,
    long Requested,
    CreditClass Class,
    long ExcessAmount)
{
    /// <summary>True when the request is above the ceiling.</summary>
    public bool IsSurplus => Class == CreditClass.Surplus;
}

/// <summary>
/// The credit rule: ceiling = net pay × allowed percent (× optional months multiplier, default 1);
/// remaining = ceiling − active documents; a request above the remaining amount is a surplus request.
/// </summary>
public static class CreditCalculator
{
    /// <summary>The employee's total ceiling in Rials, rounded down.</summary>
    public static long CalculateCap(long netPayRial, decimal percent, int months = 1)
    {
        if (netPayRial <= 0 || percent <= 0m || months <= 0)
        {
            return 0;
        }

        var cap = decimal.Floor(netPayRial * (percent / 100m) * months);
        return decimal.ToInt64(cap);
    }

    /// <summary>Checks a new request against the ceiling.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The requested amount is not positive.</exception>
    public static CreditAssessment Assess(
        long netPayRial,
        decimal percent,
        int months,
        long activeTotalRial,
        long requestedRial)
    {
        if (requestedRial <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedRial), "The requested amount must be positive.");
        }

        var cap = CalculateCap(netPayRial, percent, months);
        var active = Math.Max(0L, activeTotalRial);
        var remaining = Math.Max(0L, cap - active);
        var isNormal = requestedRial <= remaining;
        var excess = isNormal ? 0L : requestedRial - remaining;

        return new CreditAssessment(
            cap,
            active,
            remaining,
            requestedRial,
            isNormal ? CreditClass.Normal : CreditClass.Surplus,
            excess);
    }
}
