using System;
using System.Runtime.InteropServices;

namespace Modules.AssetDeliveryModule.Gateway
{
    /// <summary>
    /// The externs FlowAssetPacks.mm exports. Every callback carries the request id it answers,
    /// and the gateway keeps the map from id to the task waiting on it. Static because DllImport
    /// forces it; outside an iOS player each call answers "not iOS" so the type compiles everywhere.
    /// </summary>
    internal static class FlowAssetPacksBridge
    {
        public delegate void StatusCallback(int requestId, string pack, int status, long totalBytes, long downloadedBytes, string error);

        public delegate void ProgressCallback(int requestId, long totalBytes, long downloadedBytes);

        public delegate void DoneCallback(int requestId, bool ok, string error);

        public const int STATUS_ON_DEVICE = 1;
        public const int STATUS_MISSING = 0;
        public const int STATUS_ERROR = -1;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern bool FlowAssetPacks_IsSupported();
        [DllImport("__Internal")] private static extern void FlowAssetPacks_GetStatus(string pack, int requestId, StatusCallback callback);
        [DllImport("__Internal")] private static extern void FlowAssetPacks_Ensure(string packsSeparatedByComma, int requestId, ProgressCallback progress, DoneCallback done);
        [DllImport("__Internal")] private static extern void FlowAssetPacks_Remove(string pack, int requestId, DoneCallback done);
        [DllImport("__Internal")] private static extern IntPtr FlowAssetPacks_PathForFile(string relativePath);

        public static bool IsSupported() => FlowAssetPacks_IsSupported();

        public static void GetStatus(string pack, int requestId, StatusCallback callback) =>
            FlowAssetPacks_GetStatus(pack, requestId, callback);

        public static void Ensure(string packsSeparatedByComma, int requestId, ProgressCallback progress, DoneCallback done) =>
            FlowAssetPacks_Ensure(packsSeparatedByComma, requestId, progress, done);

        public static void Remove(string pack, int requestId, DoneCallback done) =>
            FlowAssetPacks_Remove(pack, requestId, done);

        /// <summary>The native side strdup's the path; it is freed here once copied into a managed string.</summary>
        public static string PathForFile(string relativePath)
        {
            IntPtr pointer = FlowAssetPacks_PathForFile(relativePath);
            if (pointer == IntPtr.Zero) return null;

            string path = Marshal.PtrToStringAnsi(pointer);
            Marshal.FreeHGlobal(pointer);
            return path;
        }
#else
        public static bool IsSupported() => false;

        public static void GetStatus(string pack, int requestId, StatusCallback callback) =>
            callback(requestId, pack, STATUS_ERROR, 0, 0, "not an iOS player");

        public static void Ensure(string packsSeparatedByComma, int requestId, ProgressCallback progress, DoneCallback done) =>
            done(requestId, false, "not an iOS player");

        public static void Remove(string pack, int requestId, DoneCallback done) =>
            done(requestId, false, "not an iOS player");

        public static string PathForFile(string relativePath) => null;
#endif
    }
}
