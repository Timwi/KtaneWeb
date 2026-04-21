using System;
using System.Collections.Generic;
using System.Linq;

using RT.Json;
using RT.Serialization;
using RT.Servers;

namespace KtaneWeb {
    public sealed partial class KtanePropellerModule {

        private sealed class MalformedQueryException : ArgumentException {
            public JsonDict Issues { get; private set; }

            public MalformedQueryException(JsonDict query)
                : base("There are errors in your query.")
            {
                this.Issues = query;
            }
        }

        private sealed class APIFilter {

            private string name = "";
            private string moduleID = "";
            private string description = "";
            private string[] authors = new string[]{};

            private string[] tags = new string[]{};
            private KtaneModuleDifficulty defuserDifficultyMinimum = KtaneModuleDifficulty.Trivial;
            private KtaneModuleDifficulty defuserDifficultyMaximum = KtaneModuleDifficulty.Extreme;
            private KtaneModuleDifficulty expertDifficultyMinimum = KtaneModuleDifficulty.Trivial;
            private KtaneModuleDifficulty expertDifficultyMaximum = KtaneModuleDifficulty.Extreme;

            private HashSet<KtaneModuleType> type = new HashSet<KtaneModuleType>(Enum.GetValues(typeof(KtaneModuleType)).Cast<KtaneModuleType>());
            private HashSet<KtaneModuleOrigin> origin = new HashSet<KtaneModuleOrigin>(Enum.GetValues(typeof(KtaneModuleOrigin)).Cast<KtaneModuleOrigin>());
            private HashSet<KtaneModuleIssues> issues = new HashSet<KtaneModuleIssues>(Enum.GetValues(typeof(KtaneModuleIssues)).Cast<KtaneModuleIssues>());

            private bool? twitchPlays = null;
            private bool? ruleSeed = null;

            private HashSet<KtaneModuleSouvenir> souvenir = new HashSet<KtaneModuleSouvenir>(Enum.GetValues(typeof(KtaneModuleSouvenir)).Cast<KtaneModuleSouvenir>());
            private HashSet<KtaneMysteryModuleCompatibility> mysteryModule = new HashSet<KtaneMysteryModuleCompatibility>(Enum.GetValues(typeof(KtaneMysteryModuleCompatibility)).Cast<KtaneMysteryModuleCompatibility>());
            private HashSet<KtaneBossStatus> bossStatus = new HashSet<KtaneBossStatus>(Enum.GetValues(typeof(KtaneBossStatus)).Cast<KtaneBossStatus>());

            private List<string> tutorialLanguages = new List<string>(TranslationInfo.LanguageCodeToName.Keys);
            private List<string> moduleLanguages = new List<string>(TranslationInfo.LanguageCodeToName.Keys);
            // private List<string> manualLanguages = new List<string>(TranslationInfo.LanguageCodeToName.Keys);

            private KtaneQuirk quirks = KtaneQuirk.SolvesAtEnd | KtaneQuirk.NeedsOtherSolves | KtaneQuirk.SolvesBeforeSome | KtaneQuirk.SolvesWithOthers | KtaneQuirk.WillSolveSuddenly | KtaneQuirk.PseudoNeedy | KtaneQuirk.TimeDependent | KtaneQuirk.NeedsImmediateAttention | KtaneQuirk.InstantDeath;
            private KtaneQuirk quirksAvoid = (KtaneQuirk)0;

            private JsonDict checkKey(Dictionary<string, string> keys, string key, JsonValue validParams, JsonDict invalidValues, char split = ' ') {
                if (keys.ContainsKey(key)) {
                    List<string> badValues = new List<string>();
                    foreach (string value in keys[key].Split(split)) {
                        if (validParams[key].IndexOf(value) < 0 && !badValues.Contains(value)) {
                            badValues.Add(value);
                        }
                    }
                    if (badValues.Count > 0) {
                        invalidValues.Add(key, badValues);
                    }
                }
                return invalidValues;
            }

            public APIFilter(Dictionary<string, string> keys) {
                JsonDict errorDict = new JsonDict{
                    {"Error", null},
                    {"Notes", "More than one of each may be specified, separated by a +. If more than one parameter is specified for defuserDifficulty and expertDifficulty, the range of mods that have the lowest and highest specified difficulty will be used."},
                    {"ValidParameters", new JsonDict{
                        {"name", "(anything)"},
                        {"moduleID", "(anything)"},
                        {"description", "(anything)"},
                        {"authors", "(anything)"},
                        {"tags", "(anything)"},
                        {"defuserDifficulty", Enum.GetNames(typeof(KtaneModuleDifficulty)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"expertDifficulty", Enum.GetNames(typeof(KtaneModuleDifficulty)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"type", Enum.GetNames(typeof(KtaneModuleType)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"origin", Enum.GetNames(typeof(KtaneModuleOrigin)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"issues", Enum.GetNames(typeof(KtaneModuleIssues)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"twitchPlays", Enum.GetNames(typeof(KtaneSupport)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"ruleSeed", Enum.GetNames(typeof(KtaneSupport)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"souvenir", Enum.GetNames(typeof(KtaneModuleSouvenir)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"mysteryModule", Enum.GetNames(typeof(KtaneMysteryModuleCompatibility)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"bossStatus", Enum.GetNames(typeof(KtaneBossStatus)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"tutorialLanguages", new List<string>(TranslationInfo.LanguageCodeToName.Keys).ToArray()},
                        {"moduleLanguages", new List<string>(TranslationInfo.LanguageCodeToName.Keys).ToArray()},
                        // {"manualLanguages", new List<string>(TranslationInfo.LanguageCodeToName.Keys).ToArray()},
                        {"quirks", Enum.GetNames(typeof(KtaneQuirk)).Select(x => x.ToLowerInvariant()).ToArray()},
                        {"quirksAvoid", Enum.GetNames(typeof(KtaneQuirk)).Select(x => x.ToLowerInvariant()).ToArray()}
                    }
                    }
                };

                if (keys.Count == 0) {
                    errorDict["Error"] = "Must provide parameters.";
                    throw new MalformedQueryException(errorDict);
                }

                JsonDict queryErrors = new JsonDict();

                if (keys.Any(x => !errorDict["ValidParameters"].ContainsKey(x.Key))) {
                    // Console.WriteLine(string.Join(", ", keys.Where(x => !errorDict["ValidParameters"].ContainsKey(x.Key)).Select(x => x.Key).ToArray()));
                    errorDict["Error"] = "Invalid parameters passed.";
                    errorDict.Add("InvalidParameters", keys.Where(x => !errorDict["ValidParameters"].ContainsKey(x.Key)).Select(x => x.Key).ToArray());
                }

                this.name = keys.ContainsKey("name") ? keys["name"] : "";
                this.moduleID = keys.ContainsKey("moduleID") ? keys["moduleID"] : "";
                this.description = keys.ContainsKey("description") ? keys["description"] : "";
                this.authors = keys.ContainsKey("authors") ? keys["authors"].Split(' ') : new string[]{};
                this.tags = keys.ContainsKey("tags") ? keys["tags"].Split(' ') : new string[]{};

                if (keys.ContainsKey("defuserDifficulty")) {
                    queryErrors = checkKey(keys, "defuserDifficulty", errorDict["ValidParameters"], queryErrors);
                    this.defuserDifficultyMinimum = (KtaneModuleDifficulty)(keys["defuserDifficulty"].Split(' ').Select(x => ((List<string>)(errorDict["ValidParameters"]["defuserDifficulty"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant())).Min());
                    this.defuserDifficultyMaximum = (KtaneModuleDifficulty)(keys["defuserDifficulty"].Split(' ').Select(x => ((List<string>)(errorDict["ValidParameters"]["defuserDifficulty"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant())).Max());
                } else {
                    this.defuserDifficultyMinimum = KtaneModuleDifficulty.Trivial;
                    this.defuserDifficultyMaximum = KtaneModuleDifficulty.Extreme;
                }

                if (keys.ContainsKey("expertDifficulty")) {
                    queryErrors = checkKey(keys, "expertDifficulty", errorDict["ValidParameters"], queryErrors);
                    this.expertDifficultyMinimum = (KtaneModuleDifficulty)(keys["expertDifficulty"].Split(' ').Select(x => ((List<string>)(errorDict["ValidParameters"]["expertDifficulty"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant())).Min());
                    this.expertDifficultyMaximum = (KtaneModuleDifficulty)(keys["expertDifficulty"].Split(' ').Select(x => ((List<string>)(errorDict["ValidParameters"]["expertDifficulty"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant())).Max());
                } else {
                    this.expertDifficultyMinimum = KtaneModuleDifficulty.Trivial;
                    this.expertDifficultyMaximum = KtaneModuleDifficulty.Extreme;
                }

                queryErrors = checkKey(keys, "type", errorDict["ValidParameters"], queryErrors);
                this.type = new HashSet<KtaneModuleType>((IEnumerable<KtaneModuleType>)(keys.ContainsKey("type") ? keys["type"].Split(' ').Select(x => (KtaneModuleType)(((List<string>)(errorDict["ValidParameters"]["type"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant()))).Cast<KtaneModuleType>().ToArray() : Enum.GetValues(typeof(KtaneModuleType))));

                queryErrors = checkKey(keys, "origin", errorDict["ValidParameters"], queryErrors);
                this.origin = new HashSet<KtaneModuleOrigin>((IEnumerable<KtaneModuleOrigin>)(keys.ContainsKey("origin") ? keys["origin"].Split(' ').Select(x => (KtaneModuleOrigin)(((List<string>)(errorDict["ValidParameters"]["origin"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant()))).Cast<KtaneModuleOrigin>().ToArray() : Enum.GetValues(typeof(KtaneModuleOrigin))));

                queryErrors = checkKey(keys, "issues", errorDict["ValidParameters"], queryErrors);
                this.issues = new HashSet<KtaneModuleIssues>((IEnumerable<KtaneModuleIssues>)(keys.ContainsKey("issues") ? keys["issues"].Split(' ').Select(x => (KtaneModuleIssues)(((List<string>)(errorDict["ValidParameters"]["issues"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant()))).Cast<KtaneModuleIssues>().ToArray() : Enum.GetValues(typeof(KtaneModuleIssues))));

                queryErrors = checkKey(keys, "twitchPlays", errorDict["ValidParameters"], queryErrors);
                this.twitchPlays = keys.ContainsKey("twitchPlays") ? (keys["twitchPlays"].ToLowerInvariant() == "supported") : null;

                queryErrors = checkKey(keys, "ruleSeed", errorDict["ValidParameters"], queryErrors);
                this.ruleSeed = keys.ContainsKey("ruleSeed") ? (keys["ruleSeed"].ToLowerInvariant() == "supported") : null;

                queryErrors = checkKey(keys, "souvenir", errorDict["ValidParameters"], queryErrors);
                this.souvenir = new HashSet<KtaneModuleSouvenir>((IEnumerable<KtaneModuleSouvenir>)(keys.ContainsKey("souvenir") ? keys["souvenir"].Split(' ').Select(x => (KtaneModuleSouvenir)(((List<string>)(errorDict["ValidParameters"]["souvenir"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant()))).Cast<KtaneModuleSouvenir>().ToArray() : Enum.GetValues(typeof(KtaneModuleSouvenir))));

                queryErrors = checkKey(keys, "mysteryModule", errorDict["ValidParameters"], queryErrors);
                this.mysteryModule = new HashSet<KtaneMysteryModuleCompatibility>((IEnumerable<KtaneMysteryModuleCompatibility>)(keys.ContainsKey("mysteryModule") ? keys["mysteryModule"].Split(' ').Select(x => (KtaneMysteryModuleCompatibility)(((List<string>)(errorDict["ValidParameters"]["mysteryModule"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant()))).Cast<KtaneMysteryModuleCompatibility>().ToArray() : Enum.GetValues(typeof(KtaneMysteryModuleCompatibility))));

                queryErrors = checkKey(keys, "bossStatus", errorDict["ValidParameters"], queryErrors);
                this.bossStatus = new HashSet<KtaneBossStatus>((IEnumerable<KtaneBossStatus>)(keys.ContainsKey("bossStatus") ? keys["bossStatus"].Split(' ').Select(x => (KtaneBossStatus)(((List<string>)(errorDict["ValidParameters"]["bossStatus"])).Select(y => y.ToLowerInvariant()).ToList().IndexOf(x.ToLowerInvariant()))).Cast<KtaneBossStatus>().ToArray() : Enum.GetValues(typeof(KtaneBossStatus))));

                queryErrors = checkKey(keys, "tutorialLanguages", errorDict["ValidParameters"], queryErrors);
                this.tutorialLanguages = new List<string>(keys.ContainsKey("tutorialLanguages") ? keys["tutorialLanguages"].Split(' ') : TranslationInfo.LanguageCodeToName.Keys);

                queryErrors = checkKey(keys, "moduleLanguages", errorDict["ValidParameters"], queryErrors);
                this.moduleLanguages = new List<string>(keys.ContainsKey("moduleLanguages") ? keys["moduleLanguages"].Split(' ') : TranslationInfo.LanguageCodeToName.Keys);

                /*
                queryErrors = checkKey(keys, "manualLanguages", errorDict["ValidParameters"], queryErrors);
                this.manualLanguages = new List<string>(keys.ContainsKey("manualLanguages") ? keys["manualLanguages"].Split(' ') : TranslationInfo.LanguageCodeToName.Keys);
                */

                queryErrors = checkKey(keys, "quirks", errorDict["ValidParameters"], queryErrors);
                this.quirks = (KtaneQuirk)0;
                string[] quirkNames = Enum.GetNames(typeof(KtaneQuirk)).Select(x => x.ToLowerInvariant()).ToArray();
                if (keys.ContainsKey("quirks")) {
                    foreach (string quirk in keys["quirks"].Split(' ')) {
                        if (Array.IndexOf(quirkNames, quirk.ToLowerInvariant()) >= 0) {
                            this.quirks |= (KtaneQuirk)(1 << (Array.IndexOf(quirkNames, quirk.ToLowerInvariant()) + 1));
                        }
                    }
                }

                this.quirksAvoid = (KtaneQuirk)0;
                if (keys.ContainsKey("quirksAvoid")) {
                    foreach (string quirk in keys["quirksAvoid"].Split(' ')) {
                        if (Array.IndexOf(quirkNames, quirk.ToLowerInvariant()) >= 0) {
                            this.quirksAvoid |= (KtaneQuirk)(1 << (Array.IndexOf(quirkNames, quirk.ToLowerInvariant()) + 1));
                        }
                    }
                }

                KtaneQuirk quirkOverlap = this.quirks & this.quirksAvoid;

                if (quirkOverlap > 0) {
                    List<string> overlapNames = new List<string>();
                    for (int i = 0; i < quirkNames.Length; i++) {
                        if (((int)(quirkOverlap) & (1 << (i + 1))) != 0) {
                            overlapNames.Add(quirkNames[i]);
                        }
                    }
                    queryErrors.Add("QuirkOverlap", overlapNames);
                }

                if (errorDict.ContainsKey("InvalidParameters") && queryErrors.Count > 0) {
                    errorDict["Error"] = "Invalid parameters passed, and invalid values given to parameters.";
                    errorDict.Add("QueryErrors", queryErrors);
                    throw new MalformedQueryException(errorDict);
                } else if (errorDict.ContainsKey("InvalidParameters") && queryErrors.Count == 0) {
                    errorDict["Error"] = "Invalid parameters passed.";
                    throw new MalformedQueryException(errorDict);
                } else if (!errorDict.ContainsKey("InvalidParameters") && queryErrors.Count > 0) {
                    errorDict["Error"] = "Invalid values given to parameters.";
                    errorDict.Add("QueryErrors", queryErrors);
                    throw new MalformedQueryException(errorDict);

                }
            }

            public bool Matches(KtaneModuleInfo module) {
                if (this.name != string.Empty && !module.Name.ToLowerInvariant().Contains(this.name.ToLowerInvariant())) {
                    // Console.WriteLine($"Query name {this.name.ToLowerInvariant()} did not match module name {module.Name.ToLowerInvariant()}.");
                    return false;
                }
                if (this.moduleID != string.Empty && (module.ModuleID == null || !module.ModuleID.ToLowerInvariant().Contains(this.moduleID.ToLowerInvariant()))) {
                    // Console.WriteLine($"Query ID {this.moduleID.ToLowerInvariant()} did not match module ID {module.ModuleID?.ToLowerInvariant() ?? $"module with name {module.Name}"}.");
                    return false;
                }
                if (this.description != string.Empty) {
                    bool notInAnyDesc = true;
                    foreach (string language in this.moduleLanguages) {
                        if (module.Descriptions.Any(x => x.Language == TranslationInfo.LanguageCodeToName[language]) && module.Descriptions.Where(x => x.Language == TranslationInfo.LanguageCodeToName[language]).Any(x => x.Description.ToLowerInvariant().Contains(this.description.ToLowerInvariant()))) {
                            notInAnyDesc = false;
                            break;
                        }
                    }
                    if (notInAnyDesc) {
                        // Console.WriteLine($"Query description {this.description} was not found in any language description of {module.Name}.");
                        return false;
                    }
                }
                if (this.authors.Length > 0 && (module.Contributors?.ToAllAuthorString() ?? module.Author) != null && !(module.Contributors?.ToAllAuthorString() ?? module.Author ?? "").Split(',').Select(x => x.Trim()).Any(x => this.authors.Any(y => y.ToLowerInvariant().Contains(x.ToLowerInvariant())))) {
                    // Console.WriteLine($"Query authors {string.Join(", ", this.authors)} were not found in {module.Name}.");
                    return false;
                }
                if (this.tags.Length > 0 && !module.Descriptions.Where(x => x.Tags != null).Select(x => x.Tags.Split(',').Select(y => y.Trim())).Any(x => x.Any(y => this.tags.Any(z => y.ToLowerInvariant().Contains(z.ToLowerInvariant()))))) {
                    // Console.WriteLine($"Query tags {string.Join(", ", this.tags)} were not found in {module.Name}.");
                    return false;
                }
                if (module.DefuserDifficulty != null && (module.DefuserDifficulty < this.defuserDifficultyMinimum || module.DefuserDifficulty > this.defuserDifficultyMaximum)) {
                    // Console.WriteLine($"Defuser difficulty {module.DefuserDifficulty} of module {module.Name} was not within the range of {this.defuserDifficultyMinimum} to {this.defuserDifficultyMaximum}.");
                    return false;
                }
                if (module.ExpertDifficulty != null && (module.ExpertDifficulty < this.expertDifficultyMinimum || module.ExpertDifficulty > this.expertDifficultyMaximum)) {
                    // Console.WriteLine($"Expert difficulty {module.ExpertDifficulty} of module {module.Name} was not within the range of {this.expertDifficultyMinimum} to {this.expertDifficultyMaximum}.");
                    return false;
                }
                if (!this.type.Contains(module.Type)) {
                    // Console.WriteLine($"{module.Name} is of type {module.Type}, not any of {string.Join(", ", this.type.ToArray())}.");
                    return false;
                }
                if (!this.origin.Contains(module.Origin)) {
                    // Console.WriteLine($"{module.Name} is of origin {module.Origin}, not any of {string.Join(", ", this.origin.ToArray())}.");
                    return false;
                }
                if (!this.issues.Contains(module.Issues)) {
                    // Console.WriteLine($"{module.Name} is has issue {module.Issues}, not any of {string.Join(", ", this.issues.ToArray())}.");
                    return false;
                }
                if (this.twitchPlays != null && this.twitchPlays != (module.TwitchPlaysScore != null)) {
                    if ((bool)this.twitchPlays) {
                        // Console.WriteLine($"{module.Name} does not support Twitch Plays, when the query requested modules that do.");
                    } else {
                        // Console.WriteLine($"{module.Name} does support Twitch Plays, when the query requested modules that do not.");
                    }
                    return false;
                }
                if (this.ruleSeed != null && this.ruleSeed != (module.RuleSeedSupport == KtaneSupport.Supported)) {
                    if ((bool)this.ruleSeed) {
                        // Console.WriteLine($"{module.Name} does not support rule seed, when the query requested modules that do.");
                    } else {
                        // Console.WriteLine($"{module.Name} does support rule seed, when the query requested modules that do not.");
                    }
                    return false;
                }

                if (module.Souvenir != null && !this.souvenir.Contains(module.Souvenir.Status)) {
                    // Console.WriteLine($"{module.Name} has Souvenir status {module.Souvenir.Status}, not any of {string.Join(", ", this.souvenir.ToArray())}.");
                    return false;
                }

                if (!this.mysteryModule.Contains(module.MysteryModule)) {
                    // Console.WriteLine($"{module.Name} has Mystery Module status {module.MysteryModule}, not any of {string.Join(", ", this.mysteryModule.ToArray())}.");
                    return false;
                }
                if (!this.bossStatus.Contains(module.BossStatus)) {
                    // Console.WriteLine($"{module.Name} has boss status {module.BossStatus}, not any of {string.Join(", ", this.bossStatus.ToArray())}.");
                    return false;
                }
                
                if (module.TutorialVideos != null && !module.TutorialVideos.Select(x => x.Language).Any(x => this.tutorialLanguages.Any(y => TranslationInfo.LanguageCodeToName[y] == x))) {
                    // Console.WriteLine($"{module.Name} does not have tutorials in any of {string.Join(", ", this.tutorialLanguages.Select(x => TranslationInfo.LanguageCodeToName[x]).ToArray())}.");
                    return false;
                }
                if (module.Descriptions != null && !module.Descriptions.Select(x => x.Language).Any(x => this.moduleLanguages.Any(y => TranslationInfo.LanguageCodeToName[y] == x))) {
                    // Console.WriteLine($"{module.Name} does not have descriptions in any of {string.Join(", ", this.moduleLanguages.Select(x => TranslationInfo.LanguageCodeToName[x]).ToArray())}.");
                    return false;
                }

                /*
                if (!module["Sheets"].Any(x => this.manualLanguages.Contains(y => y == "en" ? module.ModuleID != "epelleMoiCa" : x.Contains(TranslationInfo.LanguageCodeToName[y])))) {
                    return false;
                }
                */

                if (this.quirks != 0) {
                    if (module.Quirks != 0 && (int)(this.quirks & module.Quirks) == 0) {
                        // Console.WriteLine($"{module.Name} has quirk value {module.Quirks}, which does not align with {this.quirks}.");
                        return false;
                    } else if (module.Quirks == 0) {
                        // Console.WriteLine($"{module.Name} has no quirks, which does not align with {this.quirks}.");
                        return false;
                    }
                }

                if (this.quirksAvoid != 0) {
                    if (module.Quirks != 0 && (int)(this.quirksAvoid & module.Quirks) != 0) {
                        // Console.WriteLine($"{module.Name} has quirk value {module.Quirks}, which does not align with avoiding {this.quirksAvoid}.");
                        return false;
                    } else if (module.Quirks == 0) {
                        // Console.WriteLine($"{module.Name} has no quirks, which does not align with avoiding {this.quirksAvoid}.");
                        return false;
                    }
                }
                // Console.WriteLine($"{module.Name} has met the criteria.");
                return true;
            }
        }

        private HttpResponse API(HttpRequest req) {
            Dictionary<string, string> query = req.Url.Query.ToDictionary(x => x.Key, x => x.Value);

            try {
                APIFilter filter = new APIFilter(query);
                JsonList result = new JsonList(_moduleInfoCache.Modules.Where(filter.Matches).Select(x => ClassifyJson.Serialize(x)));
                return HttpResponse.Json(result, HttpStatusCode._200_OK);
            } catch (MalformedQueryException e) {
                return HttpResponse.Json(e.Issues, HttpStatusCode._400_BadRequest);
            }

        }
    }
}
