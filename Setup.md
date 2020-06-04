# Set up the solution for KtaneWeb

You need several other git repos for KtaneWeb to function. Execute the following script to put everything in the right place relative to each other:

```bash
mkdir Ktane
git clone https://github.com/Timwi/KtaneWeb Ktane/KtaneWeb
git clone https://github.com/Timwi/Propeller Propeller
git clone https://github.com/RT-Projects/RT.Util RT.Util
git clone https://github.com/RT-Projects/RT.SelfService RT.SelfService
git clone https://github.com/RT-Projects/RT.Servers RT.Servers
git clone https://github.com/RT-Projects/RT.TagSoup RT.TagSoup
```

This will generate a directory tree as follows:

```tree
.
├── Ktane
| └── KtaneWeb
├── Propeller
├── RT.SelfService
├── RT.Servers
├── RT.TagSoup
└── RT.Util
```

Links to the individual repos:

- KtaneWeb -- <https://github.com/Timwi/KtaneWeb>
- Propeller -- <https://github.com/Timwi/Propeller>
- RT.SelfService -- <https://github.com/RT-Projects/RT.SelfService>
- RT.Servers -- <https://github.com/RT-Projects/RT.Servers>
- RT.TagSoup -- <https://github.com/RT-Projects/RT.TagSoup>
- RT.Util -- <https://github.com/RT-Projects/RT.Util>

# Set up KtaneWeb locally

1. Make sure you have git installed **OR** you already have a clone of KtaneContent.

2. The first time you run KtaneWeb, it will provide interactive prompts to help you set up the system. Follow the instructions on screen.
