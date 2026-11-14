using Microsoft.EntityFrameworkCore;
using Zemanat.Core.Dates;
using Zemanat.Core.Documents;
using Zemanat.Core.Settings;

namespace Zemanat.Data.Documents;

/// <summary>Hands out secretariat numbers. Call it inside a transaction; the sequence restarts at 1 every Jalali year.</summary>
public static class DocumentNumbering
{
    public static async Task<DocumentNumber> NextAsync(AppDbContext db, SystemSettings settings, DateTime nowUtc, CancellationToken ct = default)
    {
        var year = JalaliDate.Year(IranClock.ToIran(nowUtc));
        var counter = await db.SecretariatCounters.FirstOrDefaultAsync(c => c.Year == year, ct);
        if (counter is null)
        {
            counter = new SecretariatCounter { Year = year };
            db.SecretariatCounters.Add(counter);
        }

        counter.LastSequence++;
        await db.SaveChangesAsync(ct);
        return new DocumentNumber(counter.LastSequence, year, settings.DocumentNumberCode);
    }
}
