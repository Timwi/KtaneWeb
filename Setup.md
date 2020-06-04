# How to setup KtaneWeb

## Propeller config file (`KTANE-Propeller-standalone.json`)

This file is generated in the build output directory with the executable. The executable will probably crash as it can't read the `KtaneWeb` config file which is defined in this file.

There are a few settings which need to be setup

Add the http endpoint

```json
{
  "endpoints": {
    "HTTP": {
      "BindAddress": null,
      "Port": 8990,
      "Secure": false
    }
  }
}
```

Add the KtaneWeb config file's path

```json
{
  "Settings": {
    ":value": { "ConfigFile": "%site%\\KtaneWeb.config.json" }
  }
}
```

Change the line starting with `"Hooks":` to `"Hooks": [{ "Protocols":" All" }],` if you want to be able to load the page on any address other than localhost.

---

## KtaneWeb config file (`KtaneWeb.config.json`)

This path is the same as the settings value defined above and will auto-generate most required information. The path `%site%` can be any path which contains the KtaneContent repo as explained below. The path `%repo%` is the path for the KtaneWeb repo on your machine.

The `CssFile` and `JavascriptFile` paths only need to be defined if it is run in `DEBUG` mode and otherwise can be left as `null`.

```json
{
  "BaseDir": "%site%\\Public",
  "ChromePath": "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe",
  "CssFile": "%repo%\\Src\\Resources\\KtaneWeb.css",
  "DocumentDirs": ["HTML", "PDF"],
  "ExtraDocumentIcons": [
    "HTML/img/html_manual_embellished.png",
    "HTML/img/pdf_manual_embellished.png"
  ],
  "JavaScriptFile": "%repo%\\Src\\Resources\\KtaneWeb.js",
  "LogfilesDir": "%site%\\Logfiles",
  "MergedPdfsDir": "%site%\\MergedPdfs",
  "ModIconDir": "%site%\\Public\\Icons",
  "ModJsonDir": "%site%\\Public\\JSON",
  "OriginalDocumentIcons": [
    "HTML/img/html_manual.png",
    "HTML/img/pdf_manual.png"
  ],
  "PdfDir": "PDF",
  "PdfTempPath": null,
  "Puzzles": {
    "EditAccess": ["Timwi"],
    "PuzzleGroups": []
  },
  "Sessions": {},
  "UsersFile": null
}
```

## KtaneContent public directory

The `BaseDir` path defines where the `KtaneContent` repo lives. To clone the repo in the correct place run the following command making sure to replace `%clonepath%` with the path defined in `BaseDir`.
```bash
git clone https://github.com/Timwi/KtaneContent.git %clonepath%
```