# How to set up the solution for KtaneWeb

```tree
.
|-- Ktane
| `-- KtaneWeb
|-- Propeller
|-- RT.SelfService
|-- RT.Servers
|-- RT.TagSoup
`-- RT.Util
  |-- RT.Json
  |-- RT.PostBuild
  |-- RT.Serialization
  |-- RT.Serialization.Binary
  |-- RT.Serialization.Json
  |-- RT.Serialization.Xml
  |-- RT.Util
  |-- RT.Util.Core
  `-- RT.Util.Legacy
```

- ktaneweb -- <https://github.com/Timwi/KtaneWeb>
- propeller -- <https://github.com/Timwi/Propeller>
- rt.util -- <https://github.com/RT-Projects/RT.Util>
- rt.selfservice -- <https://github.com/RT-Projects/RT.SelfService>
- rt.servers -- <https://github.com/RT-Projects/RT.Servers>
- rt.tagsoup -- <https://github.com/RT-Projects/RT.TagSoup>

The following script will clone all the repos for you. Just create a cmd/bash script in the root folder of the tree above and execute it.

```
mkdir Ktane
git clone https://github.com/Timwi/KtaneWeb Ktane/KtaneWeb
git clone https://github.com/Timwi/Propeller Propeller
git clone https://github.com/RT-Projects/RT.Util RT.Util
git clone https://github.com/RT-Projects/RT.SelfService RT.SelfService
git clone https://github.com/RT-Projects/RT.Servers RT.Servers
git clone https://github.com/RT-Projects/RT.TagSoup RT.TagSoup
```