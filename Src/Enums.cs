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

    public enum KtaneSupport
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

    public enum KtaneModuleSouvenir
    {
        [KtaneFilterOption("Unexamined"), KtaneSouvenirInfo('U', "We have not yet decided whether this module is a candidate for inclusion in Souvenir.")]
        Unexamined,
        [KtaneFilterOption("Not a candidate"), KtaneSouvenirInfo('N', "This module is not a candidate for inclusion in Souvenir.")]
        NotACandidate,
        [KtaneFilterOption("Considered"), KtaneSouvenirInfo('C', "This module may be a candidate for inclusion in Souvenir.")]
        Considered,
        [KtaneFilterOption("Planned"), KtaneSouvenirInfo('P', "Future inclusion in Souvenir is planned for this module.")]
        Planned,
        [KtaneFilterOption("Supported"), KtaneSouvenirInfo('S', "This module is included in Souvenir. Refer to the Souvenir manual for details.")]
        Supported
    }
}