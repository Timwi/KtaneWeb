using System;

namespace KtaneWeb
{
    public enum KtaneModuleType
    {
        [KtaneFilterOption(nameof(TranslationInfo.moduleTypeRegular), 'R')]
        Regular,
        [KtaneFilterOption(nameof(TranslationInfo.moduleTypeNeedy), 'y')]
        Needy,
        [KtaneFilterOption(nameof(TranslationInfo.moduleTypeHoldable))]
        Holdable,
        [KtaneFilterOption(nameof(TranslationInfo.moduleTypeWidget))]
        Widget
    }

    public enum KtaneModuleOrigin
    {
        [KtaneFilterOption(nameof(TranslationInfo.originVanilla), 'V')]
        Vanilla,
        [KtaneFilterOption(nameof(TranslationInfo.originMods), 'M')]
        Mods
    }

    public enum KtaneModuleDifficulty
    {
        [KtaneFilterOption(nameof(TranslationInfo.moduleDiffVeryEasy))]
        VeryEasy,
        [KtaneFilterOption(nameof(TranslationInfo.moduleDiffEasy))]
        Easy,
        [KtaneFilterOption(nameof(TranslationInfo.moduleDiffMedium))]
        Medium,
        [KtaneFilterOption(nameof(TranslationInfo.moduleDiffHard))]
        Hard,
        [KtaneFilterOption(nameof(TranslationInfo.moduleDiffVeryHard))]
        VeryHard
    }

    public enum KtaneSupport
    {
        [KtaneFilterOption(nameof(TranslationInfo.filterNotSupported))]
        NotSupported,
        [KtaneFilterOption(nameof(TranslationInfo.filterSupported))]
        Supported
    }

    public enum KtaneModuleCompatibility
    {
        [KtaneFilterOption(nameof(TranslationInfo.compatibilityCompatible))]
        Compatible,
        [KtaneFilterOption(nameof(TranslationInfo.compatibilityProblematic))]
        Problematic,
        [KtaneFilterOption(nameof(TranslationInfo.compatibilityUnplayable))]
        Unplayable
    }

    public enum KtaneModuleSouvenir
    {
        [KtaneFilterOption(nameof(TranslationInfo.filterUnexamined)), KtaneSouvenirInfo('U', "We have not yet decided whether this module is a candidate for inclusion in Souvenir.")]
        Unexamined,
        [KtaneFilterOption(nameof(TranslationInfo.filterNotCandidate)), KtaneSouvenirInfo('N', "This module is not a candidate for inclusion in Souvenir.")]
        NotACandidate,
        [KtaneFilterOption(nameof(TranslationInfo.filterConsidered)), KtaneSouvenirInfo('C', "This module may be a candidate for inclusion in Souvenir.")]
        Considered,
        [KtaneFilterOption(nameof(TranslationInfo.filterSupported)), KtaneSouvenirInfo('S', "This module is included in Souvenir. Refer to the Souvenir manual for details.")]
        Supported
    }

    public enum KtaneMysteryModuleCompatibility
    {
        [KtaneFilterOption(nameof(TranslationInfo.filterMMNoConfilct))]
        NoConflict,
        [KtaneFilterOption(nameof(TranslationInfo.filterMMNotHide))]
        MustNotBeHidden,
        [KtaneFilterOption(nameof(TranslationInfo.filterMMNotRequire))]
        MustNotBeKey,
        [KtaneFilterOption(nameof(TranslationInfo.filterMMNotUse))]
        MustNotBeHiddenOrKey,
        [KtaneFilterOption(nameof(TranslationInfo.filterMMAutoSovle))]
        RequiresAutoSolve,
    }

    public enum KtaneModuleLicense
    {
        [KtaneFilterOption(nameof(TranslationInfo.licenseOpenSource))]
        OpenSource,
        [KtaneFilterOption(nameof(TranslationInfo.licenseOpenSourceClone))]
        OpenSourceClone,
        [KtaneFilterOption(nameof(TranslationInfo.licenseRepublishable))]
        Republishable,
        [KtaneFilterOption(nameof(TranslationInfo.licenseRestricted))]
        Restricted,
    }

    public enum KtaneTimeModeOrigin
    {
        [KtaneFilterOption(nameof(TranslationInfo.timeModeUnassigned))]
        Unassigned,
        [KtaneFilterOption(nameof(TranslationInfo.timeModeFromTP))]
        TwitchPlays,
        [KtaneFilterOption(nameof(TranslationInfo.timeModeCommunityScore))]
        Community,
        [KtaneFilterOption(nameof(TranslationInfo.timeModeAssigned))]
        Assigned
    }

    public enum KtaneBossStatus
    {
        [KtaneFilterOption(nameof(TranslationInfo.bossStatusNotBoss))]
        NotABoss,
        [KtaneFilterOption(nameof(TranslationInfo.bossStatusSemiBoss))]
        SemiBoss,
        [KtaneFilterOption(nameof(TranslationInfo.bossStatusFullBoss))]
        FullBoss
    }

    public enum KtaneTutorialStatus
    {
        [KtaneFilterOption(nameof(TranslationInfo.quirkNone))]
        NoTutorial,
        [KtaneFilterOption(nameof(TranslationInfo.filterHasTutorial))]
        HasTutorial
    }

    [Flags]
    public enum KtaneQuirk
    {
        [KtaneFilterOption(nameof(TranslationInfo.quirkSolvesLater)), EditableHelp(nameof(TranslationInfo.quirkSolvesLaterExplain))]
        SolvesAtEnd = 1 << 1,
        [KtaneFilterOption(nameof(TranslationInfo.quirkNeedsSolves)), EditableHelp(nameof(TranslationInfo.quirkNeedsSolvesExplain))]
        NeedsOtherSolves = 1 << 2,
        [KtaneFilterOption(nameof(TranslationInfo.quirkSolvesBefore)), EditableHelp(nameof(TranslationInfo.quirkSolvesBeforeExplain))]
        SolvesBeforeSome = 1 << 3,
        [KtaneFilterOption(nameof(TranslationInfo.quirkSolvesWithOthers)), EditableHelp(nameof(TranslationInfo.quirkSolvesWithOthersExplain))]
        SolvesWithOthers = 1 << 4,
        [KtaneFilterOption(nameof(TranslationInfo.quirkWillSolveSuddenly)), EditableHelp(nameof(TranslationInfo.quirkWillSolveSuddenlyExplain))]
        WillSolveSuddenly = 1 << 5,
        [KtaneFilterOption(nameof(TranslationInfo.quirkPseudoNeedy)), EditableHelp(nameof(TranslationInfo.quirkPseudoNeedyExplain))]
        PseudoNeedy = 1 << 6,
        [KtaneFilterOption(nameof(TranslationInfo.quirkTimeDependent)), EditableHelp(nameof(TranslationInfo.quirkTimeDependentExplain))]
        TimeDependent = 1 << 7,
        [KtaneFilterOption(nameof(TranslationInfo.quirkNeedsImmediateAttention)), EditableHelp(nameof(TranslationInfo.quirkNeedsImmediateAttentionExplain))]
        NeedsImmediateAttention = 1 << 8,
        [KtaneFilterOption(nameof(TranslationInfo.quirkInstantDeath)), EditableHelp(nameof(TranslationInfo.quirkInstantDeathExplain))]
        InstantDeath = 1 << 9
    }
}
