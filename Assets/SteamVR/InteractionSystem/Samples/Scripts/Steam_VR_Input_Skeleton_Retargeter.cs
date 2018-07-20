using UnityEngine;
using System.Collections;
using Valve.VR;
using UnityEngine.Serialization;

public class Steam_VR_Input_Skeleton_Retargeter : MonoBehaviour
{
    public Retargetable wrist;

    [FormerlySerializedAs("Fingers")]
    public Finger[] fingers;
    [FormerlySerializedAs("Thumbs")]
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
        [FormerlySerializedAs("Metacarpal")]
        public Retargetable metacarpal;
        [FormerlySerializedAs("f_middle")]
        public Retargetable middle;
        [FormerlySerializedAs("f_tip")]
        public Retargetable distal;

        public Transform aux;
        public Thumb(Retargetable Metacarpal, Retargetable f_middle, Retargetable f_tip, Transform aux)
        {
            this.metacarpal = Metacarpal;
            this.middle = f_middle;
            this.distal = f_tip;
            this.aux = aux;
        }
    }

    [System.Serializable]
    public class Finger
    {
        [FormerlySerializedAs("Metacarpal")]
        public Retargetable metacarpal;
        [FormerlySerializedAs("f_base")]
        public Retargetable proximal;
        [FormerlySerializedAs("f_middle")]
        public Retargetable middle;
        [FormerlySerializedAs("f_tip")]
        public Retargetable distal;

        public Transform aux;
        public Finger(Retargetable Metacarpal, Retargetable f_base, Retargetable f_middle, Retargetable f_tip, Transform aux)
        {
            this.metacarpal = Metacarpal;
            this.proximal = f_base;
            this.middle = f_middle;
            this.distal = f_tip;
            this.aux = aux;
        }
    }
}
