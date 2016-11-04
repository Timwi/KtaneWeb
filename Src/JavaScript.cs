namespace KtaneWeb
{
    sealed partial class KtanePropellerModule
    {
        public static string JavaScript = @"

$(function()
{
    var filters = [ 'regular', 'needy', 'vanilla', 'mods', 'nonexist' ];
    var filter = {};
    try { filter = JSON.parse(localStorage.getItem('filters')) || {}; }
    catch (exc) { }
    var selectable = localStorage.getItem('selectable') || 'pdf';

    function setSelectable(sel)
    {
        selectable = sel;
        $('a.modlink').each(function(_, e) {
            $(e).attr('href', $(e).data(sel) || null);
        });
        $('label.set-selectable').removeClass('selected');
        $('label.set-selectable.selectable-' + sel).addClass('selected');
        $('#selectable-' + sel).prop('checked', true);
        localStorage.setItem('selectable', sel);
        updateFilter();
    }

    function updateFilter()
    {
        for (var i = 0; i < filters.length; i++)
            filter[filters[i]] = $('input#filter-' + filters[i]).prop('checked');

        $('tr.mod').each(function(_, e) {
            if ((($(e).data('type') == 'Regular' && filter.regular) || ($(e).data('type') == 'Needy' && filter.needy) || !(filter.regular || filter.needy)) &&
                (($(e).data('origin') == 'Vanilla' && filter.vanilla) || ($(e).data('origin') == 'Mods' && filter.mods) || !(filter.vanilla || filter.mods)) &&
                (filter.nonexist || $(e).find('a.modlink').data(selectable)))
                $(e).show();
            else
                $(e).hide();
        });

        localStorage.setItem('filters', JSON.stringify(filter));
    }

    for (var i = 0; i < filters.length; i++)
    {
        if (!(filters[i] in filter))
            filter[filters[i]] = true;
        $('input#filter-' + filters[i]).prop('checked', filter[filters[i]]);
    }

    // This also calls UpdateFilter()
    setSelectable(selectable);

    $('input.set-selectable').click(function() { setSelectable($(this).data('selectable')); return true; });
    $('input.filter').click(function() { updateFilter(); return true; });
});

";
    }
}
