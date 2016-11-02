using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KtaneWeb
{
    sealed partial class KtanePropellerModule
    {
        public static string JavaScript = @"

$(function()
{
    function setSelectable(sel)
    {
        $('a.modlink').each(function(_, e) {
            $(e).attr('href', $(e).data(sel) || null);
        });
        $('label.set-selectable').removeClass('selected');
        $('label.set-selectable.selectable-' + sel).addClass('selected');
        $('#selectable-' + sel).prop('checked', true);
        localStorage.setItem('selectable', sel);
    }

    function updateFilter()
    {
        var regular = $('input#filter-regular').prop('checked');
        var needy = $('input#filter-needy').prop('checked');
        var vanilla = $('input#filter-vanilla').prop('checked');
        var mods = $('input#filter-mods').prop('checked');

        $('tr.mod').each(function(_, e) {
            if ((($(e).data('type') == 'Regular' && regular) || ($(e).data('type') == 'Needy' && needy) || !(regular || needy)) &&
                (($(e).data('origin') == 'Vanilla' && vanilla) || ($(e).data('origin') == 'Mods' && mods) || !(vanilla || mods)))
                $(e).show();
            else
                $(e).hide();
        });

        localStorage.setItem('filter-regular', regular ? '' : '1');
        localStorage.setItem('filter-needy', needy ? '' : '1');
        localStorage.setItem('filter-vanilla', vanilla ? '' : '1');
        localStorage.setItem('filter-mods', mods ? '' : '1');
    }

    setSelectable(localStorage.getItem('selectable') || 'pdf');
    
    $('input#filter-regular').prop('checked', !localStorage.getItem('filter-regular'));
    $('input#filter-needy').prop('checked', !localStorage.getItem('filter-needy'));
    $('input#filter-vanilla').prop('checked', !localStorage.getItem('filter-vanilla'));
    $('input#filter-mods').prop('checked', !localStorage.getItem('filter-mods'));
    updateFilter();

    $('input.set-selectable').click(function() { setSelectable($(this).data('selectable')); return true; });
    $('input.filter').click(function() { updateFilter(); return true; });
});

";
    }
}
