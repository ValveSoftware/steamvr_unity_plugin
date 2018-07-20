using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

public enum SteamVR_Input_Input_Sources //todo: get these paths from the constants enum
{
    [Description("/unrestricted")] //todo: check to see if this gets exported: k_ulInvalidInputHandle 
    Any,

    [Description("/user/hand/left")]
    LeftHand,

    [Description("/user/hand/right")]
    RightHand,
}