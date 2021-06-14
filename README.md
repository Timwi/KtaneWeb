# Set up the solution for KtaneWeb

KtaneWeb depends on a number of Nuget packages. Visual Studio handles this automatically when you try to compile the project; otherwise you may need to run a “Nuget restore” first.

Here are some links to the individual repos. You do not need to clone these unless your contributions requires a change to any of those.

- KtaneWeb -- <https://github.com/Timwi/KtaneWeb> (that’s this project)
- Propeller -- <https://github.com/Timwi/Propeller> (contains PropellerApi)
- RT.Servers -- <https://github.com/RT-Projects/RT.Servers> (contains HttpServer)
- RT.TagSoup -- <https://github.com/RT-Projects/RT.TagSoup> (an HTML library)
- RT.Util -- <https://github.com/RT-Projects/RT.Util> (lots of utilities, including extension methods)
- PdfSharp -- <https://github.com/empira/PDFsharp> (used only by the “Generate merged PDF” feature)

# Set up KtaneWeb locally

1. Make sure you have git installed **OR** you already have a clone of [KtaneContent](https://github.com/Timwi/KtaneContent).

2. The first time you run KtaneWeb, it will provide interactive prompts to help you set up the system. Follow the instructions on screen.
