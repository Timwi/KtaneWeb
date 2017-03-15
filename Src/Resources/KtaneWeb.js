$(function()
{
    //var filters = ['regular', 'needy', 'vanilla', 'mods', 'showMissing'];
    // TODO ‘showMissing’ filter

    var filter = {};
    try { filter = JSON.parse(localStorage.getItem('filters')) || {}; }
    catch (exc) { }
    var selectable = localStorage.getItem('selectable') || 'manual';
    if (Ktane.Selectables.indexOf(selectable) === -1)
        selectable = 'manual';
    var preferredManuals = JSON.parse(localStorage.getItem('preferredManuals') || '{}');

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

    function updateFilter()
    {
        filter.showMissing = $('input#filter-show-missing').prop('checked');

        var noneSelected = {};
        for (var i = 0; i < Ktane.Filters.length; i++)
        {
            var none = true;
            filter[Ktane.Filters[i].id] = {};
            for (var j = 0; j < Ktane.Filters[i].values.length; j++)
            {
                filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]] = $('input#filter-' + Ktane.Filters[i].values[j]).prop('checked');
                if (filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]])
                    none = false;
            }
            noneSelected[Ktane.Filters[i].id] = none;
        }

        $('tr.mod').each(function(_, e)
        {
            var data = $(e).data();

            var filteredIn = true;
            for (var i = 0; i < Ktane.Filters.length; i++)
                filteredIn = filteredIn && (filter[Ktane.Filters[i].id][data[Ktane.Filters[i].id]] || noneSelected[Ktane.Filters[i].id] || Ktane.Filters[i].alwaysShow.indexOf(data[Ktane.Filters[i].id]) !== -1);

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

    for (var i = 0; i < Ktane.Filters.length; i++)
    {
        if (!(Ktane.Filters[i].id in filter))
            filter[Ktane.Filters[i].id] = {};
        for (var j = 0; j < Ktane.Filters[i].values.length; j++)
        {
            if (!(Ktane.Filters[i].values[j] in filter[Ktane.Filters[i].id]))
                filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]] = true;
            $('input#filter-' + Ktane.Filters[i].values[j]).prop('checked', filter[Ktane.Filters[i].id][Ktane.Filters[i].values[j]]);
        }
    }

    // This also calls updateFilter()
    setSelectable(selectable);
    setPreferredManuals();

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

    $(document).click(function() { $('.disappear').remove(); });
    $(window).resize(function() { resize(); });

});

