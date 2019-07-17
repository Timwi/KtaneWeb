interface KtaneModuleInfo 
{
    Name: string;
    Description: string;
    ModuleID: string;
    SortKey: string;
    SteamID: string;
    Author: string;

    SourceUrl: string;
    TutorialVideoUrl: string;
    Symbol: string;

    Type: KtaneModuleType;
    Origin: KtaneModuleOrigin;
    Compatibility: KtaneModuleCompatibility;
    CompatibilityExplanation: string;
    Published: string;

    // The following are only relevant for modules (not widgets)
    DefuserDifficulty: KtaneModuleDifficulty | null;
    ExpertDifficulty: KtaneModuleDifficulty | null;

    // null if the module doesn’t support TP. Always null for widgets.
    TwitchPlays: KtaneTwitchPlaysInfo;
    Souvenir: KtaneSouvenirInfo;
    RuleSeedSupport: KtaneSupport;

    // Specifies which modules this module should ignore. Applies to boss and semi-boss modules such as Forget Me Not, Alchemy, Hogwarts, etc.
    Ignore: string[];


    // ** CLIENT-SPECIFIC THINGS (this is not in the C# class declaration) ** //

    // Things sent by the server
    Sheets: string[];
    X: number;
    Y: number;

    // Things used on the client side
    IsVisible: boolean;
    FncsShowHide: ((sh: boolean) => void)[];
    FncsSetHighlight: ((sh: boolean) => void)[];
    FncsSetManualIcon: ((url: string) => void)[];
    FncsSetManualLink: ((url: string) => void)[];
    FncsSetSelectable: ((url: string) => void)[];
    Manuals: KtaneModuleManual[];
    TwitchPlaysInfo: string;
    RuleSeedInfo: string;
    SouvenirInfo: string;
    SelectableLinkUrl: string;

    ViewData: ModuleViewDatas;
}

interface ModuleViewDatas
{
    List?: ModuleListViewData;
    PeriodicTable?: ModulePeriodicTableViewData;
}

interface ModuleViewData
{
    SelectableLink: HTMLElement;
}
interface ModuleListViewData extends ModuleViewData
{
    TableRow: HTMLTableRowElement;
}
interface ModulePeriodicTableViewData extends ModuleViewData
{
}
interface ViewData
{
    Show: () => void;
    Hide: () => void;
    Sort: () => void;
}

interface KtaneModuleManual
{
    Name: string;
    Url: string;
    Icon: string;
}

interface KtaneSouvenirInfo 
{
    Status: KtaneModuleSouvenir;
    Explanation: string;
}

interface KtaneTwitchPlaysInfo 
{
    Score: number;
    ScorePerModule: number;
    ScorePerModuleCap: number;
    NeedyScoring: KtaneTwitchPlaysNeedyScoring | null;
    ScoreExplanation: string;
    TagPosition: KtaneTwitchPlaysTagPosition;
    AutoPin: boolean;
}

type ViewType = 'List' | 'PeriodicTable';

type KtaneModuleType = 'Regular' | 'Needy' | 'Widget';
type KtaneModuleOrigin = 'Vanilla' | 'Mods';
type KtaneModuleDifficulty = 'VeryEasy' | 'Easy' | 'Medium' | 'Hard' | 'VeryHard';
type KtaneSupport = 'NotSupported' | 'Supported';
type KtaneModuleCompatibility = 'Untested' | 'Unplayable' | 'Problematic' | 'Compatible';
type KtaneModuleSouvenir = 'Unexamined' | 'NotACandidate' | 'Considered' | 'Supported';
type KtaneTwitchPlaysTagPosition = 'Automatic' | 'TopRight' | 'TopLeft' | 'BottomRight' | 'BottomLeft';
type KtaneTwitchPlaysNeedyScoring = 'Time' | 'Solves';
