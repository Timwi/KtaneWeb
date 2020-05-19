function initializePuzzles() {
    function setEvents() {
        Array.from(document.getElementsByClassName('operable')).forEach((opElem) => {
            opElem.onclick = function () {
                var data = this.dataset;
                if (!('fn' in data))
                    return false;
                if ('query' in data) {
                    // Don’t save this value in data.query directly because data.query is a DOMStringMap, so null would get converted to "null"
                    let result = prompt('Enter new value:', data.query);
                    if (result === null)
                        return false;
                    data.query = result;
                }
                var req = new XMLHttpRequest();
                req.open('POST', `api/${data.fn}`, true);
                req.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
                req.onload = function () {
                    var json = JSON.parse(req.response);
                    if (json.status === 'ok') {
                        document.getElementById('everything').innerHTML = json.result;
                        setEvents();
                    }
                    else {
                        console.log(arguments);
                        console.log(json);
                        alert("Request failed. Error logged in console.");
                    }
                };
                req.onerror = function () {
                    console.log(arguments);
                    alert("Request failed. Error logged in console.");
                };
                req.send(`data=${encodeURIComponent(JSON.stringify(data))}`);
                return false;
            };
        });
        document.getElementById('show-pristine').onclick = function () {
            Array.from(document.getElementsByClassName('req-priv')).forEach(elem => { elem.parentNode.removeChild(elem); });
            return false;
        };
    }
    setEvents();
}