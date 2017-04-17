namespace KtaneWeb
{
    /// <summary>
    ///     This class contains the configuration that goes inside the Propeller configuration file and is thus deliberately
    ///     kept minimal.</summary>
    public sealed class KtaneSettings
    {
        /// <summary>
        ///     Refers to the path and filename of the configuration file that contains JSON which deserializes to <see
        ///     cref="KtaneWebConfig"/>.</summary>
        public string ConfigFile;
    }
}
