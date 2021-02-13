namespace KtaneWeb
{
    public enum KtaneModuleType
    {
        [KtaneFilterOption("Regular module", 'R')]
        Regular,
        [KtaneFilterOption("Needy module", 'y')]
        Needy,
        [KtaneFilterOption("Hodable", 'H')]
        Holdable,
        [KtaneFilterOption("Widget", 'W')]
        Widget
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
        [KtaneFilterOption("Very easy")]
        VeryEasy,
        [KtaneFilterOption("Easy")]
        Easy,
        [KtaneFilterOption("Medium")]
        Medium,
        [KtaneFilterOption("Hard")]
        Hard,
        [KtaneFilterOption("Very hard")]
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
        [KtaneFilterOption("Supported"), KtaneSouvenirInfo('S', "This module is included in Souvenir. Refer to the Souvenir manual for details.")]
        Supported
    }

    public enum KtaneTwitchPlaysTagPosition
    {
        [KtaneFilterOption("Automatic")]
        Automatic,
        [KtaneFilterOption("Top-right")]
        TopRight,
        [KtaneFilterOption("Top-left")]
        TopLeft,
        [KtaneFilterOption("Bottom-right")]
        BottomRight,
        [KtaneFilterOption("Bottom-left")]
        BottomLeft
    }

    public enum KtaneTwitchPlaysNeedyScoring
    {
        [KtaneFilterOption("Time-based")]
        Time = 0,
        [KtaneFilterOption("Solve-based")]
        Solves = 1
    }

    public enum KtaneMysteryModuleCompatibility
    {
        [KtaneFilterOption("No conflict")]
        NoConflict,
        [KtaneFilterOption("MM must not hide this")]
        MustNotBeHidden,
        [KtaneFilterOption("MM must not require this")]
        MustNotBeKey,
        [KtaneFilterOption("MM must not use this at all")]
        MustNotBeHiddenOrKey,
        [KtaneFilterOption("MM must auto-solve")]
        RequiresAutoSolve,
    }

    public enum KtaneModuleLicense
    {
        [KtaneFilterOption("The module has its source code released and will follow the module’s license.")]
        OpenSource,
        [KtaneFilterOption("The module may be republished on someone else’s Steam account. Any work may not be reused.")]
        Republishable,
        [KtaneFilterOption("The module may not be republished and any work may not be reused.")]
        Restricted,
    }
}