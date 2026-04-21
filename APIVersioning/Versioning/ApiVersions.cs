namespace APIVersioning.Versioning
{
    /// <summary>
    /// Centralized version number constants.
    ///
    /// Rules:
    ///   1. Never use magic version strings anywhere except this class.
    ///   2. Add a Sunset date when deprecating a version.
    ///   3. Keep at minimum N-1 versions supported at all times.
    ///   4. Announce deprecation at least 6 months before removal.
    /// </summary>
    public static class ApiVersions
    {
        // ── Supported versions ────────────────────────────────────────────────────

        /// <summary>
        /// V1.0 — initial release.
        /// Status: DEPRECATED as of 2025-01-01.
        /// Sunset date: 2025-07-01 — clients must migrate to V2 before this date.
        /// </summary>
        public const string V1 = "1.0";

        /// <summary>
        /// V2.0 — redesigned order model, pagination overhaul.
        /// Status: STABLE — fully supported.
        /// </summary>
        public const string V2 = "2.0";

        /// <summary>
        /// V3.0 — cursor-based pagination, structured errors, async webhooks.
        /// Status: CURRENT — recommended for all new integrations.
        /// </summary>
        public const string V3 = "3.0";

        // ── Metadata ──────────────────────────────────────────────────────────────

        /// <summary>The latest stable version — used as the default.</summary>
        public const string Latest = V3;

        /// <summary>Versions that are deprecated but still served.</summary>
        public static readonly string[] Deprecated = [V1];

        /// <summary>Sunset dates per deprecated version (for Sunset HTTP header).</summary>
        public static readonly Dictionary<string, DateTimeOffset> SunsetDates = new()
        {
            [V1] = new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero)
        };
    }
}