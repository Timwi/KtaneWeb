declare const Ktane: { Themes: any };

// Handle access to localStorage
var lStorage = localStorage;

try
{
    localStorage.setItem("testStorage", "testData");
    localStorage.removeItem("testStorage");
}
catch (e)
{
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
            for (var key in this.storage)
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
var theme = lStorage.getItem("theme");
if (!(theme in Ktane.Themes))
    theme = null;
if (theme in Ktane.Themes)
    document.getElementById("theme-css").setAttribute('href', Ktane.Themes[theme]);
else
    document.getElementById("theme-css").setAttribute('href', '');

function el<K extends keyof HTMLElementTagNameMap>(tagName: K, className?: string, ...args: any[]): HTMLElementTagNameMap[K]
{
    const element = document.createElement(tagName);
    if (className) element.className = className;
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

function initializePage(modules: KtaneModuleInfo[], initIcons, initDocDirs, initDisplays, initFilters, initSelectables, souvenirAttributes, iconSpriteMd5)
{
    // Find all the languages
    function getLanguageFromSheet(sheet)
    {
        const matches = sheet.match(/^ translated \((?:(.+) — .+|(.+))?\)/);
        return matches !== null ? (matches[2] || matches[1]) : "English";
    }

    const languages = modules
        .map(module => module.Sheets.map(getLanguageFromSheet))
        .reduce((a, b) => a.concat(b))
        .filter((value, index, array) => array.indexOf(value) === index);

    languages.sort();

    const languageCodes = {
        "Dansk": "da",
        "Deutsch": "de",
        "Eesti": "et",
        "English": "en",
        "Español": "es",
        "Esperanto": "eo",
        "Français": "fr",
        "Italiano": "it",
        "Magyar": "hu",
        "Nederlands": "nl",
        "Norsk": "no",
        "Polski": "pl",
        "Português do Brasil": "pt-br",
        "Suomi": "fi",
        "Svenska": "sv",
        "Türkçe": "tr",
        "Čeština": "cs",
        "Български": "bg",
        "Русский": "ru",
        "Українске": "uk",
        "עברית": "he",
        "العربية": "ar",
        "ภาษาไทย": "th",
        "日本語": "jp",
        "简体中文": "zh-CN",
        "繁體中文": "zh-TW",
        "한국어": "ko"
    };

    var filter = {};
    try { filter = JSON.parse(lStorage.getItem('filters') || '{}') || {}; }
    catch (exc) { }
    var selectable = lStorage.getItem('selectable') || 'manual';
    if (initSelectables.map(sel => sel.PropName).indexOf(selectable) === -1)
        selectable = 'manual';
    var preferredManuals = {};
    try { preferredManuals = JSON.parse(lStorage.getItem('preferredManuals') || '{}') || {}; }
    catch (exc) { }
    var preferredLanguages = {};
    try { preferredLanguages = JSON.parse(lStorage.getItem('preferredLanguages') || '{}') || {}; }
    catch (exc) { }

    $('#rule-seed-input').val(+lStorage.getItem('ruleseed') || 1);
    function updateRuleseed()
    {
        setLinksAndPreferredManuals();
        var seed = +($('#rule-seed-input').val() || 0);
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
    var defdiffFilterValues = initFilters.filter(f => f.id === 'defdiff')[0].values;
    var expdiffFilterValues = initFilters.filter(f => f.id === 'expdiff')[0].values;
    var sorts = {
        'name': { fnc: function(mod) { return mod.SortKey.toLowerCase(); }, reverse: false, bodyCss: 'sort-name', radioButton: '#sort-name' },
        'defdiff': { fnc: function(mod) { return defdiffFilterValues.indexOf(mod.DefuserDifficulty); }, reverse: false, bodyCss: 'sort-defdiff', radioButton: '#sort-defuser-difficulty' },
        'expdiff': { fnc: function(mod) { return expdiffFilterValues.indexOf(mod.ExpertDifficulty); }, reverse: false, bodyCss: 'sort-expdiff', radioButton: '#sort-expert-difficulty' },
        'twitchscore': { fnc: function(mod) { return mod.TwitchPlays ? mod.TwitchPlays.Score : 0; }, reverse: false, bodyCss: 'sort-twitch-score', radioButton: '#sort-twitch-score' },
        'published': { fnc: function(mod) { return mod.Published; }, reverse: true, bodyCss: 'sort-published', radioButton: '#sort-published' }
    };
    var sort = lStorage.getItem('sort') || 'name';
    if (!(sort in sorts))
        sort = 'name';
    var reverse = lStorage.getItem('sort-reverse') == "true" || false;

    var defaultDisplayOptions = ['author', 'type', 'difficulty', 'description', 'published'];
    var displayOptions = defaultDisplayOptions;
    try { displayOptions = JSON.parse(lStorage.getItem('display')) || defaultDisplayOptions; } catch (exc) { }

    var validSearchOptions = ['names', 'authors', 'descriptions'];
    var defaultSearchOptions = ['names'];
    var searchOptions = defaultSearchOptions;
    try { searchOptions = JSON.parse(lStorage.getItem('searchOptions')) || defaultSearchOptions; } catch (exc) { }

    var validViews: (keyof ModuleViewDatas)[] = ['List', 'PeriodicTable'];
    var view: keyof ModuleViewDatas = <keyof ModuleViewDatas>lStorage.getItem('view');
    if (validViews.indexOf(view) === -1)
        view = 'List';

    let profileVetoList = null;

    var version: number = JSON.parse(lStorage.getItem('version')) || 0;
    if (version < 2)
    {
        sort = 'name';
        selectable = 'manual';
        displayOptions = defaultDisplayOptions;
        filter = {};
        view = 'List';
    }
    lStorage.setItem('version', '2');

    // Refers to a module if the “Find” box contains the exact Periodic Table symbol for a module
    let filterCurrentlyIncludesSymbol = null;

    var selectedIndex = 0;
    function updateSearchHighlight()
    {
        let visible = modules.filter(mod => mod.IsVisible);
        if (selectedIndex < 0)
            selectedIndex = 0;
        if (selectedIndex >= visible.length)
            selectedIndex = visible.length - 1;
        for (let i = 0; i < visible.length; i++)
            for (let fnc of visible[i].FncsSetHighlight)
                fnc(i === selectedIndex);
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
            if (a === filterCurrentlyIncludesSymbol)
                return -1;
            if (b === filterCurrentlyIncludesSymbol)
                return 1;
            var c = compare(sorts[srt].fnc(a), sorts[srt].fnc(b), sorts[srt].reverse);
            return (c === 0) ? compare(a.SortKey, b.SortKey, false) : c;
        });

        if (rvrse) modules.reverse();

        viewsReady.get(view).Sort();

        $(document.body).removeClass(document.body.className.split(' ').filter(cls => cls.startsWith('sort-')).join(' ')).addClass(sorts[srt].bodyCss);
        $(sorts[srt].radioButton).prop('checked', true);
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();
    }

    function setDisplayOptions(set)
    {
        displayOptions = (set instanceof Array) ? set.filter(function(x) { return initDisplays.indexOf(x) !== -1; }) : defaultDisplayOptions;
        $(document.body).removeClass(document.body.className.split(' ').filter(function(x) { return x.startsWith('display-'); }).join(' '));
        $('input.display').prop('checked', false);
        $(document.body).addClass(displayOptions.map(function(x) { return "display-" + x; }).join(' '));
        $(displayOptions.map(function(x) { return '#display-' + x; }).join(',')).prop('checked', true);
        lStorage.setItem('display', JSON.stringify(displayOptions));
    }

    function setSearchOptions(set)
    {
        searchOptions = (set instanceof Array) ? set.filter(function(x) { return validSearchOptions.indexOf(x) !== -1; }) : defaultSearchOptions;
        $('input.search-option-input').prop('checked', false);
        $(searchOptions.map(function(x) { return '#search-' + x; }).join(',')).prop('checked', true);
        lStorage.setItem('searchOptions', JSON.stringify(searchOptions));
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
                if (profile.DisabledList)
                {
                    profileVetoList = profile.DisabledList;
                    $(".filter-profile-enabled-text").text('\u00a0Enabled by ' + file.name);
                    $(".filter-profile-disabled-text").text('\u00a0Vetoed by ' + file.name);
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

    //var viewsReady: { [view in keyof ModuleViewDatas]: ViewData } = {};
    var viewsReady: Map<keyof ModuleViewDatas, ViewData> = new Map();

    function createView(newView: keyof ModuleViewDatas)
    {
        if (viewsReady.has(newView))
            return true;

        function setCompatibilityTooltip(element, mod)
        {
            const compatiblities = {
                Unplayable: 'This module has a programming bug that prevents it from being played reliably.',
                Untested: 'The compatibility of this module has not yet been determined.',
                Problematic: 'This module exhibits a cosmetic or other minor problem that doesn’t affect its playability.',
            };

            if (compatiblities[mod.Compatibility] === undefined)
                return;

            element.setAttribute('title', compatiblities[mod.Compatibility] + (mod.CompatibilityExplanation !== undefined ? ` ${mod.CompatibilityExplanation}` : ''));
        }

        switch (newView)
        {
            case 'List': {
                const mainTable = document.getElementById("main-table").getElementsByTagName("tbody")[0];

                for (var i = 0; i < modules.length; i++)
                {
                    let mod = modules[i];

                    let tr = el("tr", `mod compatibility-${mod.Compatibility}${mod.TwitchPlays === null ? '' : ' tp'}${mod.RuleSeedSupport === 'Supported' ? ' rs' : ''}`);
                    mainTable.appendChild(tr);
                    mod.FncsShowHide.push(sh => { tr.style.display = (sh ? '' : 'none'); });
                    mod.FncsSetHighlight.push(hgh =>
                    {
                        if (hgh)
                            tr.classList.add('selected');
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
                            let iconImg = el("img", "icon", { title: sel.HumanReadable, alt: sel.HumanReadable, src: sel.Icon });
                            let lnkA = el("a", sel.CssClass, { href: sel.UrlFunction(mod, mod.Manuals) }, iconImg);
                            td.appendChild(lnkA);
                            if (sel.PropName === 'manual')
                            {
                                mod.FncsSetManualIcon.push(url => { iconImg.src = url; });
                                mod.FncsSetManualLink.push(url => { lnkA.href = url; });
                            }
                        }
                    }

                    let icon = el("div", "mod-icon", { style: `background-image:url(iconsprite/${iconSpriteMd5});background-position:-${mod.X * 32}px -${mod.Y * 32}px;` });
                    let modlink = el("a", "modlink", icon, el("span", "mod-name", mod.Name));
                    setCompatibilityTooltip(modlink, mod);
                    mod.ViewData.List = { TableRow: tr, SelectableLink: modlink };
                    let td1 = el("td", "infos-1", el("div", "modlink-wrap", modlink));
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
                        el("div", "inf-type inf", mod.Type),
                        el("div", "inf-origin inf inf2", mod.Origin));
                    if (mod.Type === 'Regular' || mod.Type === 'Needy')
                    {
                        function readable(difficulty)
                        {
                            var result = '';
                            for (var i = 0; i < difficulty.length; i++)
                            {
                                if (i > 0 && difficulty[i] >= 'A' && difficulty[i] <= 'Z')
                                    result += ' ';
                                result += difficulty[i].toLowerCase();
                            }
                            return result;
                        }
                        if (mod.DefuserDifficulty === mod.ExpertDifficulty)
                            infos.append(el("div", "inf-difficulty inf inf2", el("span", "inf-difficulty-sub", readable(mod.DefuserDifficulty))));
                        else
                            infos.append(el("div", "inf-difficulty inf inf2", el("span", "inf-difficulty-sub", readable(mod.DefuserDifficulty)), ' (d), ', el("span", "inf-difficulty-sub", readable(mod.ExpertDifficulty)), ' (e)'));
                    }
                    infos.append(el("div", "inf-author inf", mod.Author),
                        el("div", "inf-published inf inf2", mod.Published));
                    if (mod.TwitchPlays)
                    {
                        if (mod.Type === 'Needy')
                            mod.TwitchPlaysInfo = `This needy module can be played in “Twitch Plays: KTANE” for a ${mod.TwitchPlays.NeedyScoring === 'Time' ? 'time-based' : 'solve-based'} score of ${mod.TwitchPlays.Score}.`;
                        else if (mod.TwitchPlays.ScorePerModule)
                            mod.TwitchPlaysInfo = `This module can be played in “Twitch Plays: KTANE” for a score of ${mod.TwitchPlays.Score ? `${mod.TwitchPlays.Score}, plus ` : ''}${mod.TwitchPlays.ScorePerModule} for each module on the bomb${mod.TwitchPlays.ScorePerModuleCap ? ` up to a maximum of ${mod.TwitchPlays.ScorePerModuleCap}` : ''}.`;
                        else
                            mod.TwitchPlaysInfo = `This module can be played in “Twitch Plays: KTANE” for a score of ${mod.TwitchPlays.Score}.`;
                        infos.append(el("div", "inf-twitch inf inf2", { title: mod.TwitchPlaysInfo },
                            mod.TwitchPlays.ScorePerModule ? 'S' : mod.TwitchPlays.Score));
                    }
                    if (mod.RuleSeedSupport === 'Supported')
                    {
                        mod.RuleSeedInfo = 'This module’s rules/manual can be dynamically varied using the Rule Seed Modifier.';
                        infos.append(el("div", "inf-rule-seed inf inf2", { title: mod.RuleSeedInfo }));
                    }

                    var value = !('Souvenir' in mod) || mod.Souvenir === null || !('Status' in mod.Souvenir) ? 'Unexamined' : mod.Souvenir.Status;
                    var attr = souvenirAttributes[value];
                    var expl = mod.Souvenir && mod.Souvenir.Explanation;
                    mod.SouvenirInfo = `${attr.Tooltip}${expl ? "\n" + expl : ""}`;
                    infos.append(el("div", `inf-souvenir inf inf2${expl ? " souvenir-explanation" : ""}`, { title: mod.SouvenirInfo }, attr.Char));
                    if (mod.ModuleID)
                        infos.append(el("div", "inf-id inf", mod.ModuleID));
                    infos.append(el("div", "inf-description inf", mod.Description));
                    td1.appendChild(infos);
                    td2.appendChild(infos.cloneNode(true));

                    var lnk1 = el("a", "manual-selector", { href: "#" });
                    $(lnk1).click(makeClickHander(lnk1, false, mod));
                    td1.appendChild(lnk1);

                    var lnk2 = el("a", "mobile-opt", { href: "#" });
                    $(lnk2).click(makeClickHander(lnk2, true, mod));
                    tr.appendChild(el("td", "mobile-ui", lnk2));
                }

                viewsReady.set('List', {
                    Show: function() { document.getElementById("main-table").style.display = 'table'; },
                    Hide: function() { document.getElementById("main-table").style.display = 'none'; },
                    Sort: function() { mainTable.append(...modules.map(mod => mod.ViewData.List.TableRow)); }
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

                for (let i = 0; i < modules.length; i++)
                {
                    let mod = modules[i];
                    let manualSelector = el('a', 'manual-selector', { href: '#' });
                    let a = el('a', `module ${mod.ExpertDifficulty} compatibility-${mod.Compatibility}`,
                        el('div', `symbol ${mod.DefuserDifficulty}`, mod.Symbol || '??', el('div', 'icon', { style: `background-image:url(iconsprite/${iconSpriteMd5});background-position:-${mod.X * 32}px -${mod.Y * 32}px` })),
                        el('div', 'name', el('div', 'inner', mod.Name)),
                        el('div', 'tpscore', mod.TwitchPlays ? mod.TwitchPlays.Score : ''),
                        el('div', 'souvenir', souvenirStatuses[(mod.Souvenir && mod.Souvenir.Status) || 'Unexamined']),
                        manualSelector);
                    setCompatibilityTooltip(a, mod.Compatibility);

                    $(manualSelector).click(makeClickHander(manualSelector, false, mod));

                    document.getElementById("actual-periodic-table").appendChild(a);

                    mod.ViewData.PeriodicTable = { SelectableLink: a };
                    mod.FncsShowHide.push(sh => { a.style.display = sh ? 'block' : 'none'; });
                    mod.FncsSetSelectable.push(url => { a.href = url; });
                    mod.FncsSetHighlight.push(hgh =>
                    {
                        if (hgh)
                            a.classList.add('highlight');
                        else
                            a.classList.remove('highlight');
                    });
                }

                // Assignments table
                let symbols = modules.filter(m => m.Symbol).map(m => m.Symbol);
                symbols.sort();
                let alphabet = ",a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z".split(',');

                let table = el('table', 'assignment-table');

                let colgroup = el('colgroup');
                table.appendChild(colgroup);
                for (let col = 0; col < alphabet.length + 3; col++)
                {
                    let colTag = el('col');
                    if (col > 0 && col < alphabet.length + 1)
                        colTag.style.width = '32px';
                    colgroup.appendChild(colTag);
                }

                // Header row
                let tr = el('tr', null, el('td'));
                for (let col = 0; col < alphabet.length; col++)
                    tr.appendChild(el('th', 'letter', alphabet[col].length ? alphabet[col] : '∅'));
                table.appendChild(tr);

                for (let row = 0; row < 26; row++)
                {
                    let letter = String.fromCharCode(65 + row);
                    tr = el('tr');
                    function makeTh()
                    {
                        tr.appendChild(el('th', 'letter', { title: symbols.filter(k => k.startsWith(letter)).map(k => `${k} = ${modules.filter(m => m.Symbol === k)[0].Name}`).join("\n") }, letter));
                    }
                    makeTh();
                    for (let col = 0; col < alphabet.length; col++)
                    {
                        let module = modules.filter(m => m.Symbol === letter + alphabet[col]);
                        let td = el('td', `module${module.length > 1 ? ' clash' : ''}`, { title: module.length > 0 ? module.map(md => `${md.Symbol} = ${md.Name}`).join('\n') : '' });
                        if (module.length === 1)
                            td.appendChild(el('div', 'icon', { style: `background-image:url(iconsprite/${iconSpriteMd5}); background-position:-${module[0].X * 32}px -${module[0].Y * 32}px;` }));
                        tr.appendChild(td);
                    }
                    makeTh();

                    let filtered = symbols.filter(a => a.startsWith(letter));
                    let td2 = el('td', 'letter-list', `${alphabet.filter(lt => filtered.indexOf(letter + lt) === -1).map(lt => lt === '' ? '∅' : lt).join('')}`);
                    tr.appendChild(td2);

                    table.appendChild(tr);
                }
                document.getElementById("assignment-table").appendChild(table);

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

    function setView(newView: keyof ModuleViewDatas)
    {
        if (createView(newView))
        {
            for (var [k, v] of viewsReady)
            {
                if (k === newView)
                    v.Show();
                else
                    v.Hide();
            }
            view = newView;
            lStorage.setItem('view', newView);
            setSort(sort, reverse);
        }
    }

    function handleDataTransfer(dataTransfer)
    {
        var url;
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
    function removeDiacritics(str: string)
    {
        return str.normalize("NFD").replace(/[\u0300-\u036f]/g, "");
    }

    function updateFilter()
    {
        var noneSelected = {};
        for (var i = 0; i < initFilters.length; i++)
        {
            var none = true;
            switch (initFilters[i].type)
            {
                case "slider":
                    filter[initFilters[i].id] = {
                        min: $('div#filter-' + initFilters[i].id).slider('values', 0),
                        max: $('div#filter-' + initFilters[i].id).slider('values', 1)
                    };
                    var x = function(str) { return str.replace(/[A-Z][a-z]*/g, function(m) { return " " + m.toLowerCase(); }).trim(); };
                    var y = function(s1, s2) { return s1 === s2 ? x(s1) : x(s1) + ' – ' + x(s2); };
                    $('div#filter-label-' + initFilters[i].id).text(y(initFilters[i].values[filter[initFilters[i].id].min], initFilters[i].values[filter[initFilters[i].id].max]));
                    none = false;
                    break;

                case "checkboxes":
                    filter[initFilters[i].id] = {};
                    for (var j = 0; j < initFilters[i].values.length; j++)
                    {
                        filter[initFilters[i].id][initFilters[i].values[j]] = $('input#filter-' + initFilters[i].id + '-' + initFilters[i].values[j]).prop('checked');
                        if (filter[initFilters[i].id][initFilters[i].values[j]])
                            none = false;
                    }
                    break;

                case "boolean":
                    filter[initFilters[i].id] = $('input#filter-' + initFilters[i].id).prop('checked');
                    break;
            }
            noneSelected[initFilters[i].id] = none;
        }

        let searchRaw = $("input#search-field").val().toString().toLowerCase();
        let searchKeywords = removeDiacritics(searchRaw).split(' ').filter(x => x.length > 0).map(x => x.replace(/'/g, '’'));
        const filterEnabledByProfile = $('input#filter-profile-enabled').prop('checked');
        const filterVetoedByProfile = $('input#filter-profile-disabled').prop('checked');

        let modCount = 0;
        let includesSymbol = null;
        modules.forEach(function(mod)
        {
            let filteredIn = true;
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
                        case "boolean":
                            filteredIn = filteredIn && (!filter[initFilters[i].id] || initFilters[i].fnc(mod) === 'True');
                            break;
                    }
                }
            }

            if (mod.Sheets.length > 0)
                filteredIn = filteredIn && mod.Sheets.map(getLanguageFromSheet).some(sheet => preferredLanguages[sheet] !== false);

            if (profileVetoList !== null)
                filteredIn = filteredIn && (profileVetoList.includes(mod.ModuleID) ? (filterVetoedByProfile || !filterEnabledByProfile) : (filterEnabledByProfile || !filterVetoedByProfile));
            let searchWhat = '';
            if (searchOptions.indexOf('names') !== -1)
                searchWhat += ' ' + mod.Name.toLowerCase() + ' ' + mod.SortKey.toLocaleLowerCase();
            if (searchOptions.indexOf('authors') !== -1)
                searchWhat += ' ' + mod.Author.toLowerCase();
            if (searchOptions.indexOf('descriptions') !== -1)
                searchWhat += ' ' + mod.Description.toLowerCase();
            if (mod.Symbol)
                searchWhat += ' ' + mod.Symbol.toLowerCase();

            searchWhat = removeDiacritics(searchWhat);

            let sh = filteredIn && searchKeywords.filter(x => searchWhat.indexOf(x) === -1).length === 0;
            if (sh)
                modCount++;
            for (let fnc of mod.FncsShowHide)
                fnc(sh);
            if (mod.Symbol && searchRaw === mod.Symbol.toLowerCase())
                includesSymbol = mod;
        });

        $('#module-count').text(modCount);
        lStorage.setItem('filters', JSON.stringify(filter));
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();

        if (includesSymbol !== filterCurrentlyIncludesSymbol)
        {
            filterCurrentlyIncludesSymbol = includesSymbol;
            setSort(sort, reverse);
        }
    }

    // Sets the module links to the current selectable and the manual icon link to the preferred manuals
    function setLinksAndPreferredManuals()
    {
        let seed = +$('#rule-seed-input').val();
        let seedHash = (seed === 1 ? '' : '#' + seed);
        for (let mod of modules)
        {
            let manual: KtaneModuleManual = null;
            if (mod.Manuals.length > 0)
            {
                manual = mod.Manuals[0];
                for (let i = 0; i < mod.Manuals.length; i++)
                    if (mod.Manuals[i].Name === mod.Name + " (PDF)")
                        manual = mod.Manuals[i];
                for (let i = 0; i < mod.Manuals.length; i++)
                    if (mod.Manuals[i].Name === mod.Name + " (HTML)")
                        manual = mod.Manuals[i];
                if (mod.Name in preferredManuals)
                    for (let i = 0; i < mod.Manuals.length; i++)
                        if (preferredManuals[mod.Name] === mod.Manuals[i].Name)
                            manual = mod.Manuals[i];
                for (let fnc of mod.FncsSetManualIcon)
                    fnc(manual === null ? null : manual.Icon);
                for (let fnc of mod.FncsSetManualLink)
                    fnc(manual === null ? null : manual.Url + seedHash);
            }
            for (let fnc of mod.FncsSetSelectable)
                fnc(selectable === 'manual' ? (manual === null ? null : manual.Url + seedHash) : (initSelectables.filter(sl => sl.PropName === selectable).map(sl => sl.UrlFunction(mod))[0] || null));
        }
        lStorage.setItem('preferredManuals', JSON.stringify(preferredManuals));
    }

    var preventDisappear = 0;
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

    // Click handler for selecting manuals/cheat sheets (both mobile and non)
    function makeClickHander(lnk: HTMLElement, isMobileOpt: boolean, mod: KtaneModuleInfo)
    {
        return function()
        {
            var numAlready = Array.from(document.getElementsByClassName('popup')).filter(p => p['data-lnk'] === lnk).length;
            disappear();
            if (numAlready)
                return false;
            var menuDiv = el('div', 'popup disappear manual-select', { 'style': 'display: block', onclick: function() { preventDisappear++; } });
            menuDiv['data-lnk'] = lnk;
            document.body.appendChild(menuDiv);
            if (isMobileOpt)
            {
                var closeButton = el('div', 'close', { onclick: disappear });
                menuDiv.appendChild(closeButton);
                var iconsDiv = el('div', 'icons');

                for (let ix = 0; ix < initSelectables.length; ix++)
                {
                    let sel = initSelectables[ix];
                    if (sel.PropName === 'manual' || !sel.ShowIconFunction(mod, mod.Manuals))
                        continue;
                    var iconDiv = el('div', 'icon',
                        el('a', 'icon-link', { href: sel.UrlFunction(mod, mod.Manuals) },
                            el('img', 'icon-img', { src: sel.Icon }),
                            el('span', 'icon-label', sel.HumanReadable)));
                    iconsDiv.appendChild(iconDiv);
                }

                menuDiv.appendChild(iconsDiv);
                if ($('#display-souvenir').prop('checked'))
                    menuDiv.appendChild(el('div', 'module-further-info', mod.SouvenirInfo));
                if ($('#display-twitch').prop('checked') && 'TwitchPlaysInfo' in mod)
                    menuDiv.appendChild(el('div', 'module-further-info', mod.TwitchPlaysInfo));
                if ($('#display-rule-seed').prop('checked') && 'RuleSeedInfo' in mod)
                    menuDiv.appendChild(el('div', 'module-further-info', mod.RuleSeedInfo));
            }
            menuDiv.appendChild(el('p', 'small-print', 'Select your preferred manual for this module.'));
            var menu = el('div', 'manual-select');
            menuDiv.appendChild(menu);
            var seed = +(<HTMLInputElement>document.getElementById('rule-seed-input')).value || 0;
            var seedHash = (seed === 1 ? '' : '#' + seed);
            var already: Map<string, { Html: Element, Pdf: Element, Row: HTMLDivElement }> = new Map();
            for (var i = 0; i < mod.Manuals.length; i++)
            {
                var rx1 = /^\s*(.*) \((HTML|PDF)\)$/.exec(mod.Manuals[i].Name.substr(mod.Name.length));
                var clickHandler = function(sh)
                {
                    return function(e)
                    {
                        menuDiv.remove();
                        preferredManuals[mod.Name] = sh;
                        setLinksAndPreferredManuals();
                        e.stopPropagation();
                        return false;
                    };
                }(mod.Manuals[i].Name);
                var link = el('a', 'link', { href: mod.Manuals[i].Url + seedHash, onclick: clickHandler }, rx1[2]);
                if (!already.has(rx1[1]))
                {
                    var trow: string[], rx2: RegExpExecArray;
                    if (rx2 = /^translated(?: full)? \((.*) — (.*)\) (.*) \((.*)\)$/.exec(rx1[1]))
                        trow = [rx2[1], rx2[2], `<div class='descriptor'>${rx2[3]}</div><div class='author'>by ${rx2[4]}</div>`];
                    else if (rx2 = /^translated(?: full)? \((.*) — (.*)\) (.*)$/.exec(rx1[1]))
                        trow = [rx2[1], rx2[2], `<div class='descriptor'>${rx2[3]}</div>`];
                    else if (rx2 = /^translated(?: full)? \((.*) — (.*)\)$/.exec(rx1[1]))
                        trow = [rx2[1], rx2[2], ""];
                    else if (rx2 = /^translated(?: full)? \((.*)\)$/.exec(rx1[1]))
                        trow = [rx2[1], mod.Name, ""];
                    else if (rx2 = /^(.*) \((.*)\)$/.exec(rx1[1]))
                        trow = ["", mod.Name, `<div class='descriptor'>${rx2[1]}</div><div class='author'>by ${rx2[2]}</div>`];
                    else
                        trow = ["", mod.Name, `<div class='descriptor'>${rx1[1]}</div>`];

                    const language = trow[0] || "English";
                    if (preferredLanguages[language] === false)
                        continue;

                    var code = languageCodes[trow[0]];
                    if (code !== undefined) {
                        trow[0] += ` (${code})`;
                    }

                    let trowElem = el('div');
                    trowElem.innerHTML = `<div class='mobile-cell'><div class='language'>${trow[0]}</div><div class='title'>${trow[1]}</div><div class='extra'>${trow[2]}</div></div><div class='link link-HTML'></div><div class='link link-PDF'></div>`;
                    menu.appendChild(trowElem);

                    already.set(rx1[1], {
                        Html: trowElem.getElementsByClassName('link-HTML')[0],
                        Pdf: trowElem.getElementsByClassName('link-PDF')[0],
                        Row: trowElem
                    });
                }
                var inf = already.get(rx1[1]);
                if (rx1[2] === 'HTML')
                    inf.Row.onclick = clickHandler;
                var elem = rx1[2] === 'HTML' ? inf.Html : inf.Pdf;
                elem.appendChild(link);
                if (mod.Name in preferredManuals && preferredManuals[mod.Name] === mod.Manuals[i].Name)
                    elem.classList.add('checked');
            }
            menuDiv.appendChild(el('div', 'bottom-links',
                el('div', null, el('a', null, { href: `find-log?find=${encodeURIComponent(mod.ModuleID)}` }, 'Find example logfile')),
                el('div', null, el('a', null, { href: '#', onclick: function() { setEditUi(mod); popup($(lnk), $('#module-ui')); return false; } }, `Edit this ${mod.Type === 'Widget' ? 'widget' : 'module'}`))));

            if (!isMobileOpt)
                $(menuDiv).position({ my: 'right top', at: 'right bottom', of: lnk, collision: 'fit none' });
            return false;
        };
    }

    // ** PROCESS ALL THE MODULES ** //
    for (var i = 0; i < modules.length; i++)
    {
        let mod = modules[i];
        mod.SelectableLinkUrl = null;
        mod.IsVisible = true;

        // (bool sh) => shows (sh) or hides (!sh) the module
        mod.FncsShowHide = [sh => { mod.IsVisible = sh; }];

        // (string url) => changes the manual icon to reflect HTML/PDF and original/embellished preference
        mod.FncsSetManualIcon = [];

        // (string url) => changes what the manual icon links to (preferred manual)
        mod.FncsSetManualLink = [];

        // (string url) => changes what the module’s main link links to (selectable)
        mod.FncsSetSelectable = [url => { mod.SelectableLinkUrl = url; }];

        // (bool hgh) => sets the highlight on or off
        mod.FncsSetHighlight = [];

        mod.ViewData = {};

        // Construct the list of manuals. (The list provided in .Sheets is kinda compressed.)
        mod.Manuals = mod.Sheets.map(str => str.split('|')).map(arr => ({
            Name: `${mod.Name}${arr[0]} (${arr[1].toUpperCase()})`,
            Url: `${initDocDirs[(parseInt(arr[2]) / 2) | 0]}/${encodeURIComponent(mod.Name)}${encodeURIComponent(arr[0])}.${arr[1]}`,
            Icon: initIcons[arr[2]]
        }));

        // Default values
        if (!mod.RuleSeedSupport)
            mod.RuleSeedSupport = 'NotSupported';
        if (!mod.Compatibility)
            mod.Compatibility = 'Untested';
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
    }

    // Set filters from saved settings
    for (var i = 0; i < initFilters.length; i++)
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
                var e = $('div#filter-' + initFilters[i].id);
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

                for (var j = 0; j < initFilters[i].values.length; j++)
                {
                    if (!(initFilters[i].values[j] in filter[initFilters[i].id]))
                        filter[initFilters[i].id][initFilters[i].values[j]] = true;
                    $('input#filter-' + initFilters[i].id + '-' + initFilters[i].values[j]).prop('checked', filter[initFilters[i].id][initFilters[i].values[j]]);
                }
                break;

            case "boolean":
                if (!(initFilters[i].id in filter) || typeof filter[initFilters[i].id] !== 'boolean')
                    filter[initFilters[i].id] = false;

                $('input#filter-' + initFilters[i].id).prop('checked', filter[initFilters[i].id]);
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

    setLanguages(preferredLanguages);

    $("input.language-toggle").click(function(this: HTMLInputElement) { preferredLanguages[$(this).data("lang")] = this.checked; setLanguages(preferredLanguages); });
    $("button.toggle-all-languages").click(function()
    {
        for (const lang of languages)
            preferredLanguages[lang] = !preferredLanguages[lang];
        setLanguages(preferredLanguages);
    });

    updateRuleseed();
    setView(view);
    setLinksAndPreferredManuals();
    setSort(sort, reverse);
    setTheme(theme);
    setDisplayOptions(displayOptions);
    setSearchOptions(searchOptions);

    // This also calls updateFilter()
    setSelectable(selectable);

    $('input.set-selectable').click(function() { setSelectable($(this).data('selectable')); });
    $('input.filter').click(function() { updateFilter(); });
    $("input.set-theme").click(function() { setTheme($(this).data('theme')); });
    $('input.display').click(function() { setDisplayOptions(initDisplays.filter(function(x) { return !$('#display-' + x).length || $('#display-' + x).prop('checked'); })); });
    $('input#profile-file').change(function() { const files = (<HTMLInputElement>document.getElementById('profile-file')).files; if (files.length === 1) { setProfile(files[0]); } });
    $('#search-field-clear').click(function() { disappear(); $('input#search-field').val(''); updateFilter(); return false; });
    $('input.search-option-input').click(function() { setSearchOptions(validSearchOptions.filter(function(x) { return !$('#search-' + x).length || $('#search-' + x).prop('checked'); })); updateFilter(); });

    // Page options pop-up (mobile only)
    $('#page-opt').click(function()
    {
        $('#icons').insertAfter('#page-opt-popup>div.close');
        $('#page-opt-popup').show();
        return false;
    });

    function popup(lnk, wnd, width?)
    {
        var wasVisible = wnd.is(':visible');
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
        return false;
    }

    $('#icon-page-next').click(function()
    {
        var th = $(this), curPage = th.data('cur-page');
        var pages = $('#icons').children('.icon-page');
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
        var pp = $(this).data('popup');
        var rel = $(`#${pp}-rel`);
        var $pp = $(`#${pp}`);
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
        var arr = Object.keys(sorts);
        var ix = -1;
        for (var i = 0; i < arr.length; i++)
            if (arr[i] === sort)
                ix = i;
        ix = (ix + 1) % arr.length;
        setSort(arr[ix], reverse);
        return false;
    });

    // Radio buttons (in “Filters”)
    $('input.sort').click(function(this: HTMLInputElement) { setSort(this.value, reverse); return true; });
    $('input.sort-reverse').click(function(this: HTMLInputElement) { setSort(sort, this.checked); return true; });
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
            var visible = modules.filter(mod => mod.IsVisible);
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
                        let inp = (<HTMLInputElement>document.getElementById('search-field'));
                        inp.focus();
                        inp.setSelectionRange(0, inp.value.length);
                    }, 1);
                }
            }

            updateSearchHighlight();
        });

    $('.select-on-focus').focus(function(this: HTMLInputElement) { this.setSelectionRange(0, this.value.length); });

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
            filterVetoedByProfile: $('input#filter-profile-disabled').prop('checked')
        }));
        return true;
    });

    $('#assignment-table-toggle').click(function()
    {
        if ($('#assignment-table:visible').length)
            $('#assignment-table').hide();
        else
            $('#assignment-table').show();
        return false;
    });


    // For the JSON module info editing UI
    function setEditUi(mod: KtaneModuleInfo)
    {
        let ui = document.getElementById('module-ui');
        for (var key of 'Name,Description,ModuleID,SortKey,SteamID,Author,SourceUrl,TutorialVideoUrl,Symbol,Type,Origin,Compatibility,CompatibilityExplanation,Published,DefuserDifficulty,ExpertDifficulty,TranslationOf,RuleSeedSupport'.split(','))
            (<any>ui.querySelector(`[name="${key}"]`)).value = (mod[key] || '');

        if ((<any>document.getElementById('nested-Souvenir')).checked = mod.Souvenir != undefined)
            for (var key of 'Status,Explanation'.split(','))
                (<any>ui.querySelector(`[name="${key}"]`)).value = (mod.Souvenir[key] || '');

        if ((<any>document.getElementById('nested-TwitchPlays')).checked = mod.TwitchPlays != undefined)
            for (var key of 'Score,ScorePerModule,ScorePerModuleCap,NeedyScoring,ScoreExplanation,TagPosition,HelpText'.split(','))
                (<any>ui.querySelector(`[name="${key}"]`)).value = mod.TwitchPlays[key] === undefined ? '' : mod.TwitchPlays[key];

        (<any>ui.querySelector(`[name="AutoPin"]`)).checked = mod.TwitchPlays && mod.TwitchPlays.AutoPin;
        (<any>ui.querySelector(`[name="Ignore"]`)).value = mod.Ignore ? mod.Ignore.join('; ') : '';
        UpdateEditUiElements();
    }
    function UpdateEditUiElements()
    {
        let ui = document.getElementById('module-ui');
        let rows = ui.querySelectorAll('tr.editable-row');
        for (let i = 0; i < rows.length; i++)
        {
            let attr = rows[i].getAttribute('data-editable-if');
            if (!attr)
                continue;
            let elem = ui.querySelector(`[name="${attr}"]`);
            var val = <string>(<any>elem).value;
            let vals = rows[i].getAttribute('data-editable-if-values').split(',');
            (<HTMLElement>rows[i]).style.display = vals.indexOf(val) === -1 ? 'none' : '';
        }
        let tables = ui.querySelectorAll('table.fields');
        for (let i = 0; i < tables.length; i++)
        {
            let attr = tables[i].getAttribute('data-nested');
            if (!attr)
                continue;
            let elem = <HTMLInputElement>document.getElementById(`nested-${attr}`);
            (<HTMLTableElement>tables[i]).style.display = elem.checked ? '' : 'none';
        }
    }
    Array.from(document.getElementById('module-ui').querySelectorAll('input,textarea,select')).forEach(elem => { (<HTMLElement>elem).onchange = UpdateEditUiElements; });
    UpdateEditUiElements();

    document.getElementById('module-json-new').onclick = function()
    {
        popup($('#tools-rel'), $('#module-ui'));
        return false;
    };
    document.getElementById('generate-json').onclick = function()
    {
        if ((<any>document.getElementById('generate-json')).form.Name.value === "")
        {
            alert("You do need to supply at least a name for the module or widget.");
            return false;
        }
    };
}