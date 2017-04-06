namespace KtaneWeb
{
    public enum KtaneModuleType
    {
        [KtaneFilterOption("Regular", 'R')]
        Regular,
        [KtaneFilterOption("Needy", 'N')]
        Needy
    }

    public enum KtaneModuleOrigin
    {
        [KtaneFilterOption("Vanilla", 'V')]
        Vanilla,
        [KtaneFilterOption("Mods", 'o')]
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
}