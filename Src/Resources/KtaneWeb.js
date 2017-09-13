// Handle access to localStorage
var lStorage = localStorage;

try {
    localStorage.setItem("testStorage", "testData");
    localStorage.removeItem("testStorage");
} catch (e) {
    lStorage = {
        storage: {},
        getItem: function(key) {
            return this.storage[key] || null;
        },
        setItem: function(key, data) {
            this.storage[key] = data;
        },
        removeItem: function(key) {
            delete this.storage[key];
        },
        clear: function() {
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

$(function() {
    var filter = {};
    try { filter = JSON.parse(lStorage.getItem('filters') || '{}') || {}; }
    catch (exc) { }
    var selectable = lStorage.getItem('selectable') || 'manual';
    if (Ktane.Selectables.indexOf(selectable) === -1)
        selectable = 'manual';
    var preferredManuals = {};
    try { preferredManuals = JSON.parse(lStorage.getItem('preferredManuals') || '{}') || {}; }
    catch (exc) { }

    function compare(a, b) { return ((a < b) ? -1 : ((a > b) ? 1 : 0)); }
    var sorts = {
        'name': { fnc: function(elem) { return $(elem).data('sortkey').toLowerCase(); }, bodyCss: 'sort-name', radioButton: '#sort-name' },
        'defdiff': { fnc: function(elem) { return Ktane.Filters[3].values.indexOf($(elem).data('defdiff')); }, bodyCss: 'sort-defdiff', radioButton: '#sort-defuser-difficulty' },
        'expdiff': { fnc: function(elem) { return Ktane.Filters[4].values.indexOf($(elem).data('expdiff')); }, bodyCss: 'sort-expdiff', radioButton: '#sort-expert-difficulty' }
    };
    var sort = lStorage.getItem('sort') || 'name';
    if (!(sort in sorts))
        sort = 'name';
    var displays = ['author', 'type', 'origin', 'difficulty', 'twitch', 'id', 'description'];
    var defaultDisplay = ['author', 'type', 'difficulty', 'description'];
    var display = defaultDisplay;
    try { display = JSON.parse(lStorage.getItem('display')); } catch (exc) { }

    var version = lStorage.getItem('version');
    if (version < 2) {
        sort = 'name';
        selectable = 'manual';
        display = defaultDisplay;
        filter = {};
    }
    lStorage.setItem('version', '2');

    function setSelectable(sel) {
        selectable = sel;
        $('a.modlink').each(function(_, e) { $(e).attr('href', sel === 'manual' ? null : ($(e).parents('tr').data(sel) || null)); });
        $('label.set-selectable').removeClass('selected');
        $('label#selectable-label-' + sel).addClass('selected');
        $('#selectable-' + sel).prop('checked', true);
        lStorage.setItem('selectable', sel);
        updateFilter();
        setPreferredManuals();
    }

    function setSort(srt) {
        sort = srt;
        lStorage.setItem('sort', srt);
        var arr = $('tr.mod').toArray();
        arr.sort(function(a, b) {
            var c = compare(sorts[srt].fnc(a), sorts[srt].fnc(b));
            return (c === 0) ? compare($(a).data('mod'), $(b).data('mod')) : c;
        });
        var table = $('#main-table');
        for (var i = 0; i < arr.length; i++)
            table.append(arr[i]);

        $(document.body).removeClass('sort-name sort-defdiff sort-expdiff').addClass(sorts[srt].bodyCss);
        $(sorts[srt].radioButton).prop('checked', true);
    }

    function setDisplay(set) {
        display = (set instanceof Array) ? set.filter(function(x) { return displays.indexOf(x) !== -1; }) : defaultDisplay;
        $(document.body).removeClass(document.body.className.split(' ').filter(function(x) { return x.startsWith('display-'); }).join(' '));
        $('input.display').prop('checked', false);
        $(document.body).addClass(display.map(function(x) { return "display-" + x; }).join(' '));
        $(display.map(function(x) { return '#display-' + x; }).join(',')).prop('checked', true);
        lStorage.setItem('display', JSON.stringify(display));
    }

    function setTheme(theme) {
        if (theme === null || !(theme in Ktane.Themes)) {
            lStorage.removeItem('theme');
            theme = null;
        }
        else
            lStorage.setItem('theme', theme);
        $('#theme-css').attr('href', theme in Ktane.Themes ? Ktane.Themes[theme] : '');
        $('#theme-' + (theme || 'default')).prop('checked', true);
    }

    function updateFilter() {
        filter.includeMissing = $('input#filter-include-missing').prop('checked');

        var noneSelected = {};
        for (var i = 0; i < Ktane.Filters.length; i++) {
            var none = true;
            switch (Ktane.Filters[i].type) {
                case "slider":
                    filter[Ktane.Filters[i].id] = {
                        min: $('div#filter-' + Ktane.Filters[i].id).slider('values', 0),
                        max: $('div#filter-' + Ktane.Filters[i].id).slider('values', 1)
                    };
                    var x = function(str) { return str.replace(/[A-Z][a-z]*/g, function(m) { return " " + m.toLowerCase() }).trim(); };
                    var y = function(s1, s2) { return s1 === s2 ? x(s1) : x(s1) + ' – ' + x(s2); };
                    $('div#filter-label-' + Ktane.Filters[i].id).text(y(Ktane.Filters[i].values[filter[Ktane.Filters[i].id].min], Ktane.Filters[i].values[filter[Ktane.Filters[i].id].max]));
                    none = false;
                    break;

                case "checkboxes":
                    filter[Ktane.Filters[i].id] = {};
                    for (var j = 0; j < Ktane.Filters[i].values.length; j++) {
                        filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]] = $('input#filter-' + Ktane.Filters[i].values[j]).prop('checked');
                        if (filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]])
                            none = false;
                    }
                    break;

                case "boolean":
                    filter[Ktane.Filters[i].id] = $('input#filter-' + Ktane.Filters[i].id).prop('checked');
                    break;
            }
            noneSelected[Ktane.Filters[i].id] = none;
        }

        var searchText = $("input#search-field").val().toLowerCase();

        $('tr.mod').each(function(_, e) {
            var data = $(e).data();

            var filteredIn = true;
            for (var i = 0; i < Ktane.Filters.length; i++) {
                if (Ktane.Filters[i].id in data) {
                    switch (Ktane.Filters[i].type) {
                        case "slider":
                            filteredIn = filteredIn && Ktane.Filters[i].values.indexOf(data[Ktane.Filters[i].id]) >= filter[Ktane.Filters[i].id].min && Ktane.Filters[i].values.indexOf(data[Ktane.Filters[i].id]) <= filter[Ktane.Filters[i].id].max;
                            break;
                        case "checkboxes":
                            filteredIn = filteredIn && (filter[Ktane.Filters[i].id][data[Ktane.Filters[i].id]] || noneSelected[Ktane.Filters[i].id]);
                            break;
                        case "boolean":
                            filteredIn = filteredIn && (!filter[Ktane.Filters[i].id] || data[Ktane.Filters[i].id] === 'True');
                            break;
                    }
                }
            }
            if (filteredIn && (filter.includeMissing || selectable === 'manual' || data[selectable]) && data.mod.toLowerCase().match(searchText) !== null)
                $(e).show();
            else
                $(e).hide();
        });

        lStorage.setItem('filters', JSON.stringify(filter));
    }

    function setPreferredManuals() {
        $('tr.mod').each(function(_, e) {
            var data = $(e).data();
            if (data.manual.length == 0)
                return;

            var manual = data.manual[0];
            for (var i = 0; i < data.manual.length; i++)
                if (data.manual[i].name === data.mod + " (PDF)")
                    manual = data.manual[i];
            for (var i = 0; i < data.manual.length; i++)
                if (data.manual[i].name === data.mod + " (HTML)")
                    manual = data.manual[i];
            if (data.mod in preferredManuals)
                for (var i = 0; i < data.manual.length; i++)
                    if (preferredManuals[data.mod] === data.manual[i].name)
                        manual = data.manual[i];
            $(e).find(selectable === 'manual' ? 'a.modlink,a.manual' : 'a.manual').attr('href', manual.url);
            $(e).find('img.manual-icon').attr('src', manual.icon);
        });
        lStorage.setItem('preferredManuals', JSON.stringify(preferredManuals));
    }

    // Set filters from saved settings
    for (var i = 0; i < Ktane.Filters.length; i++) {
        switch (Ktane.Filters[i].type) {
            case "slider":
                if (!(Ktane.Filters[i].id in filter) || typeof filter[Ktane.Filters[i].id] !== 'object')
                    filter[Ktane.Filters[i].id] = {};

                if (!('min' in filter[Ktane.Filters[i].id]))
                    filter[Ktane.Filters[i].id].min = 0;
                if (!('max' in filter[Ktane.Filters[i].id]))
                    filter[Ktane.Filters[i].id].max = Ktane.Filters[i].values.length - 1;
                var e = $('div#filter-' + Ktane.Filters[i].id);
                e.slider({
                    range: true,
                    min: 0,
                    max: Ktane.Filters[i].values.length - 1,
                    values: [filter[Ktane.Filters[i].id].min, filter[Ktane.Filters[i].id].max],
                    slide: function(event, ui) { window.setTimeout(updateFilter, 1); }
                });
                break;

            case "checkboxes":
                if (!(Ktane.Filters[i].id in filter) || typeof filter[Ktane.Filters[i].id] !== 'object')
                    filter[Ktane.Filters[i].id] = {};

                for (var j = 0; j < Ktane.Filters[i].values.length; j++) {
                    if (!(Ktane.Filters[i].values[j] in filter[Ktane.Filters[i].id]))
                        filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]] = true;
                    $('input#filter-' + Ktane.Filters[i].values[j]).prop('checked', filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]]);
                }
                break;

            case "boolean":
                if (!(Ktane.Filters[i].id in filter) || typeof filter[Ktane.Filters[i].id] !== 'boolean')
                    filter[Ktane.Filters[i].id] = false;

                $('input#filter-' + Ktane.Filters[i].id).prop('checked', filter[Ktane.Filters[i].id]);
                break;
        }
    }

    // This also calls updateFilter()
    setSelectable(selectable);
    setPreferredManuals();
    setSort(sort);
    setTheme(theme);
    setDisplay(display);

    var preventDisappear = 0;
    function disappear() {
        if (preventDisappear === 0) {
            $('.disappear.stay').hide();
            $('.disappear:not(.stay)').remove();

            if ($('#more>#icons').length)
                $('#icons').insertAfter('#logo');
        }
        else
            preventDisappear--;
    }
    $(document).click(disappear);

    $('input.set-selectable').click(function() { setSelectable($(this).data('selectable')); });
    $('input.filter').click(function() { updateFilter(); });
    $("input.set-theme").click(function() { setTheme($(this).data('theme')); });
    $('input.display').click(function() { setDisplay(displays.filter(function(x) { return !$('#display-' + x).length || $('#display-' + x).prop('checked'); })); });

    $('input#search-field').on('input', function() { updateFilter(); });
    $('#search-field-clear').click(function() { $('input#search-field').val(''); updateFilter(); return false; });

    $('tr.mod').each(function(_, e) {
        var data = $(e).data();
        var mod = data.mod;
        var sheets = data.manual;

        // Click handler for selecting manuals/cheat sheets (both mobile and non)
        function makeClickHander(lnk, isMobileOpt) {
            return function() {
                disappear();
                var menuDiv = $('<div>').addClass('popup disappear');
                menuDiv.click(function() { preventDisappear++; });
                if (isMobileOpt) {
                    menuDiv.append($('<div class="close">').click(disappear));
                    var iconsDiv = $('<div>').addClass('icons');
                    $(e).find('td.selectable:not(.manual) img.icon').each(function(_, ic) {
                        var iconDiv = $("<div class='icon'><a><img class='icon' /><span></span></a></div>");
                        iconDiv.find('a').attr('href', $(ic).parent().attr('href'));
                        iconDiv.find('img').attr('src', $(ic).attr('src'));
                        iconDiv.find('span').text($(ic).attr('title'));
                        iconsDiv.append(iconDiv);
                    });
                    menuDiv.append(iconsDiv);
                }
                menuDiv.append('<p class="manual-select">Select your preferred manual for this module.</p>');
                var menu = $('<menu>').addClass('manual-select');
                for (var i = 0; i < sheets.length; i++) {
                    var li = $('<li>').text(sheets[i].name);
                    if (mod in preferredManuals && preferredManuals[mod] === sheets[i].name)
                        li.addClass('checked');
                    var ahref = $('<a>').attr('href', sheets[i].url).append(li);
                    ahref.click(function(sh) {
                        return function() {
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
                if (!isMobileOpt) {
                    var pos = $(lnk).offset();
                    menuDiv.css({ left: pos.left + $(lnk).outerWidth() - $(menuDiv).outerWidth(), top: pos.top + $(lnk).height() });
                }
                return false;
            };
        }

        // Add a copy of the .infos divs from the last column into the next-to-last (used by medium-width layout only)
        $(e).find('td.infos-1').append($('<div class="infos">').html($(e).find('td.infos-2>div.infos').html()));

        // Add UI for selecting manuals/cheat sheets (both mobile and non)
        if (sheets.length > 1) {
            var lnk1 = $('<a>').attr('href', '#').addClass('manual-selector').text('▼');
            $(e).find('td.infos-1').append(lnk1.click(makeClickHander(lnk1, false)));
        }

        var lnk2 = $(e).find('a.mobile-opt');
        lnk2.click(makeClickHander(lnk2, true));
    });

    // Page options pop-up (mobile only)
    $('#page-opt').click(function() {
        $('#icons').insertAfter('#more > div.close');
        $('#more').css({ left: '', top: '' }).show();
        return false;
    });

    $('#more-link').click(function() {
        if (!$('#more').is(':visible')) {
            $('#more').show();
            if ($(window).width() <= 650) {
                // Mobile interface: CSS does it all
                $('#more').css({ width: '', left: '', top: '' });
            } else {
                // Desktop interface: position relative to the tab
                $('#more').css({ width: '90%' });
                var pos = $('#more-tab').position();
                $('#more').css({ left: pos.left + $('#more-tab').outerWidth() - $('#more').outerWidth(), top: pos.top + $('#more-tab').outerHeight() });
            }
        }
        else
            disappear();
        return false;
    });
    $('#more>.close').click(disappear);

    // Links in the table headers (not visible on mobile UI)
    $('#sort-by-name').click(function() { setSort(sort === 'defdiff' ? 'expdiff' : sort === 'expdiff' ? 'name' : 'defdiff'); return false; });
    $('#sort-by-difficulty').click(function() { setSort(sort === 'defdiff' ? 'expdiff' : 'defdiff'); return false; });

    // Radio buttons (visible only on mobile UI)
    $('#sort-name').click(function() { setSort('name'); return true; });
    $('#sort-defuser-difficulty').click(function() { setSort('defdiff'); return true; });
    $('#sort-expert-difficulty').click(function() { setSort('expdiff'); return true; });

    $('#more').click(function() { preventDisappear++; });
});