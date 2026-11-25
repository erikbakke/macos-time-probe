// MacTimeProbe: does a platform get the CLOCK TIME right (zone data) and the TEXT right (locale data) for the rows of OceTimeTests' parity table?
//
// Run it with only the .NET 10 SDK, from any folder:   dotnet run MacTimeProbe.cs
// Nothing from the repository is needed. It mirrors what OceTime.FormatDate and FormatTime do (the same pattern building, and the same spacing normalisation as PR #873), and
// prints, for each of the 21 rows, three separate verdicts:
//   zone  - the clock time after converting the UTC instant to the zone, in 24-hour form with no culture involved (a fault here is zone data);
//   date  - the date text, as the platform's culture data formats it;
//   time  - the time text, the same.
// A row can fail on one and pass the others, which is the point: the test suite asserts the date first, so a row whose date fails never shows its time.
// Characters a person cannot tell from a space (U+202F, U+00A0, U+2009, direction marks and so on) are written as \uXXXX in the output.
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

// (locale, zone, UTC instant, expected 24-hour clock time in the zone, expected date text, expected time text): the OceTimeTests parity table, with the clock time added.
(string Locale, string Zone, string Utc, string Clock, string Date, string Time)[] rows =
[
    ("en-AU", "Australia/Sydney", "2026-09-17T05:03:00Z", "15:03", "17 Sept 2026", "3:03 pm"),
    ("en-AU", "Australia/Sydney", "2026-10-08T00:23:00Z", "11:23", "8 Oct 2026", "11:23 am"),
    ("en-AU", "Australia/Sydney", "2026-01-05T15:45:00Z", "02:45", "6 Jan 2026", "2:45 am"),
    ("en-US", "America/Los_Angeles", "2026-09-17T05:03:00Z", "22:03", "Sep 16, 2026", "10:03 PM"),
    ("en-US", "America/Los_Angeles", "2026-10-08T00:23:00Z", "17:23", "Oct 7, 2026", "5:23 PM"),
    ("en-US", "America/Los_Angeles", "2026-01-05T15:45:00Z", "07:45", "Jan 5, 2026", "7:45 AM"),
    ("en-GB", "Europe/London", "2026-09-17T05:03:00Z", "06:03", "17 Sept 2026", "6:03"),
    ("en-GB", "Europe/London", "2026-10-08T00:23:00Z", "01:23", "8 Oct 2026", "1:23"),
    ("en-GB", "Europe/London", "2026-01-05T15:45:00Z", "15:45", "5 Jan 2026", "15:45"),
    ("en-IN", "Asia/Kolkata", "2026-09-17T05:03:00Z", "10:33", "17 Sept 2026", "10:33 am"),
    ("en-IN", "Asia/Kolkata", "2026-10-08T00:23:00Z", "05:53", "8 Oct 2026", "5:53 am"),
    ("en-IN", "Asia/Kolkata", "2026-01-05T15:45:00Z", "21:15", "5 Jan 2026", "9:15 pm"),
    ("de-DE", "Europe/Berlin", "2026-09-17T05:03:00Z", "07:03", "17. Sept. 2026", "7:03"),
    ("de-DE", "Europe/Berlin", "2026-10-08T00:23:00Z", "02:23", "8. Okt. 2026", "2:23"),
    ("de-DE", "Europe/Berlin", "2026-01-05T15:45:00Z", "16:45", "5. Jan. 2026", "16:45"),
    ("fr-FR", "Europe/Paris", "2026-09-17T05:03:00Z", "07:03", "17 sept. 2026", "7:03"),
    ("fr-FR", "Europe/Paris", "2026-10-08T00:23:00Z", "02:23", "8 oct. 2026", "2:23"),
    ("fr-FR", "Europe/Paris", "2026-01-05T15:45:00Z", "16:45", "5 janv. 2026", "16:45"),
    ("ja-JP", "Asia/Tokyo", "2026-09-17T05:03:00Z", "14:03", "2026年9月17日", "14:03"),
    ("ja-JP", "Asia/Tokyo", "2026-10-08T00:23:00Z", "09:23", "2026年10月8日", "9:23"),
    ("ja-JP", "Asia/Tokyo", "2026-01-05T15:45:00Z", "00:45", "2026年1月6日", "0:45"),
];

string Backslash = "\\";

// What a person cannot see is written out: every space that is not a plain one, control, format and unassigned characters.
string Esc(string text)
{
    StringBuilder result = new();
    foreach (char c in text)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
        bool hidden = c != ' ' && category is UnicodeCategory.SpaceSeparator or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator
            or UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned;
        if (hidden)
        {
            result.Append(Backslash).Append('u').Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        }
        else
        {
            result.Append(c);
        }
    }

    return result.ToString();
}

// The same reduction OceTime.NormalizeSpacing does (PR #873): direction marks dropped, every kind of space a single plain space, ends trimmed. Built from code points so the
// source stays readable.
Regex directionMarks = new("[" + (char)0x200E + (char)0x200F + (char)0x061C + (char)0x202A + "-" + (char)0x202E + (char)0x2066 + "-" + (char)0x2069 + "]", RegexOptions.CultureInvariant);
Regex spaceRuns = new("[\\p{Zs}\\t\\r\\n\\f\\v" + (char)0x0085 + (char)0x2028 + (char)0x2029 + "]+", RegexOptions.CultureInvariant);
string Normalise(string text) => spaceRuns.Replace(directionMarks.Replace(text, string.Empty), " ").Trim(' ');

// The same pattern building as OceTime.DatePattern and TimePattern.
string DatePattern(CultureInfo culture)
{
    string pattern = culture.DateTimeFormat.LongDatePattern;
    pattern = Regex.Replace(pattern, @"(?<![d'])d{3,4}(?![d'])[\s,.]*", string.Empty);
    pattern = Regex.Replace(pattern, @"[\s,.]+$", string.Empty);
    pattern = pattern.Replace("MMMM", "MMM", StringComparison.Ordinal);
    pattern = Regex.Replace(pattern, @"(?<=MMM),\s*(?=y)", " ");
    return string.IsNullOrWhiteSpace(pattern) ? "d MMM yyyy" : pattern;
}

string TimePattern(CultureInfo culture) => culture.DateTimeFormat.ShortTimePattern.Replace("HH", "H", StringComparison.Ordinal).Replace("hh", "h", StringComparison.Ordinal);

bool invariant = AppContext.TryGetSwitch("System.Globalization.Invariant", out bool invariantSwitch) && invariantSwitch;
Console.WriteLine("== Platform");
Console.WriteLine($"  {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture}), {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"  invariant globalization: {invariant}; collation version {CultureInfo.InvariantCulture.CompareInfo.Version.FullVersion}");
Console.WriteLine();
Console.WriteLine("== Culture data (September abbreviation, am/pm, patterns), the platform's own");
foreach (string locale in rows.Select(r => r.Locale).Distinct())
{
    CultureInfo culture = CultureInfo.GetCultureInfo(locale);
    DateTimeFormatInfo f = culture.DateTimeFormat;
    Console.WriteLine($"  {locale}: September abbreviated \"{Esc(f.AbbreviatedMonthNames[8])}\", genitive \"{Esc(f.AbbreviatedMonthGenitiveNames[8])}\"; "
        + $"AM \"{Esc(f.AMDesignator)}\" PM \"{Esc(f.PMDesignator)}\"; long date \"{Esc(f.LongDatePattern)}\", short time \"{Esc(f.ShortTimePattern)}\"");
}

Console.WriteLine();
Console.WriteLine("== Rows (zone = clock time without any culture; date and time = the text)");
int zoneFaults = 0, dateFaults = 0, timeFaults = 0;
foreach ((string locale, string zoneId, string utc, string clock, string date, string time) in rows)
{
    DateTimeOffset instant = DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
    TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
    CultureInfo culture = CultureInfo.GetCultureInfo(locale);
    DateTimeOffset local = TimeZoneInfo.ConvertTime(instant, zone);

    string actualClock = local.ToString("HH:mm", CultureInfo.InvariantCulture);
    string rawDate = local.ToString(DatePattern(culture), culture);
    string rawTime = local.ToString(TimePattern(culture), culture);
    string actualDate = Normalise(rawDate);
    string actualTime = Normalise(rawTime);

    bool zoneOk = actualClock == clock;
    bool dateOk = actualDate == date;
    bool timeOk = actualTime == time;
    zoneFaults += zoneOk ? 0 : 1;
    dateFaults += dateOk ? 0 : 1;
    timeFaults += timeOk ? 0 : 1;

    Console.WriteLine($"  {locale} {zoneId} {utc}   zone {(zoneOk ? "ok  " : "DIFF")}  date {(dateOk ? "ok  " : "DIFF")}  time {(timeOk ? "ok  " : "DIFF")}");
    if (!zoneOk)
    {
        Console.WriteLine($"      clock:  expected {clock}, got {actualClock} (offset {zone.GetUtcOffset(instant)})");
    }

    if (!dateOk)
    {
        Console.WriteLine($"      date:   expected \"{Esc(date)}\", got \"{Esc(actualDate)}\"   [raw from the platform: \"{Esc(rawDate)}\", pattern \"{Esc(DatePattern(culture))}\"]");
    }

    if (!timeOk)
    {
        Console.WriteLine($"      time:   expected \"{Esc(time)}\", got \"{Esc(actualTime)}\"   [raw from the platform: \"{Esc(rawTime)}\", pattern \"{Esc(TimePattern(culture))}\"]");
    }
}

Console.WriteLine();
Console.WriteLine($"== Summary: {rows.Length} rows; zone faults {zoneFaults}, date text faults {dateFaults}, time text faults {timeFaults}");
Console.WriteLine(zoneFaults > 0
    ? "   The clock time itself is wrong for at least one row: that is the platform's time zone data or the conversion, not the locale text."
    : "   Every clock time is right: any difference above is in the locale's TEXT (month names, am/pm, spacing), not in the conversion.");
