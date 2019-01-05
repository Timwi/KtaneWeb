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

function el(tagName, className, ...args)
{
    const element = document.createElement(tagName);
    if (className) element.className = className;
    for (const arg of args)
    {
        if (arg instanceof window.Element)
            element.appendChild(arg);
        else if (typeof arg !== "object")
            element.appendChild(document.createTextNode(arg));
        else
            for (const attr in arg)
                if (arg[attr] != undefined)
                    element.setAttribute(attr, arg[attr]);
    }
    return element;
}

function initializePage(modules, initIcons, initDocDirs, initDisplays, initFilters, initSelectables, souvenirAttributes)
{
    var filter = {};
    try { filter = JSON.parse(lStorage.getItem('filters') || '{}') || {}; }
    catch (exc) { }
    var selectable = lStorage.getItem('selectable') || 'manual';
    if (initSelectables.map(sel => sel.DataAttributeName).indexOf(selectable) === -1)
        selectable = 'manual';
    var preferredManuals = {};
    try { preferredManuals = JSON.parse(lStorage.getItem('preferredManuals') || '{}') || {}; }
    catch (exc) { }

    function compare(a, b, rev) { return (rev ? -1 : 1) * ((a < b) ? -1 : ((a > b) ? 1 : 0)); }
    var defdiffFilterValues = initFilters.filter(f => f.id === 'defdiff')[0].values;
    var expdiffFilterValues = initFilters.filter(f => f.id === 'expdiff')[0].values;
    var sorts = {
        'name': { fnc: function(mod) { return mod.SortKey.toLowerCase(); }, reverse: false, bodyCss: 'sort-name', radioButton: '#sort-name' },
        'defdiff': { fnc: function(mod) { return defdiffFilterValues.indexOf(mod.DefuserDifficulty); }, reverse: false, bodyCss: 'sort-defdiff', radioButton: '#sort-defuser-difficulty' },
        'expdiff': { fnc: function(mod) { return expdiffFilterValues.indexOf(mod.ExpertDifficulty); }, reverse: false, bodyCss: 'sort-expdiff', radioButton: '#sort-expert-difficulty' },
        'twitchscore': { fnc: function(mod) { return mod.TwitchPlaysScore || 0; }, reverse: false, bodyCss: 'sort-twitch-score', radioButton: '#sort-twitch-score' },
        'published': { fnc: function(mod) { return mod.Published; }, reverse: true, bodyCss: 'sort-published', radioButton: '#sort-published' }
    };
    var sort = lStorage.getItem('sort') || 'name';
    if (!(sort in sorts))
        sort = 'name';
    var defaultDisplay = ['author', 'type', 'difficulty', 'description', 'published'];
    var display = defaultDisplay;
    try { display = JSON.parse(lStorage.getItem('display')) || defaultDisplay; } catch (exc) { }

    var validSearchOptions = ['names', 'authors', 'descriptions'];
    var defaultSearchOptions = ['names'];
    var searchOptions = defaultSearchOptions;
    try { searchOptions = JSON.parse(lStorage.getItem('searchOptions')) || defaultSearchOptions; } catch (exc) { }

    let profileVetoList = null;

    var version = lStorage.getItem('version');
    if (version < 2)
    {
        sort = 'name';
        selectable = 'manual';
        display = defaultDisplay;
        filter = {};
    }
    lStorage.setItem('version', '2');

    const mainTable = document.getElementById("main-table").getElementsByTagName("tbody")[0];
    const mainPeriodicTable = document.getElementById("main-periodic-table");

    var selectedRow = 0;
    function updateSearchHighlight()
    {
        var visible = $(modTrs()).removeClass('selected').filter((_, x) => x.style.display != "none");
        if (selectedRow >= visible.length)
            selectedRow = visible.length - 1;
        visible.eq(selectedRow).addClass('selected');
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
        $('#main-table').css({ display: 'table' });
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();
    }

    function setSort(srt)
    {
        sort = srt;
        lStorage.setItem('sort', srt);
        modules.sort(function(a, b)
        {
            var c = compare(sorts[srt].fnc(a), sorts[srt].fnc(b), sorts[srt].reverse);
            return (c === 0) ? compare(a.SortKey, b.SortKey, false) : c;
        });

        mainTable.append(...modTrs());

        $(document.body).removeClass(document.body.className.split(' ').filter(cls => cls.startsWith('sort-')).join(' ')).addClass(sorts[srt].bodyCss);
        $(sorts[srt].radioButton).prop('checked', true);
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();
    }

    function setDisplay(set)
    {
        display = (set instanceof Array) ? set.filter(function(x) { return initDisplays.indexOf(x) !== -1; }) : defaultDisplay;
        $(document.body).removeClass(document.body.className.split(' ').filter(function(x) { return x.startsWith('display-'); }).join(' '));
        $('input.display').prop('checked', false);
        $(document.body).addClass(display.map(function(x) { return "display-" + x; }).join(' '));
        $(display.map(function(x) { return '#display-' + x; }).join(',')).prop('checked', true);
        lStorage.setItem('display', JSON.stringify(display));
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
                const profile = JSON.parse(reader.result);
                if (profile.DisabledList)
                {
                    profileVetoList = profile.DisabledList;
                    $(".filter-profile-enabled-text").text('\u00a0Enabled by ' + file.name);
                    $(".filter-profile-disabled-text").text('\u00a0Vetoed by ' + file.name);
                    $(".profile").removeClass("none-selected");
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

    function handleDataTransfer(dataTransfer)
    {
        if (dataTransfer.files.length == 1)
        {
            setProfile(dataTransfer.files[0]);
            return false;
        } else
        {
            const url = dataTransfer.getData("text/plain");
            const handleData = data =>
            {
                const fileName = url.match(/\/(\w+\.json)$/);
                setProfile(new File([data], fileName ? fileName[1] : "Default.json"));
            };

            $.get("/proxy/" + url, handleData)
                .fail(function()
                {
                    $.get(url, handleData);
                });
        }
    }

    function updateFilter()
    {
        filter.includeMissing = $('input#filter-include-missing').prop('checked');

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

        var searchKeywords = $("input#search-field").val().toLowerCase().split(' ').filter(x => x.length > 0).map(x => x.replace(/'/g, '’'));
        const filterEnabledByProfile = $('input#filter-profile-enabled').prop('checked');
        const filterVetoedByProfile = $('input#filter-profile-disabled').prop('checked');

        var modCount = 0;
        modules.forEach(function(mod)
        {
            var filteredIn = true;
            for (var i = 0; i < initFilters.length; i++)
            {
                if (initFilters[i].id in mod)
                {
                    switch (initFilters[i].type)
                    {
                        case "slider":
                            filteredIn = filteredIn && initFilters[i].values.indexOf(mod[initFilters[i].id]) >= filter[initFilters[i].id].min && initFilters[i].values.indexOf(mod[initFilters[i].id]) <= filter[initFilters[i].id].max;
                            break;
                        case "checkboxes":
                            filteredIn = filteredIn && (filter[initFilters[i].id][mod[initFilters[i].id]] || noneSelected[initFilters[i].id]);
                            break;
                        case "boolean":
                            filteredIn = filteredIn && (!filter[initFilters[i].id] || mod[initFilters[i].id] === 'True');
                            break;
                    }
                }
            }
            if (profileVetoList !== null)
                filteredIn = filteredIn && (profileVetoList.includes(mod.ModuleID) ? (filterVetoedByProfile || !filterEnabledByProfile) : (filterEnabledByProfile || !filterVetoedByProfile));
            var searchWhat = '';
            if (searchOptions.indexOf('names') !== -1)
                searchWhat += ' ' + mod.Name.toLowerCase();
            if (searchOptions.indexOf('authors') !== -1)
                searchWhat += ' ' + mod.Author.toLowerCase();
            if (searchOptions.indexOf('descriptions') !== -1)
                searchWhat += ' ' + mod.Description.toLowerCase();
            if (filteredIn && (filter.includeMissing || selectable === 'manual' || mod[selectable]) && searchKeywords.filter(x => searchWhat.indexOf(x) !== -1).length === searchKeywords.length)
            {
                modCount++;
                mod.tr.style.display = '';
            }
            else
                mod.tr.style.display = 'none';
        });

        $('#module-count').text(modCount);
        lStorage.setItem('filters', JSON.stringify(filter));
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();
    }

    // Sets the module links to the current selectable and the manual icon link to the preferred manuals
    function setLinksAndPreferredManuals()
    {
        var seed = $('#rule-seed-input').val() | 0;
        var seedHash = (seed === 1 ? '' : '#' + seed);
        modules.forEach(function(mod)
        {
            if (mod.Manuals.length === 0)
                return;

            var manual = mod.Manuals[0], i;
            for (i = 0; i < mod.Manuals.length; i++)
                if (mod.Manuals[i].name === mod.Name + " (PDF)")
                    manual = mod.Manuals[i];
            for (i = 0; i < mod.Manuals.length; i++)
                if (mod.Manuals[i].name === mod.Name + " (HTML)")
                    manual = mod.Manuals[i];
            if (mod.Name in preferredManuals)
                for (i = 0; i < mod.Manuals.length; i++)
                    if (preferredManuals[mod.Name] === mod.Manuals[i].name)
                        manual = mod.Manuals[i];

            $(mod.tr)
                // Manual icon
                .find('img.manual-icon').attr('src', manual.icon).end()
                // Manual icon link
                .find('a.manual').attr('href', manual.url + seedHash).end()
                // Module text link
                .find('a.modlink').attr('href', selectable === 'manual' ? (manual.url + seedHash) : mod[selectable]).end()
        });
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
    $(document).click(disappear)
        .on("dragover", () => false)
        .on("drop", function(event)
        {
            event.preventDefault();
            event.stopPropagation();

            handleDataTransfer(event.originalEvent.dataTransfer);
        }).on("paste", function(event)
        {
            event.preventDefault();

            handleDataTransfer(event.originalEvent.clipboardData);
        });

    // Click handler for selecting manuals/cheat sheets (both mobile and non)
    function makeClickHander(lnk, isMobileOpt, mod)
    {
        return function()
        {
            var already = $('.popup').filter((_, p) => $(p).data('lnk') === lnk).length;
            disappear();
            if (already)
                return false;
            var menuDiv = $('<div>').addClass('popup disappear').data('lnk', lnk).css('display', 'block').appendTo(document.body);
            menuDiv.click(function() { preventDisappear++; });
            if (isMobileOpt)
            {
                menuDiv.append($('<div class="close">').click(disappear));
                var iconsDiv = $('<div>').addClass('icons');
                mod.tr.find('td.selectable:not(.manual) img.icon').each(function(_, ic)
                {
                    var iconDiv = $("<div class='icon'><a class='icon-link'><img class='icon-img' /><span class='icon-label'></span></a></div>");
                    iconDiv.find('a').attr('href', $(ic).parent().attr('href'));
                    iconDiv.find('img').attr('src', $(ic).attr('src'));
                    iconDiv.find('span').text($(ic).attr('title'));
                    iconsDiv.append(iconDiv);
                });
                menuDiv.append(iconsDiv);
                if ($('#display-souvenir').prop('checked'))
                    menuDiv.append($('<div class="module-further-info"></div>').text(mod.tr.find('.inf-souvenir').attr('title')));
                if ($('#display-twitch').prop('checked'))
                    menuDiv.append($('<div class="module-further-info"></div>').text(mod.TwitchPlaysSupport === "Supported" ? mod.tr.find('.inf-twitch').attr('title') : 'This module cannot be played in “Twitch Plays: KTANE”.'));
                if ($('#display-rule-seed').prop('checked'))
                    menuDiv.append($('<div class="module-further-info"></div>').text(mod.RuleSeedSupport === "Supported" ? mod.tr.find('.inf-rule-seed').attr('title') : 'This module does not support rule modification through Rule Seed Modifier.'));
            }
            menuDiv.append('<p class="small-print">Select your preferred manual for this module.</p>');
            var menu = $('<table>').addClass('manual-select').appendTo(menuDiv);
            var seed = $('#rule-seed-input').val() | 0;
            var seedHash = (seed === 1 ? '' : '#' + seed);
            var already = {};
            for (var i = 0; i < mod.Manuals.length; i++)
            {
                var r1 = /^\s*(.*) \((HTML|PDF)\)$/.exec(mod.Manuals[i].name.substr(mod.Name.length));
                var clickHandler = function(sh)
                {
                    return function()
                    {
                        menuDiv.remove();
                        preferredManuals[mod.Name] = sh;
                        setLinksAndPreferredManuals();
                        return false;
                    };
                }(mod.Manuals[i].name);
                var link = $(`<a href='${escape(mod.Manuals[i].url + seedHash)}'>${r1[2]}</a>`).click(clickHandler);
                if (!(r1[1] in already))
                {
                    var trow;
                    var r2 = /^translated(?: full)? \((.*) — (.*)\) (.*) \((.*)\)$/.exec(r1[1]);
                    var r3 = /^translated(?: full)? \((.*) — (.*)\)$/.exec(r1[1]);
                    var r4 = /^translated(?: full)? \((.*)\)$/.exec(r1[1]);
                    var r5 = /^(.*) \((.*)\)$/.exec(r1[1]);
                    if (r2)
                        trow = `<tr><td class='language'>${r2[1]}</td><td class='title'>${r2[2]}</td><td class='extra'><div class='descriptor'>${r2[3]}</div><div class='author'>by ${r2[4]}</div></td><td class='link-HTML'></td><td class='link-PDF'></td></tr>`;
                    else if (r3)
                        trow = `<tr><td class='language'>${r3[1]}</td><td class='title'>${r3[2]}</td><td class='extra'></td><td class='link-HTML'></td><td class='link-PDF'></td></tr>`;
                    else if (r4)
                        trow = `<tr><td class='language'>${r4[1]}</td><td class='title'>${mod.Name}</td><td class='extra'></td><td class='link-HTML'></td><td class='link-PDF'></td></tr>`;
                    else if (r5)
                        trow = `<tr><td class='language'></td><td class='title'>${mod.Name}</td><td class='extra'><div class='descriptor'>${r5[1]}</div><div class='author'>by ${r5[2]}</div></td><td class='link-HTML'></td><td class='link-PDF'></td></tr>`;
                    else
                        trow = `<tr><td class='language'></td><td class='title'>${mod.Name}</td><td class='extra'>${r1[1]}</td><td class='link-HTML'></td><td class='link-PDF'></td></tr>`;
                    already[r1[1]] = $(trow).appendTo(menu);
                    if (r1[2] === 'HTML')
                        already[r1[1]].click(clickHandler);
                }
                var link = already[r1[1]].find(`.link-${r1[2]}`).html(link).addClass('link').click(clickHandler);
                if (mod.Name in preferredManuals && preferredManuals[mod.Name] === mod.Manuals[i].name)
                    link.addClass('checked');
            }
            menuDiv.append(`<p class="small-print"><a href="find-log?find=${escape(mod.ModuleID)}">Find example logfile</a></p>`);

            if (!isMobileOpt)
                menuDiv.position({ my: 'right top', at: 'right bottom', of: lnk, collision: 'fit none' });
            return false;
        };
    }

    // ** GENERATE THE MAIN TABLE ** //
    for (var modIx = 0; modIx < modules.length; modIx++)
    {
        var mod = modules[modIx];
        mod.Manuals = mod.Sheets.map(str => str.split('|')).map(arr => { return { name: `${mod.Name}${arr[0]} (${arr[1].toUpperCase()})`, url: `${initDocDirs[(arr[2] / 2) | 0]}/${mod.Name}${arr[0]}.${arr[1]}`, icon: initIcons[arr[2]] }; });
        mod.tr = el("tr", `mod${mod.TwitchPlaysSupport === 'Supported' ? ' tp' : ''}${mod.RuleSeedSupport === 'Supported' ? ' rs' : ''}`);
        mainTable.appendChild(mod.tr);
        for (var ix = 0; ix < initFilters.length; ix++)
        {
            var value = initFilters[ix].fnc(mod);
            if (typeof value !== 'undefined')
                mod[initFilters[ix].id] = value;
        }
        for (var ix = 0; ix < initSelectables.length; ix++)
        {
            var sel = initSelectables[ix];
            var dataVal = sel.FncPropValue(mod, mod.Manuals);
            if (typeof dataVal !== 'undefined')
                mod[sel.DataAttributeName] = dataVal;
            var td = el("td", `selectable${(ix == initSelectables.length - 1 ? " last" : "")}${sel.CssClass ? " " + sel.CssClass : ""}`);
            mod.tr.appendChild(td);
            if (sel.ShowIconFunction(mod, mod.Manuals))
                td.appendChild(el("a", sel.CssClass, { href: sel.UrlFunction(mod, mod.Manuals) }, sel.IconFunction ? sel.IconFunction(mod, mod.Manuals) : el("img", "icon", { title: sel.HumanReadable, alt: sel.HumanReadable, src: sel.Icon })));
        }

        var icon = el("img", "mod-icon", { title: mod.Symbol, src: `Icons/${mod.Name}.png` });
        icon.onerror = function() { this.src = 'Icons/blank.png'; };
        var td1 = el("td", "infos-1",
            el("div", "modlink-wrap",
                el("a", "modlink",
                    icon,
                    el("span", "mod-name", mod.Name)
                )
            )
        );
        mod.tr.appendChild(td1);

        var td2 = el("td", "infos-2");
        mod.tr.appendChild(td2);
        var infos = el("div", "infos",
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
        if (mod.TwitchPlaysSupport === 'Supported')
            infos.append(
                el("div", "inf-twitch inf inf2", { title: `This module can be played in “Twitch Plays: KTANE”${mod.TwitchPlaysSpecial ? `. ${mod.TwitchPlaysSpecial}` : mod.TwitchPlaysScore ? ` for a score of ${mod.TwitchPlaysScore}.` : "."}` },
                    mod.TwitchPlaysSpecial ? 'S' : mod.TwitchPlaysScore === undefined ? '' : mod.TwitchPlaysScore));
        if (mod.RuleSeedSupport === 'Supported')
            infos.append(el("div", "inf-rule-seed inf inf2", { title: 'This module’s rules/manual can be dynamically varied using the Rule Seed Modifier.' }));

        var value = !('Souvenir' in mod) || mod.Souvenir === null || !('Status' in mod.Souvenir) ? 'Unexamined' : mod.Souvenir.Status;
        var attr = souvenirAttributes[value];
        var expl = mod.Souvenir && mod.Souvenir.Explanation;
        infos.append(el("div", `inf-souvenir inf inf2${expl ? " souvenir-explanation" : ""}`, { title: `${attr.Tooltip}${expl ? "\n" + expl : ""}` }, attr.Char));
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
        mod.tr.appendChild(el("td", "mobile-ui", lnk2));
    }

    function modTrs() { return modules.map(mod => mod.tr); }
    function visibleMods() { return modTrs().filter(x => x.style.display != "none"); }

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

        $('input#filter-include-missing').prop('checked', filter.includeMissing);
    }

    setLinksAndPreferredManuals();
    setSort(sort);
    setTheme(theme);
    setDisplay(display);
    setSearchOptions(searchOptions);

    // This also calls updateFilter()
    setSelectable(selectable);

    $('input.set-selectable').click(function() { setSelectable($(this).data('selectable')); });
    $('input.filter').click(function() { updateFilter(); });
    $("input.set-theme").click(function() { setTheme($(this).data('theme')); });
    $('input.display').click(function() { setDisplay(initDisplays.filter(function(x) { return !$('#display-' + x).length || $('#display-' + x).prop('checked'); })); });
    $('input#profile-file').change(function() { const files = document.getElementById('profile-file').files; if (files.length === 1) { setProfile(files[0]); } });
    $('#search-field-clear').click(function() { disappear(); $('input#search-field').val(''); updateFilter(); return false; });
    $('input.search-option-input').click(function() { setSearchOptions(validSearchOptions.filter(function(x) { return !$('#search-' + x).length || $('#search-' + x).prop('checked'); })); updateFilter(); });

    // Page options pop-up (mobile only)
    $('#page-opt').click(function()
    {
        $('#icons').insertAfter('#page-opt-popup>div.close');
        $('#page-opt-popup').show();
        return false;
    });

    function popup(lnk, wnd, width)
    {
        if (!wnd.is(':visible'))
        {
            disappear();
            wnd.show();
            if (window.innerWidth <= 650)
            {
                // Mobile interface: CSS does it all
                wnd.css({ width: '', left: '', top: '' });
            } else
            {
                // Desktop interface: position relative to the tab
                wnd.css({ width: width || wnd.data('width') }).position({ my: 'right top', at: 'right bottom', of: lnk, collision: 'fit none' });
            }
        }
        else
            disappear();
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
        Array.from($pp.find('.focus-on-show').focus()).forEach(x => x.select());
        return false;
    });

    $('#rule-seed-input').on('change', function()
    {
        setLinksAndPreferredManuals();
        var seed = $('#rule-seed-input').val() | 0;
        if (seed === 1)
        {
            document.body.classList.remove('rule-seed-active');
            document.getElementById('rule-seed-number').innerText = '';
        }
        else
        {
            document.body.classList.add('rule-seed-active');
            document.getElementById('rule-seed-number').innerText = ' = ' + seed;
            document.getElementById('rule-seed-mobile').innerText = seed;
        }
    });

    // Links in the table headers (not visible on mobile UI)
    $('.sort-header').click(function()
    {
        var arr = Object.keys(sorts);
        var ix = -1;
        for (var i = 0; i < arr.length; i++)
            if (arr[i] === sort)
                ix = i;
        ix = (ix + 1) % arr.length;
        setSort(arr[ix]);
        return false;
    });

    // Radio buttons (in “Filters”)
    $('input.sort').click(function() { setSort(this.value); return true; });
    $('.popup').click(function() { preventDisappear++; });
    $('.popup>.close').click(disappear);

    $("#search-field")
        .focus(updateSearchHighlight)
        .blur(function() { $(modTrs()).removeClass('selected'); })
        .keyup(function(e)
        {
            if (e.keyCode === 38 || e.keyCode === 40 || e.keyCode == 13)   // up/down arrows, enter
                return;
            updateFilter();
            updateSearchHighlight();
        })
        .keydown(function(e)
        {
            if (e.keyCode === 38 && selectedRow > 0)   // up arrow
                selectedRow--;
            else if (e.keyCode === 40 && selectedRow < visibleMods().length - 1)      // down arrow
                selectedRow++;
            else if (e.keyCode === 13)
            {
                if (!e.originalEvent.ctrlKey && !e.originalEvent.shiftKey && !e.originalEvent.altKey)  // enter
                    window.location.href = $(visibleMods()[selectedRow]).find('a.modlink').attr("href");
                else
                {
                    // This seems to work in Firefox (it dispatches the keypress to the link), but not in Chrome. Adding .trigger(e) also doesn’t work
                    $(visibleMods()[selectedRow]).find('a.modlink').focus();
                    setTimeout(function()
                    {
                        var inp = document.getElementById('search-field');
                        inp.focus();
                        inp.setSelectionRange(0, inp.value.length);
                    }, 1);
                }
            }

            updateSearchHighlight();
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
            filterVetoedByProfile: $('input#filter-profile-disabled').prop('checked')
        }));
        return true;
    });
}