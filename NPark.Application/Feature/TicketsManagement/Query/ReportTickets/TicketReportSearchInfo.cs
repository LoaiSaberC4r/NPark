using System.Globalization;

namespace NPark.Application.Specifications.TicketSpecification
{
    internal sealed class TicketReportSearchInfo
    {
        public string? SearchTerm { get; init; }

        public string? NormalizedSearchTerm { get; init; }

        public Guid? SearchGuid { get; init; }

        public int? SearchInt { get; init; }

        public DateTime? SearchDate { get; init; }

        public bool WantsInsideParking { get; init; }

        public bool WantsExited { get; init; }

        public bool WantsSubscriber { get; init; }

        public bool WantsWalkIn { get; init; }

        public bool WantsCollected { get; init; }

        public bool WantsPendingCollection { get; init; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(SearchTerm);
    }

    internal static class TicketReportSearchHelper
    {
        public static TicketReportSearchInfo Build(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new TicketReportSearchInfo();
            }

            var trimmed = searchTerm.Trim();
            var normalized = trimmed.ToLowerInvariant();

            Guid? searchGuid = null;
            if (Guid.TryParse(trimmed, out var parsedGuid))
            {
                searchGuid = parsedGuid;
            }

            int? searchInt = null;
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
            {
                searchInt = parsedInt;
            }

            DateTime? searchDate = null;
            if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedInvariantDate))
            {
                searchDate = parsedInvariantDate.Date;
            }
            else if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedCurrentDate))
            {
                searchDate = parsedCurrentDate.Date;
            }

            var wantsInside = ContainsAny(normalized,
                "inside",
                "insideparking",
                "in parking",
                "داخل",
                "جوه",
                "بالداخل",
                "في البارك",
                "فى البارك");

            var wantsExited = ContainsAny(normalized,
                "exited",
                "exit",
                "outside",
                "خرج",
                "خارجة",
                "خارج",
                "طلعت",
                "بره");

            var wantsWalkIn = ContainsAny(normalized,
                "walkin",
                "walk-in",
                "visitor",
                "guest",
                "non subscriber",
                "non-subscriber",
                "not subscriber",
                "زائر",
                "غير مشترك",
                "مش مشترك");

            var wantsSubscriber = !wantsWalkIn && ContainsAny(normalized,
                "subscriber",
                "subscription",
                "subscribed",
                "member",
                "membership",
                "مشترك",
                "اشتراك",
                "عضوية");

            var wantsPendingCollection = ContainsAny(normalized,
                "pending",
                "uncollected",
                "not collected",
                "unpaid",
                "not paid",
                "غير محصل",
                "غير مدفوع",
                "لسه متحصلش");

            var wantsCollected = !wantsPendingCollection && ContainsAny(normalized,
                "collected",
                "paid",
                "محصل",
                "مدفوع",
                "تم التحصيل",
                "تم الدفع");

            return new TicketReportSearchInfo
            {
                SearchTerm = trimmed,
                NormalizedSearchTerm = normalized,
                SearchGuid = searchGuid,
                SearchInt = searchInt,
                SearchDate = searchDate,
                WantsInsideParking = wantsInside,
                WantsExited = wantsExited,
                WantsSubscriber = wantsSubscriber,
                WantsWalkIn = wantsWalkIn,
                WantsCollected = wantsCollected,
                WantsPendingCollection = wantsPendingCollection
            };
        }

        private static bool ContainsAny(string source, params string[] values)
        {
            return values.Any(source.Contains);
        }
    }
}