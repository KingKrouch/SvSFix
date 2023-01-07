using System;
using System.Collections.Generic;
using System.Linq;
using Game.UI;
using Game.UI.Config;
using IF.Utility.Helpers;
using KingKrouch.Utility.Helpers;
namespace SvSFix.ResolutionClasses;

public class CustomConfigScreenResolutionList : GameUiConfigDropDownList
{
    public void Entry()
    {
        GameUiConfigDropDownList.Local local_ = this.local_;
        if (local_ != null) {
            local_.Heading.Renew(StrInterface.UI_CONFIG_LIST_GENERAL_SCREEN_RESOLUTION);
        }
        GameUiConfigDropDownList.Local local_2 = this.local_;
        if (((local_2 != null) ? local_2.Listing : null) == null) {
            return;
        }
        OSB.GetShared();
        string @string = GameUiAccessor.GetString(StrInterface.UI_CONFIG_PARAM_RESOLUTION);
        List<ResolutionManager.resolution> list = ResolutionManager.ScreenResolutions().ToList<ResolutionManager.resolution>();
        for (int i = 0; i < list.Count; i++) {
            this.local_.Listing.Add(OSB.Start.AppendFormat(@string, list[i].width, list[i].height));
        }
        this.local_.Entry();
    }
}