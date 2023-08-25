//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Valve.VR
{
    public enum SteamVR_Input_Sources
    {
        [Description("/unrestricted")] 
        Any,

        [Description("/user/hand/left")]
        LeftHand,

        [Description("/user/hand/right")]
        RightHand,

        [Description("/user/foot/left")]
        LeftFoot,

        [Description("/user/foot/right")]
        RightFoot,

        [Description("/user/shoulder/left")]
        LeftShoulder,

        [Description("/user/shoulder/right")]
        RightShoulder,

        [Description("/user/waist")]
        Waist,

        [Description("/user/chest")]
        Chest,

        [Description("/user/head")]
        Head,

        [Description("/user/gamepad")]
        Gamepad,

        [Description("/user/camera")]
        Camera,

        [Description("/user/keyboard")]
        Keyboard,

        [Description("/user/treadmill")]
        Treadmill,

        [Description("/user/knee/left")]
        LeftKnee,

        [Description("/user/knee/right")]
        RightKnee,

        [Description("/user/elbow/left")]
        LeftElbow,

        [Description("/user/elbow/right")]
        RightElbow,

        [Description("/user/wrist/left")]
        LeftWrist,

        [Description("/user/wrist/right")]
        RightWrist,

        [Description("/user/ankle/left")]
        LeftAnkle,

        [Description("/user/ankle/right")]
        RightAnkle,
    }
}

namespace Valve.VR.InputSources
{
    using Sources = SteamVR_Input_Sources;
}