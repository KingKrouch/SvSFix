# SvSFix
[![Github All Releases](https://img.shields.io/github/downloads/KingKrouch/SvSFix/total.svg)]()
<br>Community Improvement Patch for the PC version of Neptunia Sisters vs Sisters.

## Features:
| _Features and Changes the Mod Introduces (Or Plans to Add)_                                                                                                                                                                                                                            | _Functional (Marked off with ✓ and ✘) / WIP / Experimental_                                                                                  |
|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|
| Fixes for Stuttering Gameplay Logic<br>(Player and Enemy Movement Functions that use FixedUpdate)                                                                                                                                                                                      | ✓ DeltaTime Fix <br> ✘ FixedUpdate Interpolation Fix _(WIP)_                                                                                 |
| Support for arbitrary resolutions and aspect ratios<br>_(Including UI and FOV tweaks for optimal ultrawide and 16:10 support)._                                                                                                                                                        | ✓ <br> _(Some UI Tweaks are Experimental, and In-Game resolution and HUD Scaling options will be coming soon)._                              |
| Improved scalability options and graphical improvement tweaks.                                                                                                                                                                                                                         | ✓                                                                                                                                            |
| Steam Deck and GPU Utilization Tweaks (For Framerate and Fan Noise Improvements)                                                                                                                                                                                                       | ✓ _(WIP)_                                                                                                                                    |
| Improved PlayStation and Nintendo Switch controller support _(With Prompts)_.<br>_(SteamInput Integration + Unity's Input System available as fallback)_.                                                                                                                              | ✓ Native DualShock and DualSense Support <br> ✓ SteamInput Support _(Experimental)_ <br> ✘ Native Nintendo Switch Controller Support _(WIP)_ |
| Option for Japanese-Style Controller Navigation in Menus<br>_(◯ = Confirm, ✖ = Cancel)_.                                                                                                                                                                                               | _✓ Optional for PlayStation Controllers. <br> ✓ Always On for Nintendo Switch Controllers. <br> ✘ Always Off for Xbox Controllers._          |
| Option to Skip Opening Logos and Video Automatically.                                                                                                                                                                                                                                  | ✓ _(WIP)_                                                                                                                                    |
| Photo Mode Improvements.                                                                                                                                                                                                                                                               | ✓                                                                                                                                            |
| Keyboard & Mouse Input Improvements.                                                                                                                                                                                                                                                   | ✘ _(WIP)_                                                                                                                                    |
| Custom Framelimiter Operating on Display Refresh Rate Factors _(Full, Half, 1/3rd, Quarter)_.<br> **This will have less consequences for input latency (compared to VSync intervals higher than 1),<br>while not being as crappy for non-VRR displays as hardcoded 30/60/120 limits.** | ✓ _(Experimental)_                                                                                                                           |T

**NOTE:** The plan is to have all of these features implemented to my own personal satisfaction by the time a stable 1.0 non-alpha release happens.
<br>I can't say whether or not some of these features will make the cut for when I plan to port the changes in this mod to the IL2CPP version of the game.

## Installation Instructions:
- Grab the latest release of SvSFix from [here](https://github.com/KingKrouch/SvSFix/releases) alongside with the mono runtimes for the game.
- Extract the .zip archive's contents into the game directory.<br />(e.g. "**steamapps\common\Neptunia Sisters VS Sisters**" for Steam).
- To adjust any settings open the config file located in **\BepInEx\Config\SvSFix.cfg**

## Compilation Notes (For Content AssetBundle):
If you want to do any content related modifications, or to use Unity's performance profiling tools, make sure to install Unity 2021.2.5f1 through [Unity's Download Archive](https://unity.com/releases/editor/archive).

## Linux and Steam Deck Notes:
If you are playing on Linux or the Steam Deck, you will need to adjust the game's launch options through the game properties on Steam.

You will need to append this to the beginning of the game's launch options before playing: ```WINEDLLOVERRIDES="winhttp=n,b" %command%```

### Special Thanks To:
[Lyall](https://github.com/Lyall), [eevbb](https://github.com/eevbb), and [PhantomGamers](https://github.com/PhantomGamers) for advice relating to BepInEx and Unity modding.

## Support The Project:
☕ If you've enjoyed or gotten usage from my work *(keep in mind, I do a majority of this completely for free on my spare time with no donations or compensation)*, please consider supporting my Ko-Fi below:
<br><br>[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/kingkrouch)

## Licensing:
- [BepinEx](https://github.com/BepInEx/BepInEx) is licensed under the GNU Lesser General Public License v2.1.

**SvSFix (c) 2024 Bryce Q.**

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

_**DISCLAIMER: Licensed under an MIT license in case a certain entity wants to implement my changes officially (hit me up, and I'll sort something out.**_ 😉 _**). I know that GPL Licensing presents problems with this kind of stuff. Since this technically modifies game code by reimplementing certain functions, this license may or may not be valid for other use scenarios (The binaries in the lib folder and the Mono runtimes ARE NOT MIT licensed). It's not my intention to stub anyone legally speaking.**_
