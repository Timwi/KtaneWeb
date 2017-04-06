$(function()
{
    var filter = {};
    try { filter = JSON.parse(localStorage.getItem('filters') || '{}') || {}; }
    catch (exc) { }
    var selectable = localStorage.getItem('selectable') || 'manual';
    if (Ktane.Selectables.indexOf(selectable) === -1)
        selectable = 'manual';
    var preferredManuals = {};
    try { preferredManuals = JSON.parse(localStorage.getItem('preferredManuals') || '{}') || {}; }
    catch (exc) { }

    function compare(a, b) { return ((a < b) ? -1 : ((a > b) ? 1 : 0)); }
    var sorts = {
        'name': { fnc: function(elem) { return $(elem).data('mod'); }, indicate: function() { $('#sort-ind-name').show().text('sorted by name'); } },
        'defdiff': { fnc: function(elem) { return Ktane.Filters[2].values.indexOf($(elem).data('defdiff')); }, indicate: function() { $('#sort-ind-difficulty').show().text('sorted by defuser difficulty'); } },
        'expdiff': { fnc: function(elem) { return Ktane.Filters[3].values.indexOf($(elem).data('expdiff')); }, indicate: function() { $('#sort-ind-difficulty').show().text('sorted by expert difficulty'); } }
    };
    var sort = localStorage.getItem('sort') || 'name';
    if (!(sort in sorts))
        sort = 'name';

    function setSelectable(sel)
    {
        selectable = sel;
        $('a.modlink').each(function(_, e) { $(e).attr('href', sel === 'manual' ? null : ($(e).parents('tr').data(sel) || null)); });
        $('label.set-selectable').removeClass('selected');
        $('label#selectable-label-' + sel).addClass('selected');
        $('#selectable-' + sel).prop('checked', true);
        localStorage.setItem('selectable', sel);
        updateFilter();
        setPreferredManuals();
    }

    function setSort(srt)
    {
        sort = srt;
        localStorage.setItem('sort', srt);
        var arr = $('tr.mod').toArray();
        arr.sort(function(a, b)
        {
            var c = compare(sorts[srt].fnc(a), sorts[srt].fnc(b));
            return (c === 0) ? compare($(a).data('mod'), $(b).data('mod')) : c;
        });
        var table = $('#main-table');
        for (var i = 0; i < arr.length; i++)
            table.append(arr[i]);

        $('.sort-ind').hide();
        sorts[srt].indicate();
    }

    function updateFilter()
    {
        filter.showMissing = $('input#filter-show-missing').prop('checked');

        var noneSelected = {};
        for (var i = 0; i < Ktane.Filters.length; i++)
        {
            var none = true;
            switch (Ktane.Filters[i].type)
            {
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
                    for (var j = 0; j < Ktane.Filters[i].values.length; j++)
                    {
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

        $('tr.mod').each(function(_, e)
        {
            var data = $(e).data();

            var filteredIn = true;
            for (var i = 0; i < Ktane.Filters.length; i++)
            {
                switch (Ktane.Filters[i].type)
                {
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
            if (filteredIn && (filter.showMissing || selectable === 'manual' || $(e).data(selectable)))
                $(e).show();
            else
                $(e).hide();
        });

        localStorage.setItem('filters', JSON.stringify(filter));
    }

    function setPreferredManuals()
    {
        $('tr.mod').each(function(_, e)
        {
            var data = $(e).data();
            if (data.manual.length == 0)
                return;

            var manual = data.manual[0];
            if (data.mod in preferredManuals)
                for (var i = 0; i < data.manual.length; i++)
                    if (preferredManuals[data.mod] === data.manual[i].name)
                        manual = data.manual[i];
            $(e).find(selectable === 'manual' ? 'a.modlink,a.manual' : 'a.manual').attr('href', manual.url);
            $(e).find('img.manual-icon').attr('src', manual.icon);
        });
        localStorage.setItem('preferredManuals', JSON.stringify(preferredManuals));
    }

    function resize()
    {
        if ($(document).width() <= 1024)
        {
            // condensed layout, designed for mobile
        }
        else
        {
            // full layout, designed for desktop
        }
    }

    // Set filters from saved settings
    for (var i = 0; i < Ktane.Filters.length; i++)
    {
        switch (Ktane.Filters[i].type)
        {
            case "slider":
                if (!(Ktane.Filters[i].id in filter))
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
                if (!(Ktane.Filters[i].id in filter))
                    filter[Ktane.Filters[i].id] = {};

                for (var j = 0; j < Ktane.Filters[i].values.length; j++)
                {
                    if (!(Ktane.Filters[i].values[j] in filter[Ktane.Filters[i].id]))
                        filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]] = true;
                    $('input#filter-' + Ktane.Filters[i].values[j]).prop('checked', filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]]);
                }
                break;

            case "boolean":
                if (!(Ktane.Filters[i].id in filter))
                    filter[Ktane.Filters[i].id] = false;

                $('input#filter-' + Ktane.Filters[i].id).prop('checked', filter[Ktane.Filters[i].id]);
                break;
        }
    }

    // This also calls updateFilter()
    setSelectable(selectable);
    setPreferredManuals();
    setSort(sort);

    $('input.set-selectable').click(function() { setSelectable($(this).data('selectable')); return true; });
    $('input.filter').click(function() { updateFilter(); return true; });

    // Deal with the manuals
    $('a.manual').each(function(_, e)
    {
        var data = $(e).parents('tr').data();
        var mod = data.mod;
        var sheets = data.manual;
        if (sheets.length > 1)
        {
            var lnk = $('<a>').attr('href', '#').addClass('manual-selector').text('▼').click(function()
            {
                $('.disappear').remove();
                var pos = $(lnk).position();
                var menu = $('<menu>');
                var menuDiv = $('<div>').addClass('manual-select disappear').css({ left: pos.left, top: pos.top + $(lnk).height() }).append('<p>Select your preferred manual for this module.</p>').append(menu);
                for (var i = 0; i < sheets.length; i++)
                {
                    var li = $('<li>').text(sheets[i].name);
                    if (mod in preferredManuals && preferredManuals[mod] === sheets[i].name)
                        li.addClass('checked');
                    li.click(function(sh)
                    {
                        return function()
                        {
                            menuDiv.remove();
                            preferredManuals[mod] = sh;
                            setPreferredManuals();
                            return false;
                        };
                    }(sheets[i].name));
                    menu.append(li);
                }
                $(e).after(menuDiv);
                return false;
            });
            $(e).after(lnk);
        }
    });

    $('#sort-by-name').click(function() { setSort('name'); return false; });
    $('#sort-by-difficulty').click(function() { setSort(sort === 'defdiff' ? 'expdiff' : 'defdiff'); return false; });

    $(document).click(function() { $('.disappear').remove(); });
    $(window).resize(function() { resize(); });

});

