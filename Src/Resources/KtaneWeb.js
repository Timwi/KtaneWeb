$(function()
{
    var filters = ['regular', 'needy', 'vanilla', 'mods', 'nonexist'];
    var filter = {};
    try { filter = JSON.parse(localStorage.getItem('filters')) || {}; }
    catch (exc) { }
    var selectable = localStorage.getItem('selectable') || 'pdf';
    var preferredCheatSheets = JSON.parse(localStorage.getItem('preferredCheatSheets') || '{}');

    function setSelectable(sel)
    {
        selectable = sel;
        $('a.modlink').each(function(_, e) { $(e).attr('href', $(e).parents('tr').data(sel === 'cheatsheet' ? 'pdf' : sel) || null); });
        $('label.set-selectable').removeClass('selected');
        $('label.set-selectable.selectable-' + sel).addClass('selected');
        $('#selectable-' + sel).prop('checked', true);
        localStorage.setItem('selectable', sel);
        updateFilter();
        setPreferredCheatSheets();
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

    function setPreferredCheatSheets()
    {
        $('tr.mod').each(function(_, e)
        {
            var data = $(e).data();
            if (data.cheatsheet.length == 0)
                return;

            var lnk = data.cheatsheet[0].Url;
            if (data.mod in preferredCheatSheets)
                for (var i = 0; i < data.cheatsheet.length; i++)
                    if (preferredCheatSheets[data.mod] === data.cheatsheet[i].Name)
                        lnk = data.cheatsheet[i].Url;
            $(e).find(selectable === 'cheatsheet' ? 'a.modlink,a.cheat' : 'a.cheat').attr('href', lnk);
        });
        localStorage.setItem('preferredCheatSheets', JSON.stringify(preferredCheatSheets));
    }

    for (var i = 0; i < filters.length; i++)
    {
        if (!(filters[i] in filter))
            filter[filters[i]] = true;
        $('input#filter-' + filters[i]).prop('checked', filter[filters[i]]);
    }

    // This also calls UpdateFilter()
    setSelectable(selectable);
    setPreferredCheatSheets();

    $('input.set-selectable').click(function() { setSelectable($(this).data('selectable')); return true; });
    $('input.filter').click(function() { updateFilter(); return true; });

    // Deal with the cheat sheet
    $('a.cheat').each(function(_, e)
    {
        var data = $(e).parents('tr').data();
        var mod = data.mod;
        var sheets = data.cheatsheet;
        if (sheets.length > 1)
        {
            var lnk = $('<a>').attr('href', '#').addClass('pdf-selector').text('▼').click(function()
            {
                var pos = $(lnk).position();
                var menu = $('<menu>').addClass('pdf-select disappear').css({ left: pos.left, top: pos.top + $(lnk).height() });
                for (var i = 0; i < sheets.length; i++)
                {
                    var li = $('<li>').text(sheets[i].Name);
                    if (mod in preferredCheatSheets && preferredCheatSheets[mod] === sheets[i].Name)
                        li.addClass('checked');
                    li.click(function(sh)
                    {
                        return function()
                        {
                            $(menu).remove();
                            preferredCheatSheets[mod] = sh;
                            setPreferredCheatSheets();
                            return false;
                        };
                    }(sheets[i].Name));
                    menu.append(li);
                }
                $(e).after(menu);
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

