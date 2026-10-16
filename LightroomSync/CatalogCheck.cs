using Microsoft.Data.Sqlite;

namespace LightroomSync
{
    // A Lightroom catalog is a SQLite database. Checking it before it is uploaded or installed means a damaged
    // catalog can never silently replace a good copy on the share or on another computer.
    internal record CatalogHealth(bool Ok, string Detail, long Images, long MissingDevelop)
    {
        public string Summary() => Images < 0
            ? "photo counts unavailable"
            : Images.ToString("N0") + " photos, " + MissingDevelop.ToString("N0") + " without develop settings";
    }

    internal static class CatalogCheck
    {
        // Opens the file read-only and immutable, so it never creates journal files or waits on a lock.
        public static CatalogHealth Inspect(string catalogPath)
        {
            try
            {
                string uri = new Uri(catalogPath).AbsoluteUri + "?mode=ro&immutable=1";
                using var connection = new SqliteConnection("Data Source=" + uri + ";Pooling=False");
                connection.Open();

                string? integrity;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA quick_check(1)";
                    integrity = command.ExecuteScalar()?.ToString();
                }
                if (integrity != "ok")
                {
                    return new CatalogHealth(false, integrity ?? "no result", -1, 0);
                }

                // Photo counts are a bonus: a file that is not a Lightroom catalog just reports "unavailable".
                try
                {
                    long images = Count(connection, "SELECT COUNT(*) FROM Adobe_images");
                    long missing = Count(connection,
                        "SELECT COUNT(*) FROM Adobe_images i LEFT JOIN Adobe_imageDevelopSettings ds ON ds.image = i.id_local WHERE ds.image IS NULL");
                    return new CatalogHealth(true, "", images, missing);
                }
                catch (SqliteException)
                {
                    return new CatalogHealth(true, "", -1, 0);
                }
            }
            catch (Exception ex)
            {
                return new CatalogHealth(false, ex.Message, -1, 0);
            }
        }

        private static long Count(SqliteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt64(command.ExecuteScalar());
        }

        // Compares with what was last uploaded. Returns a reason to be careful, or null when nothing looks wrong.
        public static string? Concern(CatalogHealth now, CatalogInfo? before)
        {
            if (before == null || before.Images <= 0 || now.Images < 0)
            {
                return null;
            }

            long tolerance = Math.Max(50, now.Images / 100);
            if (now.MissingDevelop > before.MissingDevelop + tolerance)
            {
                return now.MissingDevelop.ToString("N0") + " photos have no develop settings (it was " + before.MissingDevelop.ToString("N0") + "). Their edits may have been lost.";
            }
            if (before.Images > 100 && now.Images < before.Images * 0.8)
            {
                return "the photo count dropped from " + before.Images.ToString("N0") + " to " + now.Images.ToString("N0") + ".";
            }
            return null;
        }
    }
}
