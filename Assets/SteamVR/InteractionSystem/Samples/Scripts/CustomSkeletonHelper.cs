//======= Copyright (c) Valve Corporation, All rights reserved. ===============

using UnityEngine;
using System.Collections;
using Valve.VR;
using UnityEngine.Serialization;

namespace Valve.VR.InteractionSystem.Sample
{
    public class CustomSkeletonHelper : MonoBehaviour
    {
        public Retargetable wrist;

        public Finger[] fingers;
        public Thumb[] thumbs;

        private void Update()
        {
            for (int fingerIndex = 0; fingerIndex < fingers.Length; fingerIndex++)
            {
                Finger finger = fingers[fingerIndex];
                finger.metacarpal.destination.rotation = finger.metacarpal.source.rotation;
                finger.proximal.destination.rotation = finger.proximal.source.rotation;
                finger.middle.destination.rotation = finger.middle.source.rotation;
                finger.distal.destination.rotation = finger.distal.source.rotation;
            }
            for (int thumbIndex = 0; thumbIndex < thumbs.Length; thumbIndex++)
            {
                Thumb thumb = thumbs[thumbIndex];
                thumb.metacarpal.destination.rotation = thumb.metacarpal.source.rotation;
                thumb.middle.destination.rotation = thumb.middle.source.rotation;
                thumb.distal.destination.rotation = thumb.distal.source.rotation;
            }

            wrist.destination.position = wrist.source.position;
            wrist.destination.rotation = wrist.source.rotation;
        }

        public enum MirrorType
        {
            None,
            LeftToRight,
            RightToLeft
        }

        [System.Serializable]
        public class Retargetable
        {
            public Transform source;
            public Transform destination;

            public Retargetable(Transform source, Transform destination)
            {
                this.source = source;
                this.destination = destination;
            }
        }

        [System.Serializable]
        public class Thumb
        {
            public Retargetable metacarpal;
            public Retargetable middle;
            public Retargetable distal;

            public Transform aux;

            public Thumb(Retargetable metacarpal, Retargetable middle, Retargetable distal, Transform aux)
            {
                this.metacarpal = metacarpal;
                this.middle = middle;
                this.distal = distal;
                this.aux = aux;
            }
        }

        [System.Serializable]
        public class Finger
        {
            public Retargetable metacarpal;
            public Retargetable proximal;
            public Retargetable middle;
            public Retargetable distal;

            public Transform aux;

            public Finger(Retargetable metacarpal, Retargetable proximal, Retargetable middle, Retargetable distal, Transform aux)
            {
                this.metacarpal = metacarpal;
                this.proximal = proximal;
                this.middle = middle;
                this.distal = distal;
                this.aux = aux;
            }
        }
    }
}