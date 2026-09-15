using System;
using Modules.AssetDeliveryModule.Data.ValueObjects;

namespace Modules.AssetDeliveryModule.Services
{
    /// <summary>"20 / 40 MB" - whole megabytes, the way a store shows a download.</summary>
    public static class AssetPackProgressText
    {
        private const double MEGABYTE = 1024d * 1024d;

        public static string Of(AssetPackProgressRVO progress) =>
            $"{Math.Round(progress.DownloadedBytes / MEGABYTE)} / {Math.Round(progress.TotalBytes / MEGABYTE)} MB";
    }
}
