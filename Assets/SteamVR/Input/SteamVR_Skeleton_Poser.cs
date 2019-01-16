//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using System;
using System.Collections;
using UnityEngine;
using Valve.VR;

namespace Valve.VR
{
    public class SteamVR_Skeleton_Poser : MonoBehaviour
    {
        public GameObject previewLeftHandPrefab;
        public GameObject previewRightHandPrefab;

        public SteamVR_Skeleton_Pose skeletonPose;

        [SerializeField]
        protected bool showLeftPreview = false;

        [SerializeField]
        protected bool showRightPreview = true; //show the right hand by default

        [SerializeField]
        protected GameObject previewLeftInstance;

        [SerializeField]
        protected GameObject previewRightInstance;

        protected void Awake()
        {
            if (previewLeftInstance != null)
                DestroyImmediate(previewLeftInstance);
            if (previewRightInstance != null)
                DestroyImmediate(previewRightInstance);
        }
    }
}