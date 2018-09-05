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

function initializePage(initModules, initIcons, initDocDirs, initDisplays, initFilters, initSelectables, souvenirAttributes)
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
    var sorts = {
        'name': { fnc: function(elem) { return $(elem).data('sortkey').toLowerCase(); }, reverse: false, bodyCss: 'sort-name', radioButton: '#sort-name' },
        'defdiff': { fnc: function(elem) { return initFilters[3].values.indexOf($(elem).data('defdiff')); }, reverse: false, bodyCss: 'sort-defdiff', radioButton: '#sort-defuser-difficulty' },
        'expdiff': { fnc: function(elem) { return initFilters[4].values.indexOf($(elem).data('expdiff')); }, reverse: false, bodyCss: 'sort-expdiff', radioButton: '#sort-expert-difficulty' },
        'twitchscore': { fnc: function(elem) { return $(elem).data('twitchscore') || 0; }, reverse: false, bodyCss: 'sort-twitch-score', radioButton: '#sort-twitch-score' },
        'published': { fnc: function(elem) { return $(elem).data('published'); }, reverse: true, bodyCss: 'sort-published', radioButton: '#sort-published' }
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

    var selectedRow = 0;
    function updateSearchHighlight()
    {
        mods.removeClass('selected').filter((_, x) => x.style.display != "none").eq(selectedRow).addClass('selected');
    }

    function setSelectable(sel)
    {
        selectable = sel;
        $('a.modlink').each(function(_, e) { $(e).attr('href', sel === 'manual' ? null : ($(e).parents('tr').data(sel) || null)); });
        $('label.set-selectable').removeClass('selected');
        $('label#selectable-label-' + sel).addClass('selected');
        $('#selectable-' + sel).prop('checked', true);
        lStorage.setItem('selectable', sel);
        updateFilter();
        setPreferredManuals();
        $('#main-table').css({ display: 'table' });
        if ($("input#search-field").is(':focus'))
            updateSearchHighlight();
    }

    function setSort(srt)
    {
        sort = srt;
        lStorage.setItem('sort', srt);
        var arr = mods.toArray();
        arr.sort(function(a, b)
        {
            var c = compare(sorts[srt].fnc(a), sorts[srt].fnc(b), sorts[srt].reverse);
            return (c === 0) ? compare($(a).data('mod'), $(b).data('mod'), false) : c;
        });

        mainTable.append(...arr);

        $(document.body).removeClass(document.body.className.split(' ').filter(cls => cls.startsWith('sort-')).join(' ')).addClass(sorts[srt].bodyCss);
        $(sorts[srt].radioButton).prop('checked', true);
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

        var searchKeywords = $("input#search-field").val().toLowerCase().split(' ').filter(x => x.length > 0);

        var modCount = 0;
        mods.each(function(_, e)
        {
            var data = $(e).data();

            var filteredIn = true;
            for (var i = 0; i < initFilters.length; i++)
            {
                if (initFilters[i].id in data)
                {
                    switch (initFilters[i].type)
                    {
                        case "slider":
                            filteredIn = filteredIn && initFilters[i].values.indexOf(data[initFilters[i].id]) >= filter[initFilters[i].id].min && initFilters[i].values.indexOf(data[initFilters[i].id]) <= filter[initFilters[i].id].max;
                            break;
                        case "checkboxes":
                            filteredIn = filteredIn && (filter[initFilters[i].id][data[initFilters[i].id]] || noneSelected[initFilters[i].id]);
                            break;
                        case "boolean":
                            filteredIn = filteredIn && (!filter[initFilters[i].id] || data[initFilters[i].id] === 'True');
                            break;
                    }
                }
            }
            var searchWhat = '';
            if (searchOptions.indexOf('names') !== -1)
                searchWhat += ' ' + data.mod.toLowerCase();
            if (searchOptions.indexOf('authors') !== -1)
                searchWhat += ' ' + data.author.toLowerCase();
            if (searchOptions.indexOf('descriptions') !== -1)
                searchWhat += ' ' + data.description.toLowerCase();
            if (filteredIn && (filter.includeMissing || selectable === 'manual' || data[selectable]) && searchKeywords.filter(x => searchWhat.indexOf(x) !== -1).length === searchKeywords.length)
            {
                modCount++;
                e.style.display = '';
            }
            else
                e.style.display = 'none';
        });

        $('#module-count').text(modCount);
        lStorage.setItem('filters', JSON.stringify(filter));
    }

    function setPreferredManuals()
    {
        var seed = $('#rule-seed-input').val() | 0;
        var seedHash = (seed === 1 ? '' : '#' + seed);
        mods.each(function(_, e)
        {
            var data = $(e).data(), i;
            if (data.manual.length === 0)
                return;

            var manual = data.manual[0];
            for (i = 0; i < data.manual.length; i++)
                if (data.manual[i].name === data.mod + " (PDF)")
                    manual = data.manual[i];
            for (i = 0; i < data.manual.length; i++)
                if (data.manual[i].name === data.mod + " (HTML)")
                    manual = data.manual[i];
            if (data.mod in preferredManuals)
                for (i = 0; i < data.manual.length; i++)
                    if (preferredManuals[data.mod] === data.manual[i].name)
                        manual = data.manual[i];
            $(e).find(selectable === 'manual' ? 'a.modlink,a.manual' : 'a.manual').attr('href', manual.url + seedHash);
            $(e).find('img.manual-icon').attr('src', manual.icon);
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
    $(document).click(disappear);

    // Click handler for selecting manuals/cheat sheets (both mobile and non)
    function makeClickHander(lnk, isMobileOpt, sheets, mod)
    {
        return function()
        {
            var already = $('.popup').filter((_, p) => $(p).data('lnk') === lnk).length;
            disappear();
            if (already)
                return false;
            var menuDiv = $('<div>').addClass('popup disappear').data('lnk', lnk);
            menuDiv.click(function() { preventDisappear++; });
            if (isMobileOpt)
            {
                menuDiv.append($('<div class="close">').click(disappear));
                var iconsDiv = $('<div>').addClass('icons');
                $(e).find('td.selectable:not(.manual) img.icon').each(function(_, ic)
                {
                    var iconDiv = $("<div class='icon'><a class='icon-link'><img class='icon-img' /><span class='icon-label'></span></a></div>");
                    iconDiv.find('a').attr('href', $(ic).parent().attr('href'));
                    iconDiv.find('img').attr('src', $(ic).attr('src'));
                    iconDiv.find('span').text($(ic).attr('title'));
                    iconsDiv.append(iconDiv);
                });
                menuDiv.append(iconsDiv);
                if ($('#display-souvenir').prop('checked'))
                    menuDiv.append($('<div class="module-further-info"></div>').text($(e).find('.inf-souvenir').attr('title')));
                if ($('#display-twitch').prop('checked'))
                    menuDiv.append($('<div class="module-further-info"></div>').text($(e).find('.inf-twitch').attr('title')));
            }
            menuDiv.append('<p class="manual-select">Select your preferred manual for this module.</p>');
            var menu = $('<menu>').addClass('manual-select');
            var seed = $('#rule-seed-input').val() | 0;
            var seedHash = (seed === 1 ? '' : '#' + seed);
            for (var i = 0; i < sheets.length; i++)
            {
                var li = $('<li>').text(sheets[i].name);
                if (mod in preferredManuals && preferredManuals[mod] === sheets[i].name)
                    li.addClass('checked');
                var ahref = $('<a>').attr('href', sheets[i].url + seedHash).append(li);
                ahref.click(function(sh)
                {
                    return function()
                    {
                        menuDiv.remove();
                        preferredManuals[mod] = sh;
                        setPreferredManuals();
                        return false;
                    };
                }(sheets[i].name));
                menu.append(ahref);
            }
            menuDiv.append(menu);
            $(document.body).append(menuDiv);
            if (!isMobileOpt)
                menuDiv.position({ my: 'right top', at: 'right bottom', of: lnk, collision: 'fit none' });
            menuDiv.css('display', 'block');
            return false;
        };
    }

    // ** GENERATE THE MAIN TABLE ** //
    for (var modIx = 0; modIx < initModules.length; modIx++)
    {
        var mod = initModules[modIx].m;
        var sheets = initModules[modIx].s.map(str => str.split('|')).map(arr => { return { name: `${mod.Name}${arr[0]} (${arr[1].toUpperCase()})`, url: `${initDocDirs[(arr[2] / 2) | 0]}/${mod.Name}${arr[0]}.${arr[1]}`, icon: initIcons[arr[2]] }; });
        var tr = el("tr", `mod${mod.TwitchPlaysSupport === 'Supported' ? ' tp' : ''}${mod.RuleSeedSupport === 'Supported' ? ' rs' : ''}`, {
            "data-mod": mod.Name,
            "data-author": mod.Author,
            "data-description": mod.Description,
            "data-sortkey": mod.SortKey,
            "data-twitchscore": mod.TwitchPlaysSpecial ? 1000 : mod.TwitchPlaysScore || -1,
            "data-published": mod.Published,
            "data-compatibility": mod.Compatibility
        });
        mainTable.appendChild(tr);
        for (var ix = 0; ix < initFilters.length; ix++)
        {
            var value = initFilters[ix].fnc(mod);
            if (value == undefined) continue;
            tr.dataset[initFilters[ix].id] = value;
        }
        for (var ix = 0; ix < initSelectables.length; ix++)
        {
            var sel = initSelectables[ix];
            var dataVal = sel.DataAttributeFunction(mod, sheets);
            if (dataVal != undefined)
            {
                if (sel.DataAttributeName != "manual")
                    tr.dataset[sel.DataAttributeName] = dataVal;
                else
                    $(tr).data(sel.DataAttributeName, dataVal);
            }
            var td = el("td", `selectable${(ix == initSelectables.length - 1 ? " last" : "")}${sel.CssClass ? " " + sel.CssClass : ""}`);
            tr.appendChild(td);
            if (sel.ShowIconFunction(mod, sheets))
                td.appendChild(el("a", sel.CssClass, { href: sel.UrlFunction(mod, sheets) }, sel.IconFunction ? sel.IconFunction(mod, sheets) : el("img", "icon", { title: sel.HumanReadable, alt: sel.HumanReadable, src: sel.Icon })));
        }

        var icon = el("img", "mod-icon", {
            alt: mod.Symbol,
            title: mod.Symbol,
            src: `Icons/${mod.Name}.png`
        });
        icon.onerror = function() { this.src = 'Icons/blank.png'; };
        var td1 = el("td", "infos-1",
            el("div", "modlink-wrap",
                el("a", "modlink",
                    icon,
                    el("span", "mod-name", mod.Name)
                )
            )
        );
        tr.appendChild(td1);

        var td2 = el("td", "infos-2");
        tr.appendChild(td2);
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
                infos.append(el("div", "inf-difficulty inf inf2",
                    el("span", "inf-difficulty-sub", readable(mod.DefuserDifficulty)),
                    ' (d), ',
                    el("span", "inf-difficulty-sub", readable(mod.ExpertDifficulty)),
                    ' (e)'));
        }
        infos.append(el("div", "inf-author inf", mod.Author),
            el("div", "inf-published inf inf2", mod.Published));
        if (mod.TwitchPlaysSupport === 'Supported')
            infos.append(
                el("div", "inf-twitch inf inf2", { title: `This module can be played in “Twitch Plays: KTANE”${mod.TwitchPlaysSpecial ? `. ${mod.TwitchPlaysSpecial}` : mod.TwitchPlaysScore ? ` for a score of ${mod.TwitchPlaysScore}.` : "."}` },
                    mod.TwitchPlaysSpecial ? 'S' : mod.TwitchPlaysScore));
        if (mod.RuleSeedSupport === 'Supported')
            infos.append(el("div", "inf-rule-seed inf inf2", { title: 'This module’s rules/manual can be dynamically varied using the Rule Seed Modifier.' }));

        var value = mod.Souvenir == null ? 'NotACandidate' : mod.Souvenir.Status;
        var attr = souvenirAttributes[value];
        var expl = mod.Souvenir && mod.Souvenir.Explanation;
        infos.append(el("div", `inf-souvenir inf inf2${expl ? " souvenir-explanation" : ""}`, { title: `${attr.Tooltip}${expl ? "\n" + expl : ""}` }, attr.Char));
        if (mod.ModuleID)
            infos.append(el("div", "inf-id inf", mod.ModuleID));
        infos.append(el("div", "inf-description inf", mod.Description));
        td1.appendChild(infos);
        td2.appendChild(infos.cloneNode(true));

        var lnk1 = el("a", "manual-selector", { href: "#" }, "▼");
        $(lnk1).click(makeClickHander(lnk1, false, $(tr).data("manual"), mod.Name));
        td1.appendChild(lnk1);

        var lnk2 = el("a", "mobile-opt", { href: "#" });
        $(lnk2).click(makeClickHander(lnk2, true, $(tr).data("manual"), mod.Name));
        tr.appendChild(el("td", "mobile-ui", lnk2));
    }

    const mods = $("tr.mod");
    const visibleMods = () => mods.filter((_, x) => x.style.display != "none");

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

    setPreferredManuals();
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
        setPreferredManuals();
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
        .blur(function() { mods.removeClass('selected'); })
        .keyup(function()
        {
            updateFilter();

            // Reducing results, move highlight
            const visModLength = visibleMods().length;
            if (selectedRow >= visModLength)
                selectedRow = visModLength - 1;

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
                    window.location.href = visibleMods().eq(selectedRow).find('a.modlink').attr("href");
                else
                {
                    // This seems to work in Firefox (it dispatches the keypress to the link), but not in Chrome. Adding .trigger(e) also doesn’t work
                    visibleMods().eq(selectedRow).find('a.modlink').focus();
                    setTimeout(function()
                    {
                        var inp = document.getElementById('search-field');
                        inp.focus();
                        inp.selectionStart = 0;
                        inp.selectionEnd = inp.value.length;
                    }, 1);
                }
            }

            updateSearchHighlight();
        });

    $('.select-on-focus').focus(function() { this.setSelectionRange(0, this.value.length); });

    //// Not currently used
    //$('#generate-pdf').click(function()
    //{
    //    $('#generate-pdf-json').val(JSON.stringify({
    //        preferredManuals: preferredManuals,
    //        sort: sort,
    //        filter: filter,
    //        selectable: selectable,
    //        searchOptions: searchOptions,
    //        search: $("input#search-field").val()
    //    }));
    //    return true;
    //});
}