const SHEETS_KEY = "AIzaSyA7OsUUdO1ZfOolQ1P_dHb8T8EJqUeyowk";

// Handle access to localStorage
let lStorage = localStorage;

try
{
    localStorage.setItem("testStorage", "testData");
    localStorage.removeItem("testStorage");
}
catch (e)
{
    console.log("Local storage not available:", e);
    lStorage = {
        storage: {},
        key: function(index)
        {
            let counter = 0;
            for (const key in this.storage)
            {
                if (counter == index) return key;
                else counter++;
            }
            return null;
        },
        getItem: function(key)
        {
            return this.storage[key] || null;
        },
        setItem: function(key, data)
        {
            this.storage[key] = data;
        },
        removeItem: function(key)
        {
            delete this.storage[key];
        },
        clear: function()
        {
            this.storage = {};
            this.length = 0;
        },
        get length()
        {
            let length = 0;
            for (let key in this.storage)
            {
                if (this.storage.hasOwnProperty(key))
                {
                    length += 1;
                }
            }
            return length;
        }
    };
}

// Change the theme CSS before the page renders
let theme = lStorage.getItem("theme");
if (!(theme in Ktane.Themes))
    theme = null;
if (theme in Ktane.Themes)
    document.getElementById("theme-css").setAttribute('href', Ktane.Themes[theme]);
else
    document.getElementById("theme-css").setAttribute('href', '');

// Function to decide on a translated string based on a number (e.g. singular/plural)
let languageNumberSystem = null;
function trN(key, n, replaceWhat)
{
    languageNumberSystem = languageNumberSystem ?? eval(translation['numberSystemJs']);
    let ix = +languageNumberSystem(+n), strs = translation[key];
    return (ix >= 0 && ix < strs.length ? strs[ix] : strs[strs.length - 1]).replace(replaceWhat ?? '{0}', +n);
}

function el(tagName, className, ...args)
{
    const element = document.createElement(tagName);
    if (className)
        element.className = className;
    for (const arg of args)
    {
        if (arg instanceof HTMLElement)
            element.appendChild(arg);
        else if (typeof arg !== "object")
            element.appendChild(document.createTextNode(arg));
        else
            for (const attr in arg)
            {
                if (typeof arg[attr] === 'function')
                    element[attr] = arg[attr];
                else if (arg[attr] !== undefined && arg[attr] !== null)
                    element.setAttribute(attr, arg[attr]);
            }
    }
    return element;
}

// Add language options for the website
function setLanguageSelector(selectedLanguage)
{
    const languageSelector = document.getElementById("lang-selector");
    languageSelector.onchange = function()
    {
        document.cookie = `lang=${languageSelector.value}`;
        window.location.reload();
    };

    let validLanguage = false;
    for (const langCode of Object.keys(Ktane.Languages))
    {
        validLanguage = validLanguage || selectedLanguage === langCode;
        languageSelector.appendChild(el("option", null, Ktane.Languages[langCode], selectedLanguage === langCode ? { selected: true, value: langCode } : { value: langCode }));
    }

    if (!validLanguage)
        languageSelector.value = 'en';
}

const contributorSortPriority = ['Developer', 'Manual', '*', 'Maintainer'];
function contributorSortIndex(a) { let p = contributorSortPriority.indexOf(a); return p === -1 ? contributorSortPriority.indexOf('*') : p; }
function contributorSort(a, b)
{
    let e1 = Array.isArray(a) ? a[0] : a, e2 = Array.isArray(b) ? b[0] : b;
    let p1 = contributorSortIndex(e1), p2 = contributorSortIndex(e2);
    return p1 === p2 ? e1.localeCompare(e2) : p1 - p2;
}

function initializePage(modules, initIcons, initDocDirs, initFilters, initSelectables, souvenirAttributes, moduleLoadExceptions, restrictedManuals, contactInfo)
{
    for (let exception of moduleLoadExceptions)
        console.error(exception);

    let pageLang, langCode;
    if ((pageLang = window.location.search.match(/lang=([^?&]+)/)) && Ktane.Languages[pageLang[1]] !== undefined)
        langCode = pageLang[1];
    else if (document.cookie && (pageLang = /(?:^|;)lang=([^=;]+)(?:$|;)/.exec(document.cookie)) && Ktane.Languages[pageLang[1]] !== undefined)
        langCode = pageLang[1];
    else
        langCode = null;

    if (!langCode)
    {
        pageLang = null;
        setLanguageSelector("en");
    }
    else
    {
        pageLang = Ktane.Languages[langCode];
        setLanguageSelector(langCode);
    }

    let filter = {};
    try { filter = JSON.parse(lStorage.getItem('filters') || '{}') || {}; }
    catch (exc) { }
    let selectable = lStorage.getItem('selectable') || 'manual';
    if (initSelectables.map(sel => sel.PropName).indexOf(selectable) === -1)
        selectable = 'manual';
    let preferredManuals = {};
    try { preferredManuals = JSON.parse(lStorage.getItem('preferredManuals') || '{}') || {}; }
    catch (exc) { }
    let preferredLanguages = {};
    try { preferredLanguages = JSON.parse(lStorage.getItem('preferredLanguages') || '{}') || {}; }
    catch (exc) { }

    document.getElementById('rule-seed-input').value = `${+lStorage.getItem('ruleseed') || 1}`;
    function updateRuleseed()
    {
        setLinksAndPreferredManuals();
        let seed = document.getElementById('rule-seed-input').value;
        if (seed === null || seed === '' || (seed | 0) < 0)
            seed = 1;
        else
            seed = (seed | 0);
        document.getElementById('rule-seed-input').value = seed;
        lStorage.setItem('ruleseed', `${seed}`);
        if (seed === 1)
        {
            document.body.classList.remove('rule-seed-active');
            document.getElementById('rule-seed-number').innerText = '';
        }
        else
        {
            document.body.classList.add('rule-seed-active');
            document.getElementById('rule-seed-number').innerText = ' = ' + seed;
            document.getElementById('rule-seed-mobile').innerText = seed.toString();
        }
    }

    function compare(a, b, rev) { return (rev ? -1 : 1) * ((a < b) ? -1 : ((a > b) ? 1 : 0)); }
    let defdiffFilterValues = initFilters.filter(f => f.id === 'defdiff')[0].values;
    let expdiffFilterValues = initFilters.filter(f => f.id === 'expdiff')[0].values;
    let sorts = {
        'name': { fnc: function(mod) { return mod.SortKey.toLowerCase(); }, reverse: false, bodyCss: 'sort-name', radioButton: '#sort-name' },
        'defdiff': { fnc: function(mod) { return defdiffFilterValues.indexOf(mod.DefuserDifficulty); }, reverse: false, bodyCss: 'sort-defdiff', radioButton: '#sort-defuser-difficulty' },
        'expdiff': { fnc: function(mod) { return expdiffFilterValues.indexOf(mod.ExpertDifficulty); }, reverse: false, bodyCss: 'sort-expdiff', radioButton: '#sort-expert-difficulty' },
        'twitchscore': { fnc: function(mod) { return mod.TwitchPlays ? mod.TwitchPlays.Score : 0; }, reverse: false, bodyCss: 'sort-twitch-score', radioButton: '#sort-twitch-score' },
        'timemodescore': { fnc: function(mod) { return mod.TimeMode ? mod.TimeMode.Score : 0; }, reverse: false, bodyCss: 'sort-time-mode-score', radioButton: '#sort-time-mode-score' },
        'published': { fnc: function(mod) { return mod.Published; }, reverse: true, bodyCss: 'sort-published', radioButton: '#sort-published' }
    };
    let sort = lStorage.getItem('sort') || 'published';
    if (!(sort in sorts))
        sort = 'published';
    let reverse = lStorage.getItem('sort-reverse') == "true" || false;
    let invertSearch = lStorage.getItem('invert-search-results') == "true" || false;
    $("#filter-invert-search").prop("checked", invertSearch);

    let defaultDisplayOptions = ['author', 'type', 'difficulty', 'description', 'tags', 'published', 'twitch', 'souvenir', 'rule-seed', 'restricted-manuals'];
    let displayOptions = defaultDisplayOptions;
    try { displayOptions = JSON.parse(lStorage.getItem('display')) || defaultDisplayOptions; } catch (exc) { }

    let resultsMode = lStorage.getItem('resultsMode') || 'hide';
    let resultsLimit = lStorage.getItem('resultsLimit') || 20;

    let validSearchOptions = ['names', 'authors', 'descriptions'];
    let defaultSearchOptions = ['names'];
    let searchOptions = defaultSearchOptions;
    try { searchOptions = JSON.parse(lStorage.getItem('searchOptions')) || defaultSearchOptions; } catch (exc) { }

    let validViews = ['List', 'PeriodicTable'];
    let view = lStorage.getItem('view');
    if (validViews.indexOf(view) === -1)
        view = 'List';

    let profileVetoList = null;
    let missionList = null;

    let version = JSON.parse(lStorage.getItem('version')) || 0;
    if (version < 2)
    {
        sort = 'published';
        selectable = 'manual';
        displayOptions = defaultDisplayOptions;
        filter = {};
        view = 'List';
    }
    lStorage.setItem('version', '2');

    // Refers to a module if the “Find” box contains the exact name or Periodic Table symbol for a module
    let showAtTopOfResults = [];

    let selectedIndex = 0;
    function updateSearchHighlight()
    {
        let visible = modules.filter(mod => mod.IsVisible);
        if (selectedIndex < 0)
            selectedIndex = 0;
        if (selectedIndex >= visible.length)
            selectedIndex = visible.length - 1;

        if (resultsMode == 'scroll')
        {
            for (let mod of modules)
                for (let fnc of mod.FncsSetHighlight)
                    fnc(false);

            for (let fnc of visible[selectedIndex].FncsSetHighlight)
                fnc(true);
        }
        else if (resultsMode == 'hide')
        {
            for (let i = 0; i < visible.length; i++)
                for (let fnc of visible[i].FncsSetHighlight)
                    fnc(i === selectedIndex);
        }
    }

    function setSelectable(sel)
    {
        selectable = sel;
        $('label.set-selectable').removeClass('selected');
        $('label#selectable-label-' + sel).addClass('selected');
        $('#selectable-' + sel).prop('checked', true);
        lStorage.setItem('selectable', sel);
        updateFilter();
        setLinksAndPreferredManuals();
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();
    }

    function setLanguages(langs)
    {
        preferredLanguages = langs;
        for (const lang of languages)
            $(`[data-lang="${lang}"]`).prop('checked', langs[lang] === undefined ? true : langs[lang]);
        lStorage.setItem('preferredLanguages', JSON.stringify(langs));
        updateFilter();
    }

    function setSort(srt, rvrse)
    {
        sort = srt;
        lStorage.setItem('sort', srt);

        reverse = rvrse;
        lStorage.setItem('sort-reverse', rvrse);

        modules.sort(function(a, b)
        {
            if (showAtTopOfResults.includes(a))
                return (showAtTopOfResults.includes(b) && showAtTopOfResults.indexOf(a) > showAtTopOfResults.indexOf(b)) ? 1 : -1;
            if (showAtTopOfResults.includes(b))
                return 1;
            let c = compare(sorts[srt].fnc(a), sorts[srt].fnc(b), sorts[srt].reverse ^ rvrse);
            return (c === 0) ? compare(a.SortKey, b.SortKey, rvrse) : c;
        });

        viewsReady.get(view).Sort();
        updateFilter();

        $(document.body).removeClass(document.body.className.split(' ').filter(cls => cls.startsWith('sort-')).join(' ')).addClass(sorts[srt].bodyCss);
        $(sorts[srt].radioButton).prop('checked', true);
        $('input#sort-reverse').prop('checked', rvrse);
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();
    }

    let displayRestrictedManuals = true;
    restrictedManualNames = restrictedManuals.Restricted.map(n => n.replace("\\", " "));

    function setDisplayOptions(set)
    {
        displayOptions = (set instanceof Array) ? set.filter(function(x) { return Ktane.InitDisplays.indexOf(x) !== -1; }) : defaultDisplayOptions;
        $(document.body).removeClass(document.body.className.split(' ').filter(function(x) { return x.startsWith('display-'); }).join(' '));
        $('input.display').prop('checked', false);
        $(document.body).addClass(displayOptions.map(function(x) { return "display-" + x; }).join(' '));
        $(displayOptions.map(function(x) { return '#display-' + x; }).join(',')).prop('checked', true);
        lStorage.setItem('display', JSON.stringify(displayOptions));
        displayRestrictedManuals = document.getElementById('display-restricted-manuals').checked;
    }

    document.getElementById('option-include-symbol').checked = (lStorage.getItem('option-include-symbol') || '1') === '1';
    document.getElementById('option-include-steam-id').checked = (lStorage.getItem('option-include-steam-id') || '1') === '1';
    document.getElementById('option-include-module-id').checked = (lStorage.getItem('option-include-module-id') || '1') === '1';
    function setSearchOptions(set)
    {
        searchOptions = (set instanceof Array) ? set.filter(function(x) { return validSearchOptions.indexOf(x) !== -1; }) : defaultSearchOptions;
        $('input.search-option-input').prop('checked', false);
        $(searchOptions.map(function(x) { return '#search-' + x; }).join(',')).prop('checked', true);
        lStorage.setItem('searchOptions', JSON.stringify(searchOptions));
        lStorage.setItem('option-include-symbol', document.getElementById('option-include-symbol').checked ? '1' : '0');
        lStorage.setItem('option-include-steam-id', document.getElementById('option-include-steam-id').checked ? '1' : '0');
        lStorage.setItem('option-include-module-id', document.getElementById('option-include-module-id').checked ? '1' : '0');
    }

    function setResultsMode(mode, limit)
    {
        // Show all modules again to undo any effects of the previous results mode.
        if (mode !== resultsMode)
            for (const mod of modules)
                for (const fnc of mod.FncsShowHide)
                    fnc(true);

        lStorage.setItem('resultsMode', mode);
        lStorage.setItem('resultsLimit', limit);
        $('#results-' + mode).prop('checked', true);
        $('#results-limit')[0].value = limit;
        resultsMode = mode;
        resultsLimit = limit;

        // Update each module's show/hide state so that it's correct for the new mode.
        updateFilter();
    }

    function setTheme(theme)
    {
        if (theme === null || !(theme in Ktane.Themes))
        {
            lStorage.removeItem('theme');
            theme = null;
        }
        else
            lStorage.setItem('theme', theme);
        $('#theme-css').attr('href', theme in Ktane.Themes ? Ktane.Themes[theme] : '');
        $('#theme-' + (theme || 'default')).prop('checked', true);
    }

    function setProfile(file)
    {
        try
        {
            const reader = new FileReader();
            reader.onload = () =>
            {
                if (typeof reader.result != 'string')
                    return;
                const profile = JSON.parse(reader.result);
                if (profile.Operation === 0 && profile.EnabledList)
                    profile.DisabledList = modules.filter(mod => !profile.EnabledList.includes(mod.ModuleID)).map(mod => mod.ModuleID);

                if (profile.DisabledList)
                {
                    profileVetoList = profile.DisabledList;
                    $(".filter-profile-enabled-text").text('\u00a0' + translation.filterProfileEnabled[0] + file.name + translation.filterProfileEnabled[1]);
                    $(".filter-profile-disabled-text").text('\u00a0' + translation.filterProfileVetoed[0] + file.name + translation.filterProfileVetoed[1]);
                    $("#filters").removeClass("no-profile-selected");
                    $('#filter-profile-enabled').prop('checked', true);
                    $('#filter-profile-disabled').prop('checked', false);
                    updateFilter();
                }
            };
            reader.readAsText(file);
        }
        catch (error)
        {
            console.error("Unable to set profile: ", error);
        }
    }

    let viewsReady = new Map();

    function getIssuesText(mod)
    {
        const issueKinds = {
            Gameplay: 'This module has a problem that prevents it from being played reliably.',
            Superficial: 'This module exhibits a cosmetic or other minor problem that doesn’t affect its playability.',
        };

        if (issueKinds[mod.Issues] === undefined)
            return;
        if (mod.IssuesExplanation !== undefined)
            return `${issueKinds[mod.Issues]} ${mod.IssuesExplanation}`;
        return issueKinds[mod.Issues];
    }

    function setElemCbsRestricted(elem, restricted)
    {
        elem.classList.toggle('cbs-restricted', restricted);
        let query = elem.querySelector('td') ? '.mod-name' : '.mod-icon';
        if (restricted)
            elem.querySelector(query).setAttribute('title', 'CBS Restricted');
        else
            elem.querySelector(query).removeAttribute('title');
    }

    function createView(newView)
    {
        if (viewsReady.has(newView))
            return true;

        function setIssuesTooltip(element, mod)
        {
            let title = getIssuesText(mod);
            if (title && title.length)
                element.setAttribute('title', title);
        }

        const services = {
            "Bluesky": "bsky.app/profile/{}",
            "DeviantArt": "deviantart.com/{}",
            "Facebook": "facebook.com/{}",
            "GitHub": "github.com/{}",
            "Itaku": "itaku.ee/profile/{}",
            "Newgrounds": "{}.newgrounds.com",
            "Reddit": "reddit.com/u/{}",
            "Steam": "steamcommunity.com/{}",
            "Twitch": "twitch.tv/{}",
            "Website": "{}",
            "X": "x.com/{}",
            "YouTube": "youtube.com/{}"
        };

        function makeAuthorElement(mod)
        {
            const title = mod.Contributors === undefined ? '' : Object.entries(mod.Contributors).filter(([_, names]) => names != null).sort(contributorSort).map(([role, names]) => `${role}: ${names.join(', ')}`).join('\n');
            return el('div', 'inf-author inf', el('span', 'contributors', mod.Author), { title: title });
        }

        function makeAllAuthorElement(mod)
        {
            const title = mod.Contributors === undefined ? '' : Object.entries(mod.Contributors).filter(([_, names]) => names != null).sort(contributorSort).map(([role, names]) => `${role}: ${names.join(', ')}`).join('\n');
            const author = mod.Contributors === undefined ? mod.Author : mod.AllContr;
            return el('div', 'inf-author all-contributors inf', el('span', 'contributors', author), { title: title });
        }

        function addAuthorClick(element, mod)
        {
            element.addEventListener('click', event =>
            {
                const contactPopup = document.getElementById('contact-info');
                const list = contactPopup.querySelector("div > ul");
                list.innerHTML = '';

                let contributors = new Set();
                if (mod.Author)
                    for (let author of mod.Author.split(', '))
                        contributors.add(author);
                if (mod.Contributors)
                    for (let key of Object.keys(mod.Contributors).sort(contributorSort))
                        for (let contributor of mod.Contributors[key])
                            contributors.add(contributor);

                for (const author of contributors)
                {
                    const roles = mod.Contributors === undefined ? '' : ` (${Object.entries(mod.Contributors).filter(([_, names]) => names.includes(author)).sort(contributorSort).map(([role, _]) => role).join(", ")})`;
                    const item = el('li', null, `${author}:${roles}`);
                    list.appendChild(item);
                    const info = contactInfo[author];
                    const sublist = el('ul');
                    if (info !== undefined)
                    {
                        for (const [service, username] of Object.entries(info))
                        {
                            const contactItem = el('li');
                            const correctedService = Object.keys(services).find(value => value.toLowerCase() == service.toLowerCase());
                            if (services[correctedService] === undefined)
                                contactItem.textContent = `${service}: ${username}`;
                            else
                                contactItem.appendChild(el('a', null, correctedService, { href: "https://" + services[correctedService].replace("{}", username) }));

                            sublist.appendChild(contactItem);
                        }
                    }
                    else
                        sublist.appendChild(el('li', null, 'None available.'));
                    list.appendChild(sublist);
                }

                popup($(element), $(contactPopup));
                event.preventDefault();
                event.stopPropagation();
            });
        }

        function makeQuirksElement(mod)
        {
            let q = String(mod.Quirks);
            let quirks = [], quirksFull = [];
            if (q.includes("SolvesAtEnd")) { quirks.push("End"); quirksFull.push("Solves at the end"); }
            if (q.includes("NeedsOtherSolves")) { quirks.push("NdOt"); quirksFull.push("Needs other solves"); }
            if (q.includes("SolvesBeforeSome")) { quirks.push("Bef"); quirksFull.push("Solves before some"); }
            if (q.includes("SolvesWithOthers")) { quirks.push("Wit"); quirksFull.push("Solves with others"); }
            if (q.includes("WillSolveSuddenly")) { quirks.push("Sud"); quirksFull.push("Will solve suddenly"); }
            if (q.includes("PseudoNeedy")) { quirks.push("Psd"); quirksFull.push("Pseudo-needy"); }
            if (q.includes("TimeDependent")) { quirks.push("HvTm"); quirksFull.push("Time-dependent"); }
            if (q.includes("NeedsImmediateAttention")) { quirks.push("ImAt"); quirksFull.push("Needs immediate attention"); }
            if (q.includes("InstantDeath")) { quirks.push("IDth"); quirksFull.push("Instant death"); }
            return el('div', 'inf-quirks inf inf2', quirks.join(", "), { title: quirksFull.join(", ") });
        }

        function parseTpScore(score)
        {
            let scoreStr = [], m;
            for (let piece of score.split('+'))
            {
                if (piece === 'TBD')
                    scoreStr.push(translation['tpScoreTbd']);
                else if (m = /^\s*(\d*\.?\d+)\s*$/.exec(piece))
                    scoreStr.push(trN('tpScoreBase', m[1]));
                else if (m = /^\s*T\s*(\d*\.?\d+)\s*x?\s*$/.exec(piece))
                    scoreStr.push(trN('tpScoreTime', m[1]));
                else if (m = /^\s*D\s*(\d*\.?\d+)\s*x?\s*$/.exec(piece))
                    scoreStr.push(trN('tpScoreNeedy', m[1]));
                else if (m = /^\s*PPA\s*(\d*\.?\d+)\s*x?\s*$/.exec(piece))
                    scoreStr.push(trN('tpScoreAction', m[1]));
                else if (m = /^\s*(\d*\.?\d+)\s*x?\s*PPA\s*$/.exec(piece))
                    scoreStr.push(trN('tpScoreAction', m[1]));
                else if (m = /^\s*S\s*(\d*\.?\d+)\s*x?\s*$/.exec(piece))
                    scoreStr.push(trN('tpScoreSolve', m[1]));
            }
            return scoreStr.join(' + ');
        }

        switch (newView)
        {
            case 'List': {
                const mainTable = document.getElementById("main-table").getElementsByTagName("tbody")[0];

                for (let i = 0; i < modules.length; i++)
                {
                    let mod = modules[i];
                    mod.ViewData.List = { Created: false };

                    mod.FncsShowHide.push(sh =>
                    {
                        if (sh && !mod.ViewData.List.Created)
                        {
                            let tr = el("tr", `mod issues-${mod.Issues}${mod.TwitchPlays === null ? '' : ' tp'}${mod.RuleSeedSupport === 'Supported' ? ' rs' : ''}`);
                            mod.ViewData.List.TableRow = tr;
                            mod.FncsSetHighlight.push(hgh =>
                            {
                                if (hgh)
                                {
                                    tr.classList.add('selected');

                                    if (resultsMode == 'scroll')
                                        requestAnimationFrame(() => tr.scrollIntoView({ block: 'center' }));
                                }
                                else
                                    tr.classList.remove('selected');
                            });

                            for (let ix = 0; ix < initSelectables.length; ix++)
                            {
                                let sel = initSelectables[ix];
                                let td = el("td", `selectable${(ix == initSelectables.length - 1 ? " last" : "")}`);
                                tr.appendChild(td);
                                if (sel.ShowIconFunction(mod, mod.Manuals))
                                {
                                    let humanReadable = sel.HumanReadableFunction ? sel.HumanReadableFunction(mod, mod.Manuals) : sel.HumanReadable;
                                    let iconImg = el("img", "icon", { title: humanReadable, alt: humanReadable, src: sel.IconFunction(mod, mod.Manuals) });
                                    if (sel.PropName === 'video' && (mod.TutorialVideos && mod.TutorialVideos.length > 1))
                                    {
                                        let lnkDiv = el("div", "dropdown", iconImg);
                                        lnkDiv.addEventListener("click", makeTutorialPopupHandler(lnkDiv, mod.TutorialVideos));
                                        td.appendChild(lnkDiv);
                                    }
                                    else
                                    {
                                        let lnkA = el("a", sel.CssClass, { href: sel.UrlFunction(mod, mod.Manuals) }, iconImg);
                                        td.appendChild(lnkA);
                                        if (sel.PropName === 'manual')
                                            mod.FncsSetManualLink.push(url => { lnkA.href = url; });
                                    }
                                }
                            }

                            let icon = el("img", "mod-icon", { src: `Icons/${mod.X === 0 && mod.Y === 0 ? 'blank' : encodeURIComponent(mod.FileName ?? mod.Name)}.png` });
                            let modlink = el("a", "modlink", { href: mod.SelectableLinkUrl }, icon, el("span", "mod-name", mod.localName.replace(/'/g, "’")));
                            setIssuesTooltip(modlink, mod);
                            mod.ViewData.List.SelectableLink = modlink;
                            let td1 = el("td", "infos-1", el("div", "modlink-wrap", modlink, mod.localName === mod.Name ? null : el("div", "inf-origname", mod.Name.replace(/'/g, "’"))));
                            tr.appendChild(td1);
                            mod.FncsSetSelectable.push(url =>
                            {
                                if (url === null)
                                    modlink.removeAttribute('href');
                                else
                                    modlink.href = url;
                            });

                            let td2 = el("td", "infos-2");
                            tr.appendChild(td2);
                            let infos = el("div", "infos",
                                el("div", "inf-type inf", translation["moduleType" + mod.Type + "S"] || translation["moduleType" + mod.Type] || mod.Type),
                                el("div", "inf-origin inf inf2", translation["origin" + mod.Origin] || mod.Origin));
                            if (mod.Quirks)
                                infos.append(makeQuirksElement(mod));
                            if (mod.Type === 'Regular' || mod.Type === 'Needy' || mod.Type === 'Holdable')
                            {
                                function readable(difficulty)
                                {
                                    if (translation["moduleDiff" + difficulty]) return translation["moduleDiff" + difficulty];
                                    let result = '';
                                    for (let i = 0; i < difficulty.length; i++)
                                    {
                                        if (i > 0 && difficulty[i] >= 'A' && difficulty[i] <= 'Z')
                                            result += ' ';
                                        result += difficulty[i].toLowerCase();
                                    }
                                    return result;
                                }

                                if (mod.DefuserDifficulty === mod.ExpertDifficulty)
                                    infos.append(el("div", "inf-difficulty real-difficulty inf inf2", el("span", "inf-difficulty-sub", readable(mod.DefuserDifficulty))));
                                else
                                    infos.append(el("div", "inf-difficulty real-difficulty inf inf2", el("span", "inf-difficulty-sub", readable(mod.DefuserDifficulty)), ' (d), ', el("span", "inf-difficulty-sub", readable(mod.ExpertDifficulty)), ' (e)'));
                            }
                            infos.append(makeAuthorElement(mod), makeAllAuthorElement(mod),
                                el("div", "inf-published inf inf2", mod.Published));

                            if (mod.Symbol)
                                infos.append(el("div", "inf-symbol inf inf2", mod.Symbol));
                            if (mod.TwitchPlays)
                            {
                                mod.TwitchPlaysInfo = `${translation['tpScore'].replace('{0}', parseTpScore(mod.TwitchPlays.ScoreString))}`;
                                infos.append(el("div", "inf-twitch inf inf2", { title: mod.TwitchPlaysInfo }, mod.TwitchPlays.ScoreString));
                            }
                            if (mod.TimeMode)
                            {
                                let origin = mod.TimeMode.Origin ? translation[`timeModeOrigin${mod.TimeMode.Origin || 'Unassigned'}`] : '';
                                mod.TimeModeInfo = translation[mod.TimeMode.ScorePerModule ? mod.TimeMode.Score ? 'timeModeScoreBoth' : 'timeModeScorePerModule' : 'timeModeScoreNormal']
                                    .replace(/\{(\d+)\}/g, (_, m) => [origin, mod.TimeMode.Score, mod.TimeMode.ScorePerModule][+m]);
                                infos.append(el("div", `inf-time-mode inf-time-mode-${mod.TimeMode.Origin || 'Unassigned'} inf inf2`, { title: mod.TimeModeInfo },
                                    mod.TimeMode.ScorePerModule ? 'S' : mod.TimeMode.Score));
                            }
                            if (mod.RuleSeedSupport === 'Supported')
                            {
                                mod.RuleSeedInfo = translation['ruleSeedSupported'];
                                infos.append(el("div", "inf-rule-seed inf inf2", { title: mod.RuleSeedInfo }));
                            }
                            if (mod.Type === 'Regular')
                            {
                                let value = !('Souvenir' in mod) || mod.Souvenir === null || !('Status' in mod.Souvenir) ? 'Unexamined' : mod.Souvenir.Status;
                                let attr = souvenirAttributes[value];
                                let expl = mod.Souvenir && mod.Souvenir.Explanation;
                                mod.SouvenirInfo = `${translation[attr.Tooltip]}${expl ? "\n" + expl : ""}`;
                                infos.append(el("div", `inf-souvenir inf inf2${expl ? " souvenir-explanation" : ""}`, { title: mod.SouvenirInfo }, attr.Char));
                            }

                            if (mod.ModuleID)
                                infos.append(el("div", "inf-id inf", mod.ModuleID));

                            let modDescr = mod.Descriptions.filter(d => d.Language == (pageLang ?? "English"))[0] ?? mod.Descriptions.filter(d => d.Language == "English")[0];

                            let descrip = el("div", "inf-description inf");
                            descrip.appendChild(el("span", "inf-description-only inf", modDescr.Description ?? ""));
                            if (modDescr.Description && modDescr.Tags)
                                descrip.appendChild(document.createTextNode(" "));
                            descrip.appendChild(el("span", "inf-tags inf", modDescr.Tags ?? ""));
                            td2.appendChild(infos.cloneNode(true));
                            infos.append(descrip);
                            td1.appendChild(infos);

                            addAuthorClick(td1.querySelector(".inf-author"), mod);
                            addAuthorClick(td2.querySelector(".inf-author"), mod);
                            addAuthorClick(td1.querySelector(".inf-author.all-contributors"), mod);
                            addAuthorClick(td2.querySelector(".inf-author.all-contributors"), mod);

                            let lnk1 = el("a", "manual-selector", { href: "#" });
                            lnk1.onclick = makeClickHander(lnk1, false, mod, tr);
                            td1.appendChild(lnk1);

                            let lnk2 = el("a", "mobile-opt", { href: "#" });
                            lnk2.onclick = makeClickHander(lnk2, true, mod, tr);
                            tr.appendChild(el("td", "mobile-ui", lnk2));

                            setElemCbsRestricted(tr, mod.CbsRestrictedIsPreferred === true);

                            mod.ViewData.List.Created = true;

                            // We need to insert the new row while maintaining sort order
                            // Find the prior mod that has been created
                            let priorMod = null;
                            for (let j = modules.indexOf(mod) - 1; j >= 0; j--)
                            {
                                if (modules[j].ViewData.List.Created)
                                {
                                    priorMod = modules[j];
                                    break;
                                }
                            }

                            if (priorMod !== null)
                                // Insert it after that prior mod
                                priorMod.ViewData.List.TableRow.after(tr);
                            else
                                // No prior mod, so it should be the first row after the header
                                mainTable.querySelector("tr.header-row").after(tr);
                        }

                        if (mod.ViewData.List.Created)
                            mod.ViewData.List.TableRow.style.display = (sh ? '' : 'none');
                    });
                }

                viewsReady.set('List', {
                    Show: function() { document.getElementById("main-table").style.display = 'table'; },
                    Hide: function() { document.getElementById("main-table").style.display = 'none'; },
                    Sort: function() { mainTable.append(...modules.filter(mod => mod.ViewData.List.Created).map(mod => mod.ViewData.List.TableRow)); }
                });
                break;
            }

            case 'PeriodicTable': {
                const souvenirStatuses = {
                    Unexamined: 'U',
                    NotACandidate: 'N',
                    Considered: 'C',
                    Planned: 'P',
                    Supported: 'S'
                };

                let layoutProcessing = [];
                function processLayout()
                {
                    let layoutFixing = [];
                    for (let symText of layoutProcessing)
                    {
                        let w = symText.offsetWidth;
                        if (w > 47)
                            layoutFixing.push({ symText: symText, transform: `scaleX(${47 / w})` });
                    }
                    for (let inf of layoutFixing)
                        inf.symText.style.transform = inf.transform;
                    layoutProcessing = [];
                }

                for (let i = 0; i < modules.length; i++)
                {
                    let mod = modules[i];
                    let manualSelector = el('a', 'manual-selector', { href: '#' });
                    let img, a = el('a', `module ${mod.ExpertDifficulty} issues-${mod.Issues}`,
                        el('div', `symbol ${mod.DefuserDifficulty}`, el('span', 'inner', mod.Symbol || '??')),
                        el('div', 'mod-icon', img = el("img", "mod-icon", { src: `Icons/blank.png` })),
                        el('div', 'name', el('div', 'inner', mod.localName)),
                        el('div', 'tpscore', mod.TwitchPlays?.ScoreString ?? '', { title: mod.TwitchPlays ? parseTpScore(mod.TwitchPlays.ScoreString) : null }),
                        el('div', 'souvenir', souvenirStatuses[(mod.Souvenir && mod.Souvenir.Status) || 'Unexamined']),
                        manualSelector);
                    setIssuesTooltip(a, mod);

                    manualSelector.onclick = makeClickHander(manualSelector, false, mod, a);

                    document.getElementById("actual-periodic-table").appendChild(a);

                    mod.ViewData.PeriodicTable = { SelectableLink: a };
                    mod.FncsShowHide.push(sh =>
                    {
                        a.style.display = sh ? 'block' : 'none';
                        if (sh)
                        {
                            let symText = a.querySelector('.symbol>.inner');
                            symText.style.transform = '';
                            img.setAttribute('src', `Icons/${mod.X === 0 && mod.Y === 0 ? 'blank' : encodeURIComponent(mod.FileName ?? mod.Name)}.png`);
                            // Prevent layout thrashing
                            layoutProcessing.push(symText);
                            requestAnimationFrame(processLayout);
                        }
                    });
                    mod.FncsSetSelectable.push(url => { a.href = url; });
                    mod.FncsSetHighlight.push(hgh =>
                    {
                        if (hgh)
                            a.classList.add('highlight');
                        else
                            a.classList.remove('highlight');
                    });
                    setElemCbsRestricted(a, mod.CbsRestrictedIsPreferred === true);
                }

                viewsReady.set('PeriodicTable', {
                    Show: function() { document.getElementById("main-periodic-table").style.display = 'block'; },
                    Hide: function() { document.getElementById("main-periodic-table").style.display = 'none'; },
                    Sort: function() { document.getElementById("actual-periodic-table").append(...modules.map(mod => mod.ViewData.PeriodicTable.SelectableLink)); }
                });
                break;
            }

            default:
                return false;
        }

        setLinksAndPreferredManuals();
        setDisplayOptions(displayOptions);
        setSearchOptions(searchOptions);
        updateFilter();
        return true;
    }

    function setView(newView)
    {
        if (createView(newView))
        {
            setCbsRestricted();
            for (let [k, v] of viewsReady)
            {
                if (k === newView)
                    v.Show();
                else
                    v.Hide();
            }
            view = newView;
            lStorage.setItem('view', newView);
            setSort(sort, reverse);
            setLinksAndPreferredManuals();
        }
    }

    function handleDataTransfer(dataTransfer)
    {
        let url;
        if (dataTransfer.files && dataTransfer.files.length == 1)
        {
            setProfile(dataTransfer.files[0]);
            return true;
        }
        else if (dataTransfer.getData && (url = dataTransfer.getData("text/plain")).endsWith(".json"))
        {
            const handleData = data =>
            {
                const fileName = url.match(/\/(\w+\.json)$/);
                setProfile(new File([data], fileName ? fileName[1] : "Default.json"));
            };
            $.get("/proxy/" + url, handleData).fail(function() { $.get(url, handleData); });
            return true;
        }
        return false;
    }

    // Source: https://stackoverflow.com/a/37511463
    function removeDiacritics(str)
    {
        return str.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
    }

    function updateFilter(showAll)
    {
        let noneSelected = {};
        for (let i = 0; i < initFilters.length; i++)
        {
            let none = true;
            switch (initFilters[i].type)
            {
                case "slider":
                    filter[initFilters[i].id] = {
                        min: $('div#filter-' + initFilters[i].id).slider('values', 0),
                        max: $('div#filter-' + initFilters[i].id).slider('values', 1)
                    };
                    let x = function(str) { return translation["moduleDiff" + str] || str.replace(/[A-Z][a-z]*/g, function(m) { return " " + m.toLowerCase(); }).trim(); };
                    let y = function(s1, s2) { return s1 === s2 ? x(s1) : x(s1) + ' – ' + x(s2); };
                    $('div#filter-label-' + initFilters[i].id).text(y(initFilters[i].values[filter[initFilters[i].id].min], initFilters[i].values[filter[initFilters[i].id].max]));
                    none = false;
                    break;

                case "checkboxes":
                    filter[initFilters[i].id] = {};
                    for (let j = 0; j < initFilters[i].values.length; j++)
                    {
                        filter[initFilters[i].id][initFilters[i].values[j]] = $('input#filter-' + initFilters[i].id + '-' + initFilters[i].values[j]).prop('checked');
                        if (filter[initFilters[i].id][initFilters[i].values[j]])
                            none = false;
                    }
                    break;
                case "flags":
                    filter[initFilters[i].id] = {};
                    for (let j = 0; j < initFilters[i].values.length; j++)
                    {
                        if ($('input#filter-' + initFilters[i].id + '-' + initFilters[i].values[j] + '-y').prop('checked'))
                            filter[initFilters[i].id][initFilters[i].values[j]] = 'y';
                        else if ($('input#filter-' + initFilters[i].id + '-' + initFilters[i].values[j] + '-n').prop('checked'))
                            filter[initFilters[i].id][initFilters[i].values[j]] = 'n';
                        else
                            filter[initFilters[i].id][initFilters[i].values[j]] = 'e';
                    }
                    none = false;
                    break;
            }
            noneSelected[initFilters[i].id] = none;
        }

        function escapeRegExp(string)
        {
            return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        }

        let searchRaw = $("input#search-field").val().toString().toLowerCase().trim();
        let searchKeywords = searchRaw.split(' ').filter(x => x.length > 0).map(x => x.replace(/’/g, '\'')).map(x => new RegExp(escapeRegExp(x).replace(/colou?/g, 'colou?').replace(/gr[ae]y/g, 'gr[ae]y').replace(/impost[oe]r/g, 'impost[oe]r')));
        const filterEnabledByProfile = $('input#filter-profile-enabled').prop('checked');
        const filterVetoedByProfile = $('input#filter-profile-disabled').prop('checked');

        let modCount = 0;
        let showAtTop = [];
        let searchBySymbol = document.getElementById('option-include-symbol').checked;
        let searchBySteamID = document.getElementById('option-include-steam-id').checked;
        let searchByModuleID = document.getElementById('option-include-module-id').checked;
        let displayAllContributors = document.getElementById('display-all-contributors').checked;
        let displayDescription = document.getElementById('display-description').checked;
        let displayTags = document.getElementById('display-tags').checked;
        modules.forEach(function(mod)
        {
            let filteredIn = missionList === null || missionList.has(mod.ModuleID);
            for (let i = 0; i < initFilters.length; i++)
            {
                if (typeof initFilters[i].fnc(mod) !== 'undefined')
                {
                    switch (initFilters[i].type)
                    {
                        case "slider":
                            filteredIn = filteredIn && initFilters[i].values.indexOf(initFilters[i].fnc(mod)) >= filter[initFilters[i].id].min && initFilters[i].values.indexOf(initFilters[i].fnc(mod)) <= filter[initFilters[i].id].max;
                            break;
                        case "checkboxes":
                            filteredIn = filteredIn && (filter[initFilters[i].id][initFilters[i].fnc(mod)] || noneSelected[initFilters[i].id]);
                            break;
                        case "flags":
                            let allMatches = initFilters[i].fnc(mod);
                            let match = true;
                            for (let key in filter[initFilters[i].id])
                            {
                                if ((filter[initFilters[i].id][key] == 'y' && !allMatches.includes(key)) ||
                                    (filter[initFilters[i].id][key] == 'n' && allMatches.includes(key)))
                                {
                                    match = false;
                                    break;
                                }
                            }
                            filteredIn = filteredIn && match;
                            break;
                    }
                }
            }

            if (mod.Manuals.length > 0)
                filteredIn = filteredIn && mod.Manuals.some(manual => preferredLanguages[manual.Language] !== false);

            if (profileVetoList !== null && mod.Origin !== 'Vanilla')
                filteredIn = filteredIn && (profileVetoList.includes(mod.ModuleID) ? (filterVetoedByProfile || !filterEnabledByProfile) : (filterEnabledByProfile || !filterVetoedByProfile));

            let searchWhat = searchBySteamID ? (mod.SteamID || '') : '';
            if (searchByModuleID && 'ModuleID' in mod && mod.ModuleID !== null)
                searchWhat += ' ' + mod.ModuleID.toLowerCase();
            if (searchOptions.indexOf('names') !== -1)
                searchWhat += ' ' + mod.Name.toLowerCase() + ' ' + mod.SortKey.toLocaleLowerCase();
            if (searchOptions.indexOf('authors') !== -1)
                if (displayAllContributors)
                    searchWhat += ' ' + mod.AllContr.toLowerCase();
                else
                    searchWhat += ' ' + mod.Author.toLowerCase();
            if (searchOptions.indexOf('descriptions') !== -1)
            {
                let modDescr = mod.Descriptions.filter(d => d.Language == (pageLang ?? "English"))[0] ?? mod.Descriptions.filter(d => d.Language == "English")[0];
                if (displayDescription)
                    searchWhat += ' ' + modDescr.Description.toLowerCase();
                if (displayTags && modDescr.Tags)
                    searchWhat += ' ' + modDescr.Tags.toLowerCase();
            }
            if (searchBySymbol && mod.Symbol)
                searchWhat += ' ' + mod.Symbol.toLowerCase();
            if (pageLang)
                searchWhat += ' ' + mod.localName.toLocaleLowerCase();

            mod.MatchesFilter = filteredIn;
            mod.MatchesSearch = searchKeywords.every(x => x.test(searchWhat));
            mod.includeInResults = mod.MatchesFilter && mod.MatchesSearch;

            if (invertSearch)
                mod.includeInResults = !mod.includeInResults;

            if ((resultsMode === 'scroll') ? mod.MatchesFilter : mod.includeInResults)
                modCount++;

            if (searchRaw.toLocaleLowerCase().replace(/\s/g, '') === mod.Name.toLocaleLowerCase().replace(/\s/g, ''))
                showAtTop.unshift(mod);
            else if (searchBySymbol && mod.Symbol && searchRaw === mod.Symbol.toLowerCase())
                showAtTop.push(mod);
        });

        let moduleCount = document.getElementById('module-count');
        moduleCount.innerText = (resultsMode === 'scroll' || resultsLimit >= modCount || showAll) ? translation["bottomLineAll"] : translation["bottomLineSome"];
        moduleCount.innerHTML = moduleCount.innerHTML.replace(/\{(\d+)\}/g, (_, n) =>
            n == 0 ? trN("bottomLineItems", modCount) :
                n == 1 ? trN('bottomLineShowingFirst', resultsLimit) :
                    n == 2 ? `<a href='#'></a>` : '?');
        if (searchRaw.length > 0)
        {
            let unshown = modules.filter(m => m.MatchesSearch && !m.MatchesFilter).length;
            if (unshown > 0)
                moduleCount.innerHTML += ` — ${trN('bottomLineFilteredOut', unshown)}`;
        }
        let moduleCountShowAllLink = moduleCount.querySelector('a');
        if (moduleCountShowAllLink)
        {
            moduleCountShowAllLink.innerText = translation["bottomLineShowAllLink"];
            moduleCountShowAllLink.onclick = function()
            {
                updateFilter(true);
                return false;
            };
        }

        lStorage.setItem('filters', JSON.stringify(filter));
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();

        if (showAtTop.length !== showAtTopOfResults.length || !showAtTop.every(mod => showAtTopOfResults.includes(mod)))
        {
            showAtTopOfResults = showAtTop;
            setSort(sort, reverse);
        }

        let n = 0;
        for (let mod of modules)
        {
            for (let fnc of mod.FncsShowHide)
                fnc(mod.includeInResults && (resultsMode === 'scroll' || ((n < resultsLimit || showAll))));
            if (mod.IsVisible)
                n++;
        }
        if (resultsMode === 'scroll')
            selectedIndex = modules.filter(m => m.IsVisible).findIndex(m => m.MatchesSearch);
    }

    // Find the default manual based on the user’s language
    function getDefaultManual(mod)
    {
        let manual = mod.Manuals[0];
        manual = mod.Manuals.filter(m => m.Name === mod.Name + " (HTML)")[0] ?? manual;
        manual = mod.Manuals.filter(m => m.Language === pageLang && m.Name.slice(-6) === "(HTML)")[0] ?? manual;
        return manual;
    }

    function removeHtmlPdfSuffix(str)
    {
        if (str.endsWith(" (HTML)"))
            str = str.slice(0, str.length - 7);
        if (str.endsWith(" (PDF)"))
            str = str.slice(0, str.length - 6);
        return str;
    }
    function setCbsRestricted()
    {
        for (let mod of modules) {
            if (mod.Manuals.length > 1) {
                if (mod.Name == "Terminology")
                    console.log(mod);
                let manual = getDefaultManual(mod);

                // if preferred is not the default
                if (mod.Name in preferredManuals && preferredManuals[mod.Name] !== manual.Name.substr(mod.Name.length + 1)) {
                    let prefManual = mod.Manuals.filter(m => m.Name.substr(mod.Name.length + 1) === preferredManuals[mod.Name])[0];
                    // record if preferred is restricted and restricted manuals are currently hidden
                    if (prefManual && displayRestrictedManuals && restrictedManualNames.some(m => removeHtmlPdfSuffix(prefManual.Name) === m))
                        mod.CbsRestrictedIsPreferred = true;
                }
            }
        }
    }

    // Sets the module links to the current selectable and the manual icon link to the preferred manuals
    function setLinksAndPreferredManuals()
    {
        let seed = +$('#rule-seed-input').val();
        let seedHash = (seed === 1 ? '' : '#' + seed);
        for (let mod of modules)
        {
            let manual = null;
            if (mod.Manuals.length > 0)
            {
                manual = getDefaultManual(mod);

                if (mod.Name in preferredManuals)
                {
                    // Remove it from preferredManuals if it’s the default anyway
                    if (preferredManuals[mod.Name] === manual.Name.substr(mod.Name.length + 1))
                        delete preferredManuals[mod.Name];
                    else
                    {
                        // Remove preferred manuals that no longer exist
                        let prefManual = mod.Manuals.filter(m => m.Name.substr(mod.Name.length + 1) === preferredManuals[mod.Name])[0];
                        if (!prefManual)
                            delete preferredManuals[mod.Name];
                        // set preferred if preferred is not restricted and restricted manuals are currently displayed
                        else if (displayRestrictedManuals || !restrictedManualNames.some(m => removeHtmlPdfSuffix(prefManual.Name) === m))
                            manual = prefManual;
                        // record if preferred is restricted and restricted manuals are currently hidden
                        else
                            mod.CbsRestrictedIsPreferred = true;
                    }
                }

                for (let fnc of mod.FncsSetManualLink)
                    fnc(manual === null ? null : manual.Url + seedHash);
            }
            for (let fnc of mod.FncsSetSelectable)
                fnc(selectable === 'manual' ? (manual === null ? null : manual.Url + seedHash) : (initSelectables.filter(sl => sl.PropName === selectable).map(sl => sl.UrlFunction(mod))[0] || null));
        }
        lStorage.setItem('preferredManuals', JSON.stringify(preferredManuals));
    }

    let preventDisappear = 0;
    function disappear()
    {
        if (preventDisappear === 0)
        {
            $('.disappear.stay').hide();
            $('.disappear:not(.stay)').remove();

            if ($('#page-opt-popup>#icons').length)
                $('#icons').insertAfter('#logo');
        }
        else
            preventDisappear--;
    }

    document.addEventListener('click', disappear);
    document.addEventListener('dragover', () => false);
    document.addEventListener('drop', event =>
    {
        event.preventDefault();
        event.stopPropagation();
        handleDataTransfer(event.dataTransfer);
    });
    document.addEventListener('paste', event =>
    {
        if (handleDataTransfer(event.clipboardData))
            event.preventDefault();
    });

    let manualsLastUpdated = {};

    // Click handler for selecting manuals/cheat sheets (both mobile and non)
    function makeClickHander(lnk, isMobileOpt, mod, modGroup)
    {
        return function(event)
        {
            let numAlready = Array.from(document.getElementsByClassName('popup')).filter(p => p['data-lnk'] === lnk).length;
            disappear();
            if (numAlready)
                return false;
            let menuDiv = el('div', 'popup disappear manual-select', { 'style': 'display: block', onclick: function() { preventDisappear++; } });
            menuDiv['data-lnk'] = lnk;
            document.body.appendChild(menuDiv);
            if (isMobileOpt)
            {
                let closeButton = el('div', 'close', { onclick: disappear });
                menuDiv.appendChild(closeButton);
                let iconsDiv = el('div', 'icons');

                for (let ix = 0; ix < initSelectables.length; ix++)
                {
                    let sel = initSelectables[ix];
                    if (['manual', 'video'].includes(sel.PropName) || !sel.ShowIconFunction(mod, mod.Manuals))
                        continue;
                    let iconDiv = el('div', 'icon',
                        el('a', 'icon-link', { href: sel.UrlFunction(mod, mod.Manuals) },
                            el('img', 'icon-img', { src: sel.IconFunction(mod, mod.Manuals) }),
                            el('span', 'icon-label', sel.HumanReadableFunction ? sel.HumanReadableFunction(mod, mod.Manuals) : sel.HumanReadable)));
                    iconsDiv.appendChild(iconDiv);
                }

                menuDiv.appendChild(iconsDiv);
                if ($('#display-souvenir').prop('checked'))
                    menuDiv.appendChild(el('div', 'module-further-info', mod.SouvenirInfo ?? translation['souvenirNotACandidate']));
                if ($('#display-twitch').prop('checked') && 'TwitchPlaysInfo' in mod)
                    menuDiv.appendChild(el('div', 'module-further-info', mod.TwitchPlaysInfo));
                if ($('#display-time-mode').prop('checked') && 'TimeModeInfo' in mod)
                    menuDiv.appendChild(el('div', 'module-further-info', mod.TimeModeInfo));
                if ($('#display-rule-seed').prop('checked') && 'RuleSeedInfo' in mod)
                    menuDiv.appendChild(el('div', 'module-further-info', mod.RuleSeedInfo));
                let title = getIssuesText(mod);
                if (title !== undefined)
                    menuDiv.appendChild(el('div', 'module-further-info', title));
            }
            let lastupdatedEnabled = false;
            try { lastupdatedEnabled = (JSON.parse(lStorage.getItem('display')) || []).includes('last-updated'); } catch (exc) { }
            menuDiv.appendChild(el('p', 'small-print', translation['selectPreferredManual'], lastupdatedEnabled ? el('span', '', translation['lastUpdated'], { style: 'float:right;' }) : null));
            let menu = el('div', 'manual-select');
            menuDiv.appendChild(menu);
            let seed = +document.getElementById('rule-seed-input').value || 0;
            let seedHash = (seed === 1 ? '' : '#' + seed);
            let already = new Map();
            for (let i = 0; i < mod.Manuals.length; i++)
            {
                let rx1 = /^\s*(.*) \((HTML|PDF)\)$/.exec(mod.Manuals[i].Name.substr(mod.Name.length));
                let restricted = restrictedManualNames.some(m => removeHtmlPdfSuffix(mod.Manuals[i].Name) === m);
                let clickHandler = function(sh)
                {
                    return function(e)
                    {
                        menuDiv.remove();
                        preferredManuals[mod.Name] = sh;
                        setLinksAndPreferredManuals();
                        setElemCbsRestricted(modGroup, restricted);
                        e.stopPropagation();
                        return false;
                    };
                }(mod.Manuals[i].Name.substr(mod.Name.length + 1));
                let link = el('a', 'link', { href: mod.Manuals[i].Url + seedHash, onclick: clickHandler }, rx1[2]);
                if (!already.has(rx1[1]))
                {
                    let trow, rx2;
                    if (rx2 = /^translated(?: full)? \((.*) — (.*)\) (.*) \((.*)\)$/.exec(rx1[1]))
                        trow = [rx2[1], rx2[2], [el('div', 'descriptor', rx2[3]), el('div', 'author', rx2[4])]];
                    else if (rx2 = /^translated(?: full)? \((.*) — (.*)\) \((.*)\)$/.exec(rx1[1]))
                        trow = [rx2[1], rx2[2], [el('div', 'author', rx2[3])]];
                    else if (rx2 = /^translated(?: full)? \((.*) — (.*)\) (.*)$/.exec(rx1[1]))
                        trow = [rx2[1], rx2[2], [el('div', 'descriptor', rx2[3])]];
                    else if (rx2 = /^translated(?: full)? \((.*) — (.*)\)$/.exec(rx1[1]))
                        trow = [rx2[1], rx2[2], []];
                    else if (rx2 = /^translated(?: full)? \((.*)\)$/.exec(rx1[1]))
                        trow = [rx2[1], mod.Name, []];
                    else if (rx2 = /^(.*) \((.*)\)$/.exec(rx1[1]))
                        trow = [null, mod.Name, [el('div', 'descriptor', rx2[1]), el('div', 'author', rx2[2])]];
                    else
                        trow = [null, mod.Name, [el('div', 'descriptor', rx1[1])]];

                    const language = trow[0] || "English";

                    let languageAllowed = preferredLanguages[language] === false;
                    if (invertSearch)
                        languageAllowed = !languageAllowed;

                    if (languageAllowed || !displayRestrictedManuals && restricted)
                        continue;

                    let code = Object.keys(Ktane.Languages).filter(langCode => Ktane.Languages[langCode] === trow[0])[0];
                    if (code !== undefined)
                        trow[0] += ` (${code})`;

                    let trowElem = el('div', null,
                        el('div', 'mobile-cell',
                            el('div', 'language', trow[0]),
                            el('div', 'title', trow[1]),
                            el('div', `alt-indicator${restricted ? ' cbs-restricted' : ''}`, {title: restricted ? 'CBS Restricted' : '' }),
                            el('div', 'extra', ...trow[2])),
                        el('div', 'link link-HTML'),
                        el('div', 'link link-PDF'));

                    if (lastupdatedEnabled && mod.Manuals[i].Url.startsWith('HTML/'))
                    {
                        let lastupdatedElem = el('div', 'last-updated');
                        let manualName = mod.Manuals[i].Url.substr(5);
                        if (manualName in manualsLastUpdated)
                            lastupdatedElem.innerText = manualsLastUpdated[manualName];
                        else
                        {
                            lastupdatedElem.innerText = 'Loading...';
                            $.ajax({
                                method: 'GET',
                                url: "ManualLastUpdated/" + manualName,
                                cache: false,
                                success: function(data)
                                {
                                    let lastupdatedDate = new Date(data);
                                    let year = lastupdatedDate.getFullYear();
                                    let month = lastupdatedDate.getMonth() + 1;
                                    let day = lastupdatedDate.getDate();
                                    let formattedDate = year + "-" + (month > 9 ? month : `0${month}`) + "-" + (day > 9 ? day : `0${day}`);
                                    manualsLastUpdated[manualName] = formattedDate;
                                    lastupdatedElem.innerText = formattedDate;
                                    $(menuDiv).position({ my: 'right top', at: 'right bottom', of: lnk, collision: 'fit none' });
                                },
                                error: function(data)
                                {
                                    lastupdatedElem.innerText = "unknown";
                                    $(menuDiv).position({ my: 'right top', at: 'right bottom', of: lnk, collision: 'fit none' });
                                }
                            });
                        }
                        trowElem.appendChild(lastupdatedElem);
                    }
                    menu.appendChild(trowElem);

                    already.set(rx1[1], {
                        Html: trowElem.getElementsByClassName('link-HTML')[0],
                        Pdf: trowElem.getElementsByClassName('link-PDF')[0],
                        Row: trowElem
                    });
                }
                let inf = already.get(rx1[1]);
                if (rx1[2] === 'HTML')
                    inf.Row.onclick = clickHandler;
                let elem = rx1[2] === 'HTML' ? inf.Html : inf.Pdf;
                elem.appendChild(link);

                //if this manual is the preferred manual or this manual is the default and there is no preferred manual
                if ((mod.Name in preferredManuals && `${mod.Name} ${preferredManuals[mod.Name]}` === mod.Manuals[i].Name) ||
                    (!(mod.Name in preferredManuals) && mod.Manuals[i].Name === getDefaultManual(mod).Name))
                    elem.classList.add('checked');
            }

            if (isMobileOpt && mod.TutorialVideos)
            {
                let vidDiv = el('div', 'tutorial-videos');
                menuDiv.appendChild(vidDiv);
                setTutorialList(vidDiv, mod.TutorialVideos);
            }

            menuDiv.appendChild(el('div', 'bottom-links',
                el('div', null, el('a', null, { href: `find-log?find=${encodeURIComponent(mod.ModuleID)}` }, translation['findExampleLogfile'])),
                el('div', null, el('a', null, { href: '#', onclick: function() { setEditUi(mod); popup($(lnk), $('#module-ui')); return false; } }, translation[mod.Type === 'Widget' ? 'editWidget' : mod.Type === 'Holdable' ? 'editHoldable' : 'editModule']))));

            if (!isMobileOpt)
                $(menuDiv).position({ my: 'right top', at: 'right bottom', of: lnk, collision: 'fit none' });
            event.stopPropagation();
            return false;
        };
    }

    function makeTutorialPopupHandler(lnk, urls)
    {
        return event =>
        {
            const numAlready = Array.from(document.getElementsByClassName('popup')).filter(p => p['data-lnk'] === lnk).length;
            disappear();
            if (numAlready)
                return false;
            const menuDiv = el('div', 'popup disappear tutorial-select', { 'style': 'display: block', onclick: function() { preventDisappear++; } });
            menuDiv['data-lnk'] = lnk;
            document.body.appendChild(menuDiv);
            setTutorialList(menuDiv, urls);
            $(menuDiv).position({ my: 'left top', at: 'left bottom', of: lnk, collision: 'fit none' });
            event.stopPropagation();
            return false;
        };
    }

    function setTutorialList(menuDiv, urls)
    {
        menuDiv.appendChild(el('h5', null, translation.selectableTutorial));
        const tutorialMenu = el('div', 'tutorial-select');
        const tutorialOrder = urls.slice();
        tutorialOrder.sort((t1, t2) =>
        {
            if (t1.length > 0 && t1.length > 0)
                return t1[0].localeCompare(t2[0]);
            else if (t1.length > 0)
                return 1;
            else if (t2.length > 0)
                return -1;
            else
                return 0;
        });
        tutorialOrder.forEach(function(tutorialItem)
        {
            const tutorialLang = "Language" in tutorialItem ? tutorialItem.Language : "";
            const tutorialName = "Description" in tutorialItem ? tutorialItem.Description : "";
            const tutorialUrl = "Url" in tutorialItem ? tutorialItem.Url : "";

            tutorialMenu.appendChild(
                el('a', null,
                    { href: tutorialUrl },
                    el('div', null, tutorialLang),
                    el('div', null, tutorialName),
                    el('div', null, el('img', 'icon', { title: "Tutorial video", alt: "Tutorial video", src: "HTML/img/video.png" }))));
        });
        menuDiv.appendChild(tutorialMenu);
    }

    let languages = [];
    function getLanguageFromSheet(sheet)
    {
        const matches = sheet.match(/^ translated \((?:(.+) — .+|(.+))?\)/);
        return matches !== null ? (matches[2] || matches[1]) : "English";
    }

    // ** PROCESS ALL THE MODULES ** //
    for (let i = 0; i < modules.length; i++)
    {
        let mod = modules[i];
        mod.ManualIconUrl = null;
        mod.SelectableLinkUrl = null;
        mod.IsVisible = true;
        mod.MatchesFilter = false;

        if (mod.Contributors)
        {
            let namesSet = new Set();
            for (let key of Object.keys(mod.Contributors).sort(contributorSort))
                for (let contributor of mod.Contributors[key])
                    namesSet.add(contributor);
            mod.AllContr = Array.from(namesSet).join(', ');
        }
        else
            mod.AllContr = mod.Author;

        // Make sure that this code matches KtanePropellerModule.GetDefaultSortKey exactly
        if (mod.SortKey === undefined)
            mod.SortKey = mod.Name.replace(/^The |[^-a-zA-Z0-9 ]/gi, '').toUpperCase();

        // split Description into Tags and Description by delimiter "Tags:"
        if (mod.Description)
        {
            let descSplit = mod.Description.split(" Tags: ");
            mod.Descriptions = [{ Language: "English", Description: descSplit[0], Tags: descSplit[1] ?? '' }];
        }

        // (bool sh) => shows (sh) or hides (!sh) the module
        mod.FncsShowHide = [sh => { mod.IsVisible = sh; }];

        // (string url) => changes what the manual icon links to (preferred manual)
        mod.FncsSetManualLink = [url => { mod.ManualIconUrl = url; }];

        // (string url) => changes what the module’s main link links to (selectable)
        mod.FncsSetSelectable = [url => { mod.SelectableLinkUrl = url; }];

        // (bool hgh) => sets the highlight on or off
        mod.FncsSetHighlight = [];

        mod.ViewData = {};

        // Construct the list of manuals. (The list provided in .Sheets is kinda compressed.)
        mod.Manuals = mod.Sheets.map(str => str.split('|')).map(arr => ({
            Name: `${mod.Name}${arr[0]} (${arr[1].toUpperCase()})`,
            Url: `${initDocDirs[(parseInt(arr[2]) / 2) | 0]}/${encodeURIComponent(mod.FileName || mod.Name)}${encodeURIComponent(arr[0])}.${arr[1]}`,
            Icon: initIcons[arr[2]],
            Language: getLanguageFromSheet(arr[0])
        }));
        for (let manualIx = 0; manualIx < mod.Manuals.length; manualIx++)
            if (!languages.includes(mod.Manuals[manualIx].Language))
                languages.push(mod.Manuals[manualIx].Language);

        // Default values
        if (!mod.RuleSeedSupport)
            mod.RuleSeedSupport = 'NotSupported';
        if (!mod.Issues)
            mod.Issues = 'None';
        if (mod.TwitchPlays && !mod.TwitchPlays.Score)
            mod.TwitchPlays.Score = 0;
        if (mod.TwitchPlays && !mod.TwitchPlays.ScorePerModule)
            mod.TwitchPlays.ScorePerModule = 0;
        if (mod.TwitchPlays && !mod.TwitchPlays.ScorePerModuleCap)
            mod.TwitchPlays.ScorePerModuleCap = 0;
        if (mod.TwitchPlays && !mod.TwitchPlays.NeedyScoring)
            mod.TwitchPlays.NeedyScoring = 'Solves';
        if (mod.TwitchPlays && !mod.TwitchPlays.TagPosition)
            mod.TwitchPlays.TagPosition = 'Automatic';
        if (mod.TwitchPlays && !mod.TwitchPlays.AutoPin)
            mod.TwitchPlays.AutoPin = false;

        // Potentially translated module names if user has specified ?lang=xx in the URL
        mod.localName = mod.Name;
        if (pageLang !== null && mod.Manuals)
            for (let j = 0; j < mod.Manuals.length; j++)
            {
                let rx2 = mod.Manuals[j].Name.match(/translated(?: full)? \((.*) — ([^)]+)\)/);
                if (rx2 && rx2[1] === pageLang)
                {
                    mod.localName = rx2[2];
                    break;
                }
            }
    }
    languages.sort();

    // Set filters from saved settings
    for (let i = 0; i < initFilters.length; i++)
    {
        switch (initFilters[i].type)
        {
            case "slider":
                if (!(initFilters[i].id in filter) || typeof filter[initFilters[i].id] !== 'object')
                    filter[initFilters[i].id] = {};

                if (!('min' in filter[initFilters[i].id]))
                    filter[initFilters[i].id].min = 0;
                if (!('max' in filter[initFilters[i].id]))
                    filter[initFilters[i].id].max = initFilters[i].values.length - 1;
                let e = $('div#filter-' + initFilters[i].id);
                e.slider({
                    range: true,
                    min: 0,
                    max: initFilters[i].values.length - 1,
                    values: [filter[initFilters[i].id].min, filter[initFilters[i].id].max],
                    slide: function() { window.setTimeout(updateFilter, 1); }
                });
                break;

            case "checkboxes":
                if (!(initFilters[i].id in filter) || typeof filter[initFilters[i].id] !== 'object')
                    filter[initFilters[i].id] = {};

                for (let j = 0; j < initFilters[i].values.length; j++)
                {
                    if (!(initFilters[i].values[j] in filter[initFilters[i].id]))
                        filter[initFilters[i].id][initFilters[i].values[j]] = true;
                    $(`input#filter-${initFilters[i].id}-${initFilters[i].values[j]}`).prop('checked', filter[initFilters[i].id][initFilters[i].values[j]]);
                }
                break;

            case "flags":
                if (!(initFilters[i].id in filter) || typeof filter[initFilters[i].id] !== 'object')
                    filter[initFilters[i].id] = {};

                for (let j = 0; j < initFilters[i].values.length; j++)
                {
                    let op = filter[initFilters[i].id][initFilters[i].values[j]] || 'e';
                    $(`input#filter-${initFilters[i].id}-${initFilters[i].values[j]}-y`).prop('checked', op == 'y');
                    $(`input#filter-${initFilters[i].id}-${initFilters[i].values[j]}-n`).prop('checked', op == 'n');
                    $(`input#filter-${initFilters[i].id}-${initFilters[i].values[j]}-e`).prop('checked', op == 'e');
                }
                break;
        }
    }

    // Make language checkboxes
    const languagesOption = document.getElementById("languages-option");
    for (const language of languages)
    {
        languagesOption.appendChild(
            el("div", null,
                el("input", "language-toggle", { type: "checkbox", name: "language", id: `lang-${language}`, "data-lang": language }),
                el("label", null, { for: `lang-${language}` }, language)
            )
        );
    }

    $("input.language-toggle").click(function() { preferredLanguages[$(this).data("lang")] = this.checked; setLanguages(preferredLanguages); });
    $("button.toggle-all-languages").click(function()
    {
        for (const lang of languages)
            preferredLanguages[lang] = !preferredLanguages[lang];
        setLanguages(preferredLanguages);
    });

    updateRuleseed();
    setView(view);
    setLanguages(preferredLanguages);
    setLinksAndPreferredManuals();
    setSort(sort, reverse);
    setResultsMode(resultsMode, resultsLimit);
    setTheme(theme);
    setDisplayOptions(displayOptions);
    setSearchOptions(searchOptions);

    // This also calls updateFilter()
    setSelectable(selectable);

    $('input.set-selectable').click(function() { setSelectable($(this).data('selectable')); });
    $('input.filter').click(function() { updateFilter(); });
    $("input.results-mode").click(function() { setResultsMode(this.value, resultsLimit); });
    $("input#results-limit").change(function() { setResultsMode(resultsMode, this.value | 0); });
    $("input.set-theme").click(function() { setTheme($(this).data('theme')); });
    $('input.display').click(function() { setDisplayOptions(Ktane.InitDisplays.filter(function(x) { return !$('#display-' + x).length || $('#display-' + x).prop('checked'); })); });
    $('input#display-restricted-manuals').click(function() { setLinksAndPreferredManuals(); });
    $('input#profile-file').change(function() { const files = document.getElementById('profile-file').files; if (files.length === 1) { setProfile(files[0]); } });
    $('.search-field-clear').click(function() { disappear(); let inp = this.parentNode.querySelector("input[type='text']"); inp.value = ''; inp.focus(); updateFilter(); return false; });
    $('input.search-option-input,input.search-option-checkbox').click(function() { setSearchOptions(validSearchOptions.filter(function(x) { return !$('#search-' + x).length || $('#search-' + x).prop('checked'); })); updateFilter(); });

    function stringTruncate(str, limit)
    {
        let final = str;
        if (str.length > limit)
        {
            let words = str.split(' ');
            for (let w = words.length - 1; w >= 3; w--)
            {
                let left = words.slice(0, w - 1).join(' ');
                let right = words.slice(words.length - 2).join(' ');
                if (left.length + right.length <= limit)
                    return `${left} … ${right}`;
            }
            let divi = Math.floor(limit * 0.7);
            final = str.substr(0, divi) + ' … ' + str.substr(str.length - limit + divi);
        }
        return final;
    }

    function withoutArticle(name)
    {
        return name.replace(/^the /i, '');
    }
    function excludeArticleSort(a, b)
    {
        return withoutArticle(a).localeCompare(withoutArticle(b));
    }

    let switcherData = { missionSheetsLoaded: false, missions: {} };
    $('#search-switcher').click(function()
    {
        disappear();
        let uis = Array.from(document.getElementsByClassName('search-container'));
        let cur = uis.findIndex(elem => elem.classList.contains('visible'));
        cur = (cur + 1) % uis.length;
        uis.forEach(elem => { elem.classList.remove('visible'); });
        uis[cur].classList.add('visible');

        switch (cur)
        {
            case 0:     // Find textbox
                break;

            case 1:     // Missions drop-down
                if (!switcherData.missionSheetsLoaded)
                {
                    switcherData.missionSheetsLoaded = true;

                    const bombsURL = 'https://bombs.samfun.dev/json/missionsreduced';
                    const sheetname = 'Challenge Bombs';

                    const sel = document.getElementById('search-field-mission');
                    sel.innerHTML = `<option value="">${translation["missionsLoading"]}</option>`;
                    let sheets = [];

                    let delay = 1;
                    let attempts = 5;
                    let attemptLoadSheet = function()
                    {
                        console.log(`Loading ${sheetname} data (${attempts} attempts)...`);
                        $.get(bombsURL, result =>
                        {
                            console.log(`Loading ${sheetname} data: success`);

                            let prevValue = sel.value;
                            sheets.push(...result.map(obj =>
                            {
                                const solvedcss = obj.designedForTP ? 'tp' : (obj.completions > 0 || obj.tpSolve) ? 'solved' : 'unsolved';
                                return { title: obj.name, label: stringTruncate(obj.name, 50), css: solvedcss };
                            }));
                            let vanillas = modules.filter(mod => mod.Origin === 'Vanilla');
                            let vanillaNeedy = vanillas.filter(mod => mod.Type === 'Needy').map(mod => mod.ModuleID);
                            let vanillaReg = vanillas.filter(mod => mod.Type === 'Regular').map(mod => mod.ModuleID);
                            let allNeedy = modules.filter(mod => mod.Type === 'Needy');
                            let allModsNeedy = allNeedy.filter(mod => mod.Origin === 'Mods').map(mod => mod.ModuleID);
                            allNeedy = allNeedy.map(mod => mod.ModuleID);

                            switcherData.missions = result.reduce((acc, curr) =>
                            {
                                let extra = curr.moduleList.includes('ALL_VANILLA_SOLVABLE') ? vanillaReg : [];
                                if (curr.moduleList.includes('ALL_VANILLA_NEEDY')) extra.push(...vanillaNeedy);
                                if (curr.moduleList.includes('ALL_NEEDY')) extra.push(...allNeedy);
                                if (curr.moduleList.includes('ALL_MODS_NEEDY')) extra.push(...allModsNeedy);

                                acc[curr.name] = {
                                    moduleList: [...new Set([...curr.moduleList, ...extra])],
                                    tpSolve: curr.tpSolve,
                                    completions: curr.completions
                                };
                                return acc;
                            }, {});
                            sheets.sort((a, b) => excludeArticleSort(a.title, b.title));
                            sel.innerHTML = '<option value=""></option>' + sheets.map(m => `<option class="${m.css}" value="${encodeURIComponent(m.title)}"></option>`).join('');
                            Array.from(sel.querySelectorAll('option')).forEach((opt, ix) => { opt.innerText = ix === 0 ? translation["missionsNoneSelected"] : sheets[ix - 1].label; });
                            sel.value = prevValue;
                        }, 'json')
                            .fail(function()
                            {
                                if (attempts-- > 0)
                                {
                                    console.log(`Loading ${sheetname} data: failure, retrying`);
                                    setTimeout(attemptLoadSheet, 700);
                                }
                                else
                                {
                                    console.log(`Loading ${sheetname} data: failure, giving up`);
                                    alert(`bombs.samfun.dev is not letting me load the ${sheetname} missions json data.`);
                                }
                            });
                    };
                    setTimeout(attemptLoadSheet, delay);

                    sel.onchange = function()
                    {
                        let title = sel.value;
                        let name = decodeURIComponent(title);

                        if (name !== '')
                            document.getElementById('search-field-mission-link').setAttribute('href', `https://bombs.samfun.dev/mission/${title.replace(/'/g, "%27")}`);
                        else
                            document.getElementById('search-field-mission-link').removeAttribute('href');

                        if (name in switcherData.missions)
                        {
                            let newMissionList = new Set(switcherData.missions[name].moduleList);
                            missionList = newMissionList;
                            updateFilter();
                        }
                        else if (missionList !== null)
                        {
                            missionList = null;
                            updateFilter();
                        }
                    };
                }
                break;
        }
        uis[cur].querySelector('.sw-focus').focus();

        return false;
    });

    // Page options pop-up (mobile only)
    $('#page-opt').click(function()
    {
        $('#icons').insertAfter('#page-opt-popup>div.close');
        $('#page-opt-popup').show();
        return false;
    });

    let lastLnk;
    function popup(lnk, wnd, width)
    {
        let wasVisible = wnd.is(':visible');
        disappear();
        if (!wasVisible)
        {
            wnd.css({ left: '', top: '' });
            wnd.show();
            if (window.innerWidth <= 650)
            {
                // Mobile interface: CSS does it all
                wnd.css({ width: '' });
            }
            else
            {
                // Desktop interface: position relative to the tab
                wnd.css({ width: width || wnd.data('width') }).position({ my: `${lnk.data('my') || 'right'} top`, at: `${lnk.data('at') || 'right'} bottom`, of: lnk, collision: 'fit none' });
            }
        }

        if (wnd.is("#module-ui"))
            lastLnk = lnk;

        return false;
    }

    $('#icon-page-next').click(function()
    {
        let th = $(this), curPage = th.data('cur-page');
        let pages = $('#icons').children('.icon-page');
        if (typeof curPage === 'undefined')
            curPage = 0;
        curPage = (curPage + 1) % pages.length;
        th.data('cur-page', curPage);
        pages.removeClass('shown');
        $(pages[curPage]).addClass('shown');
        return false;
    });

    $('.popup-link').click(function()
    {
        let pp = $(this).data('popup');
        let rel = $(`#${pp}-rel`);
        let $pp = $(`#${pp}`);
        popup(rel.length ? rel : $(`#${pp}-link`), $pp);
        Array.from($pp.find('.focus-on-show').focus()).forEach(x => $(x).select());
        return false;
    });
    $('.view-link').click(function()
    {
        setView($(this).data('view'));
        return false;
    });
    $('#toggle-view').click(function()
    {
        setView(validViews[(validViews.indexOf(view) + 1) % validViews.length]);
        return false;
    });

    $('#rule-seed-input').on('change', updateRuleseed);

    // Links in the table headers (not visible on mobile UI)
    $('.sort-header').click(function()
    {
        let arr = Object.keys(sorts);
        let ix = -1;
        for (let i = 0; i < arr.length; i++)
            if (arr[i] === sort)
                ix = i;
        ix = (ix + 1) % arr.length;
        setSort(arr[ix], reverse);
        return false;
    });

    // Radio buttons (in “Filters”)
    $('input.sort').click(function() { setSort(this.value, reverse); return true; });
    $('input.sort-reverse').click(function() { setSort(sort, this.checked); return true; });
    $('.popup').click(function() { preventDisappear++; });
    $('.popup>.close').click(disappear);

    $("#search-field")
        .focus(updateSearchHighlight)
        .blur(function() { modules.forEach(mod => mod.FncsSetHighlight.forEach(fnc => fnc(false))); })
        .keyup(function(e)
        {
            if (e.keyCode === 38 || e.keyCode === 40 || e.keyCode == 13)   // up/down arrows, enter
                return;
            updateFilter();
            updateSearchHighlight();
        })
        .keydown(function(e)
        {
            let visible = modules.filter(mod => mod.IsVisible);
            if (e.keyCode === 38 && selectedIndex > 0)   // up arrow
                selectedIndex--;
            else if (e.keyCode === 40 && selectedIndex < visible.length - 1)      // down arrow
                selectedIndex++;
            else if (e.keyCode === 13 && visible[selectedIndex].SelectableLinkUrl !== null)
            {
                if (!e.originalEvent.ctrlKey && !e.originalEvent.shiftKey && !e.originalEvent.altKey)  // enter
                    window.location.href = visible[selectedIndex].SelectableLinkUrl;
                else    // enter with a modifier (Ctrl, Alt, Shift)
                {
                    // This seems to work in Firefox (it dispatches the keypress to the link), but not in Chrome. Adding .trigger(e) also doesn’t work
                    visible[selectedIndex].ViewData[view].SelectableLink.focus();
                    setTimeout(function()
                    {
                        let inp = document.getElementById('search-field');
                        inp.focus();
                        inp.setSelectionRange(0, inp.value.length);
                    }, 1);
                }
            }

            updateSearchHighlight();
        });
    $("#filter-invert-search").on("input", e =>
    {
        invertSearch = $(e.target).prop("checked");
        lStorage.setItem("invert-search-results", invertSearch);
        updateFilter();
    });

    $('.select-on-focus').focus(function() { this.setSelectionRange(0, this.value.length); });

    $('#generate-pdf').click(function()
    {
        $('#generate-pdf-json').val(JSON.stringify({
            preferredManuals: preferredManuals,
            sort: sort,
            filter: filter,
            selectable: selectable,
            searchOptions: searchOptions,
            search: $("input#search-field").val(),
            profileVetoList: profileVetoList,
            filterEnabledByProfile: $('input#filter-profile-enabled').prop('checked'),
            filterVetoedByProfile: $('input#filter-profile-disabled').prop('checked'),
            searchBySymbol: document.getElementById('option-include-symbol').checked,
            searchBySteamID: document.getElementById('option-include-steam-id').checked,
            searchByModuleID: document.getElementById('option-include-module-id').checked,
            dispAllContr: document.getElementById('display-all-contributors').checked,
            displayDesc: document.getElementById('display-description').checked,
            displayTags: document.getElementById('display-tags').checked,
            restrictedManuals: displayRestrictedManuals ? [''] : restrictedManualNames
        }));
        return true;
    });


    // For the JSON module info editing UI
    let editableArrays = [
        { id: 'description', list: mod => mod.Descriptions, keys: ['Language', 'Description', 'Tags'], formName: 'Descriptions' },
        { id: 'tutorial-video', list: mod => mod.TutorialVideos, keys: ['Language', 'Description', 'Url'], formName: 'TutorialVideos' }
    ];
    function setEditUi(mod)
    {
        let ui = document.getElementById('module-ui');
        for (let key of 'Name,ModuleID,SortKey,SteamID,Author,SourceUrl,Symbol,Type,Origin,Issues,IssuesExplanation,Published,DefuserDifficulty,ExpertDifficulty,TranslationOf,RuleSeedSupport,BossStatus,MysteryModule'.split(','))
            ui.querySelector(`[name="${key}"]`).value = (mod[key] || '');

        ui.querySelector('[name="DBMLIgnored"]').checked = (mod.DBMLIgnored || false);

        if (!mod['BossStatus'] && Object.keys(mod).length > 0)
            ui.querySelector(`[name="BossStatus"]`).value = 'NotABoss';
        if (!mod['MysteryModule'] && Object.keys(mod).length > 0)
            ui.querySelector(`[name="MysteryModule"]`).value = 'NoConflict';

        for (let key of 'Quirks'.split(','))
        {
            let modValues = mod[key] ? mod[key].split(', ') : [];
            for (let enumValue of ui.querySelector(`tr#edit-${key}`).dataset.enumValues.split(','))
                ui.querySelector(`#input-${key}-${enumValue}`).checked = modValues.includes(enumValue);
        }

        if (document.getElementById('nested-Souvenir').checked = mod.Souvenir != undefined)
            for (let key of 'Status,Explanation'.split(','))
                ui.querySelector(`[name="${key}"]`).value = (mod.Souvenir[key] || '');

        if (document.getElementById('nested-Contributors').checked = mod.Contributors != undefined)
        {
            let inputKeys = 'Developer,Manual,ManualGraphics,TwitchPlays,Maintainer,Audio,Modeling,Idea'.split(',');
            let contrKeys = Object.assign({}, ...inputKeys.map(k => ({ [k]: ui.querySelector(`tr#edit-${k} th`).innerText })));
            for (let key of inputKeys)
                ui.querySelector(`[name="${key}"]`).value = (mod.Contributors[contrKeys[key]] ? mod.Contributors[contrKeys[key]].join('; ') : '');
        }

        ui.querySelector(`[name="ObsoleteSteamIDs"]`).value = mod.ObsoleteSteamIDs ? mod.ObsoleteSteamIDs.join(', ') : '';
        ui.querySelector(`[name="Ignore"]`).value = mod.Ignore ? mod.Ignore.join('; ') : '';

        for (let inf of editableArrays)
        {
            let rowHtml = `${"<td><input type='text' value='' /></td>".repeat(inf.keys.length)}<td><button type='button'>−</button></td>`;
            let tbody = ui.querySelector(`table.${inf.id}-list>tbody`);
            function removeRow(row)
            {
                let trs = Array.from(tbody.querySelectorAll('tr'));
                let buttons = Array.from(tbody.querySelectorAll('button[type="button"]'));
                trs[row].remove();
                for (let i = row + 1; i < trs.length; i++)
                    buttons[i].onclick = (function(j) { return function() { return removeRow(j); }; })(i - 1);
                return false;
            }
            if (inf.list(mod) && inf.list(mod).length > 0)
            {
                tbody.innerHTML = inf.list(mod).map(_ => `<tr>${rowHtml}</tr>`).join('');
                let inputs = tbody.querySelectorAll('input[type="text"]');
                let buttons = tbody.querySelectorAll('button[type="button"]');
                for (let i = 0; i < inf.list(mod).length; i++)
                {
                    for (let k = 0; k < inf.keys.length; k++)
                        inputs[inf.keys.length * i + k].value = inf.list(mod)[i][inf.keys[k]] || '';
                    buttons[i].onclick = (function(j) { return function() { return removeRow(j); }; })(i);
                }
            }
            else
                tbody.innerHTML = '';
            ui.querySelector(`#${inf.id}-add`).onclick = function()
            {
                let c = tbody.querySelectorAll('tr').length;
                let tr = document.createElement('tr');
                tr.innerHTML = rowHtml;
                tbody.appendChild(tr);
                tr.querySelector('button[type="button"]').onclick = function() { return removeRow(c); };
                return false;
            };
        }
        UpdateEditUiElements();
    }

    function UpdateEditUiElements()
    {
        let ui = document.getElementById('module-ui');
        let rows = ui.querySelectorAll('tr.editable-row');
        for (let i = 0; i < rows.length; i++)
        {
            if (rows[i].id === 'edit-Author')
                rows[i].style.display = ui.querySelector('#nested-Contributors').checked ? 'none' : '';
            if (!rows[i].dataset.editableIf)
                continue;
            let elem = ui.querySelector(`[name="${rows[i].dataset.editableIf}"]`);
            let val = elem.value;
            let vals = rows[i].dataset.editableIfValues.split(',');
            rows[i].style.display = vals.indexOf(val) === -1 ? 'none' : '';
        }
        ui.querySelector(`[name="Author"]`);
        let tables = ui.querySelectorAll('table.fields');
        for (let i = 0; i < tables.length; i++)
        {
            if (!tables[i].dataset.nested)
                continue;
            let elem = document.getElementById(`nested-${tables[i].dataset.nested}`);
            tables[i].style.display = elem.checked ? '' : 'none';
        }

        const license = ui.querySelector('select[name="License"]');
        const agreement = ui.querySelector('#license-agreement');
        agreement.style.display = license.value == "OpenSource" || license.value == "OpenSourceClone" ? '' : 'none';
    }
    Array.from(document.getElementById('module-ui').querySelectorAll('input,textarea,select')).forEach(elem => { elem.onchange = UpdateEditUiElements; });
    UpdateEditUiElements();

    document.getElementById('module-json-new').onclick = function()
    {
        setEditUi({ "Descriptions": [{ "Language": "English", "Description": "" }] });
        popup($('#tools-rel'), $('#module-ui'));
        return false;
    };
    document.getElementById('generate-json').onclick = function()
    {
        let form = document.getElementById('generate-json').form;
        let openSource = form.License.value === "OpenSource" || form.License.value === "OpenSourceClone";
        if (form.Name.value === "")
        {
            alert("You do need to supply at least a name for the module or widget.");
            return false;
        }
        else if (form.SourceUrl.value === "" && openSource)
        {
            alert("A link to the source code must be provided to use the open source license.");
            return false;
        }
        else if (form.SourceUrl.value !== "" && !openSource)
        {
            alert("If a link to the source code is provided then you must use the open source or the open source clone license.");
            return false;
        }
        else if (openSource && !form.LicenseAgreement.checked)
        {
            alert("You must read and agree to the modkit license.");
            return false;
        }
        for (let el of document.querySelectorAll(".use-dict-editor"))
        {
            if (!el.value
                .split(new RegExp(`(${el.dataset.allowedseparators})`))
                .filter(str => !el.dataset.allowedseparators.includes(str))
                .every(str => new RegExp(`^[^${el.dataset.alloweddictseparators}]+(${el.dataset.alloweddictseparators})[^${el.dataset.alloweddictseparators}]+$`).test(str))
            )
            {
                alert(`Invalid dict value for field ${el.getAttribute("name")}`);
                return false;
            }
        }
        for (let inf of editableArrays)
        {
            let arrayInputs = Array.from(form.querySelectorAll(`table.${inf.id}-list input[type="text"]`));
            let objects = [];
            for (let i = 0; i < arrayInputs.length; i += inf.keys.length)
            {
                let obj = {};
                for (let k = 0; k < inf.keys.length; k++)
                    obj[inf.keys[k]] = arrayInputs[i + k].value;
                objects.push(obj);
            }
            form[inf.formName].value = JSON.stringify(objects);
        }
    };
    document.getElementById('show-license').onclick = function()
    {
        popup(lastLnk, $('#license'));
        return false;
    };
    document.getElementById('back-to-json').onclick = function()
    {
        popup(lastLnk, $('#module-ui'));
        return false;
    };
}
