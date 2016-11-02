namespace KtaneWeb
{
    sealed partial class KtanePropellerModule 
    {
        public static string Css = @"

body {
    max-width: 60em;
    margin: 0 auto 10em;
    font-family: 'Special Elite', serif;
}

div.heading {
    margin: 3em 0 1.5em;
    overflow: auto;
}

img.logo {
    float: left;
}

div.filters {
    float: right;
    font-size: 10pt;
    text-align: left;
    margin-left: 2em;
}

div.filters .filter-section {
    margin-top: .5em;
}

div.heading div.selectables {
    float: right;
    font-size: 10pt;
    text-align: right;
}

div.head {
    font-size: 11pt;
    font-weight: bold;
    text-decoration: underline;
}

.selectables label.selected {
    font-weight: bold;
}

table {
    border-collapse: collapse;
    border: 1px solid black;
    width: 100%;
}

td, th {
    border: 1px solid black;
    vertical-align: middle;    
    padding: .3em .7em 0;
}

td.manual-icon {
    text-align: center;
}

img.icon {
    width: 25px;
}

";
    }
}
