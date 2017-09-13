namespace KtaneWeb
{
    public enum KtaneModuleType
    {
        [KtaneFilterOption("Regular module", 'R')]
        Regular,
        [KtaneFilterOption("Needy module", 'y')]
        Needy,
        [KtaneFilterOption("Widget", 'W')]
        Widget,
        [KtaneFilterOption("Bomb room", 'B')]
        Room,
        [KtaneFilterOption("Bomb casing", 'g')]
        Casing,
        [KtaneFilterOption("Mission pack", 'o')]
        Missions,
        [KtaneFilterOption("Sound pack", 'd')]
        Sounds,
        [KtaneFilterOption("Other", 'h')]
        Other
    }

    public enum KtaneModuleOrigin
    {
        [KtaneFilterOption("Vanilla", 'V')]
        Vanilla,
        [KtaneFilterOption("Mods", 'M')]
        Mods
    }

    public enum KtaneModuleDifficulty
    {
        VeryEasy,
        Easy,
        Medium,
        Hard,
        VeryHard
    }

    public enum KtaneTwitchPlays
    {
        [KtaneFilterOption("Not supported")]
        NotSupported,
        [KtaneFilterOption("Supported")]
        Supported
    }
}