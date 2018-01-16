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
        //[KtaneFilterOption("Gameplay room", 'G')]
        Room,
        //[KtaneFilterOption("Bomb casing", 'B')]
        Casing,
        //[KtaneFilterOption("Mission pack", 'o')]
        Missions,
        //[KtaneFilterOption("Sound pack", 'd')]
        Sounds,
        //[KtaneFilterOption("Other", 'h')]
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

    public enum KtaneModuleCompatibility
    {
        [KtaneFilterOption("Untested")]
        Untested,
        [KtaneFilterOption("Unplayable")]
        Unplayable,
        [KtaneFilterOption("Problematic")]
        Problematic,
        [KtaneFilterOption("Compatible")]
        Compatible,
    }
}