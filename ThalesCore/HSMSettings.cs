using System;

namespace ThalesCore
{
    /// <summary>
    /// Global runtime settings for the hosted HSM used by tests and services.
    /// </summary>
    public static class HSMSettings
    {
        // Delay in milliseconds applied to HSM responses. Default 0 (no artificial delay).
        public static int ResponseDelayMs { get; set; } = 0;
    }
}
