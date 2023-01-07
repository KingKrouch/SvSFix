# SvSFix
Community Improvement Patch for Neptunia Sisters vs Sisters

## Features
- Fixes for stuttering gameplay logic _(Particularly Player and Enemy movement functions that use FixedUpdate incorrectly)._
- Support for arbitrary resolutions and aspect ratios _(Including UI and FOV tweaks for optimal ultrawide and 16:10 support)._
- Improved scalability options and graphical improvement tweaks.
- Improved PlayStation and Nintendo Switch controller support _(Hooks into SteamInput with Unity's standard input systems as a fallback)_.
- Photo Mode improvements.
- Keyboard & Mouse input improvements.
- Custom framelimiter that operates on factors of the display refresh rate _(Full, Half, 1/3rd, Quarter)_. **This will have less consequences for input latency (compared to VSync intervals higher than 1), while not being as crappy for arbitrary refresh rates as hardcoded 30/60/120 limits.**


## Installation
- Grab the latest release of SvSFix from [here](https://github.com/KingKrouch/SvSFix/releases).
- Extract the .zip archive's contents into the game directory.<br />(e.g. "**steamapps\common\Neptunia Sisters VS Sisters**" for Steam).
- To adjust any settings open the config file located in **\BepInEx\Config\SvSFix.cfg**

## Linux and Steam Deck Notes
If you are playing on Linux or the Steam Deck, you will need to adjust the game's launch options through the game properties on Steam.

You will need to append this to the beginning of the game's launch options before playing: ```WINEDLLOVERRIDES="winhttp=n,b" %command%```

### Special Thanks To:
[Lyall](https://github.com/Lyall) and [PhantomGamers](https://github.com/PhantomGamers) for advice relating to BepInEx and Unity modding.

## Licensing
- [BepinEx](https://github.com/BepInEx/BepInEx) is licensed under the GNU Lesser General Public License v2.1.

**SvSFix (c) 2023 Bryce Q.**

**Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:**

**The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.**

**THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.**

**[See the MIT License for more details.](https://github.com/KingKrouch/SvSFix/blob/main/LICENSE)**