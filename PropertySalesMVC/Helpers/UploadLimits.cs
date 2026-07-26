namespace PropertySalesMVC.Helpers
{
    /// <summary>
    /// Videos aren't resized/re-encoded server-side like images are (that
    /// needs real transcoding infra — FFmpeg — which isn't safe to run on
    /// this host's 256MB RAM budget). Instead we enforce a size ceiling
    /// both client-side (instant feedback) and server-side (authoritative,
    /// since client-side checks are trivially bypassed via a direct POST).
    /// </summary>
    public static class UploadLimits
    {
        public const long MaxVideoSizeBytes = 20 * 1024 * 1024; // 20MB
        public const int MaxVideoDimension = 1920; // checked client-side only, see AddProperty.cshtml/EditProperty.cshtml
    }
}
