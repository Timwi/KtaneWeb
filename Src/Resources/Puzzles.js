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

$(function()
{
    function setEvents()
    {
        $('.operable').click(function()
        {
            var data = this.dataset;
            if (!('fn' in data))
                return false;
            if ('query' in data)
            {
                data.query = prompt('Enter new value:', data.query);
                if (data.query === null)
                    return false;
            }

            $.post('api/' + data.fn, { data: JSON.stringify(data) }, function(resp)
            {
                $('#everything').html(resp.result);
                setEvents();
            })
                .fail(function()
                {
                    console.log(arguments);
                    alert("Request failed. Error logged in console.");
                });
            return false;
        });

        $('#show-pristine').click(function()
        {
            $('.req-priv').remove();
            return false;
        });
    }
    setEvents();
});
