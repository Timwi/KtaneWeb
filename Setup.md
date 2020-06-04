# How to set up KtaneWeb locally

## 1. Clone the KtaneContent repository

This repository contains all the HTML manuals and many other files. To clone KtaneContent on the command line, run the following command but replace the path to whatever actual path you would like to use:

```bash
git clone https://github.com/Timwi/KtaneContent.git C:\\Path\\KtaneContent
```

This will generate a folder at the specified location containing all the content.

Next, we will need to deal with two configuration files before the site can run. The first one is for the web server system (Propeller) and the second one is for KtaneWeb itself.

## 2. Propeller config file (`KTANE-Propeller-standalone.json`)

The first time you run KtaneWeb, it will crash with an exception but it will generate this file in the build output directory with the executable. (Note this means there will be a separate such file for DEBUG and RELEASE mode.) Open this file in a text editor. In the `"Settings"` option, add a reference to the KtaneWeb config file that we will be dealing with in the following step. Choose any path where you would like to put the file. The following is just an example.

```json
{
  "Settings": {
    ":value": { "ConfigFile": "C:\\SomePath\\KtaneWeb.config.json" }
  }
}
```

---

## 3. KtaneWeb config file (`KtaneWeb.config.json`)

Create this JSON file at the path you defined above in the previous JSON file. Fill that file as follows, but make the following replacements:

* Replace `%KtaneContent%` with the path where you cloned the KtaneContent KtaneWeb in Step 1 above.
* Replace `%KtaneWeb%` with the path where KtaneWeb’s source is on your machine.
* Also decide on the other paths marked below with `!DECIDE!`. `LogfilesDir` is where logfiles uploaded through the LFA go. `MergedPdfsDir` is where merged PDFs go. `PdfTempPath` is optional; if left at `null` it will use your Windows default Temp folder.

```json
{
  "BaseDir": "%KtaneContent%",
  "ChromePath": "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe",
  "CssFile": "%KtaneWeb%\\Src\\Resources\\KtaneWeb.css",
  "JavaScriptFile": "%KtaneWeb%\\Src\\Resources\\KtaneWeb.js",
  "LogfilesDir": "!DECIDE!",
  "MergedPdfsDir": "!DECIDE!",
  "ModIconDir": "%KtaneContent%\\Icons",
  "ModJsonDir": "%KtaneContent%\\JSON",
  "PdfTempPath": null
}
```

(The `CssFile` and `JavascriptFile` paths are only used in `DEBUG` mode to allow you to edit those files while the software is running. In `RELEASE` mode, those values are ignored and the CSS/JS files are baked into the EXE.)

After you run KtaneWeb, the above file will expand to contain all of the settings/options available. If you’re curious, feel free to take a look through it.
