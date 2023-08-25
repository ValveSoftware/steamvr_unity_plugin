//======= Copyright (c) Valve Corporation, All rights reserved. ===============

#if UNITY_UGUI_UI || !UNITY_2019_2_OR_NEWER
using UnityEngine;
using System.Collections;

namespace Valve.VR.InteractionSystem.Sample
{
    public class RenderModelChangerUI : UIElement
    {
        public GameObject leftPrefab;
        public GameObject rightPrefab;

        protected SkeletonUIOptions ui;

        protected override void Awake()
        {
            base.Awake();

            ui = this.GetComponentInParent<SkeletonUIOptions>();
        }

        protected override void OnButtonClick()
        {
            base.OnButtonClick();

            if (ui != null)
            {
                ui.SetRenderModel(this);
            }
        }
    }
}
#else
using UnityEngine;
namespace Valve.VR.InteractionSystem.Sample { public class RenderModelChangerUI : MonoBehaviour {
        public GameObject leftPrefab;
        public GameObject rightPrefab;
} }
#endif