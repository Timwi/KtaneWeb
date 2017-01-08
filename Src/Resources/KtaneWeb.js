$(function()
{
    var filters = ['regular', 'needy', 'vanilla', 'mods', 'nonexist'];
    var filter = {};
    try { filter = JSON.parse(localStorage.getItem('filters')) || {}; }
    catch (exc) { }
    var selectable = localStorage.getItem('selectable') || 'pdf';
    var preferredManuals = JSON.parse(localStorage.getItem('preferredManuals') || '{}');

    function setSelectable(sel)
    {
        selectable = sel;
        $('a.modlink').each(function(_, e) { $(e).attr('href', sel === 'manual' ? null : ($(e).parents('tr').data(sel) || null)); });
        $('label.set-selectable').removeClass('selected');
        $('label.set-selectable.selectable-' + sel).addClass('selected');
        $('#selectable-' + sel).prop('checked', true);
        localStorage.setItem('selectable', sel);
        updateFilter();
        setPreferredManuals();
    }

    function updateFilter()
    {
        for (var i = 0; i < filters.length; i++)
            filter[filters[i]] = $('input#filter-' + filters[i]).prop('checked');

        $('tr.mod').each(function(_, e)
        {
            var data = $(e).data();
            if (((data.type == 'Regular' && filter.regular) || (data.type == 'Needy' && filter.needy) || !(filter.regular || filter.needy)) &&
                ((data.origin == 'Vanilla' && filter.vanilla) || (data.origin == 'Mods' && filter.mods) || !(filter.vanilla || filter.mods)) &&
                (filter.nonexist || $(e).data(selectable)))
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

    for (var i = 0; i < filters.length; i++)
    {
        if (!(filters[i] in filter))
            filter[filters[i]] = true;
        $('input#filter-' + filters[i]).prop('checked', filter[filters[i]]);
    }

    // This also calls UpdateFilter()
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

    $(document).click(function()
    {
        $('.disappear').remove();
    });
});

