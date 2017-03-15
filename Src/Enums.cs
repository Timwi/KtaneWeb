using System;

namespace KtaneWeb
{
    [KtaneFilter("type", "Type")]
    public enum KtaneModuleType
    {
        [KtaneFilterOption("Regular", 'R')]
        Regular,
        [KtaneFilterOption("Needy", 'N')]
        Needy
    }

    [KtaneFilter("origin", "Origin")]
    public enum KtaneModuleOrigin
    {
        [KtaneFilterOption("Vanilla", 'V')]
        Vanilla,
        [KtaneFilterOption("Mods", 'o')]
        Mods
    }

    [KtaneFilter("difficulty", "Difficulty")]
    public enum KtaneModuleDifficulty
    {
        UnknownDifficulty,
        [KtaneFilterOption("very easy", 'y')]
        VeryEasy,
        [KtaneFilterOption("easy", 'e')]
        Easy,
        [KtaneFilterOption("medium", 'i')]
        Medium,
        [KtaneFilterOption("hard", 'h')]
        Hard,
        [KtaneFilterOption("very hard for defuser", 'd')]
        VeryHardForDefuser,
        [KtaneFilterOption("very hard for expert", 'x')]
        VeryHardForExpert,
        [KtaneFilterOption("very hard for both", 'b')]
        VeryHardForBoth
    }
}