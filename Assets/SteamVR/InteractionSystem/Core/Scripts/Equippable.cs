//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Flip Object to match which hand you pick it up in
//
//=============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Valve.VR.InteractionSystem
{

    public enum WhichHand
    {
        Left,
        Right
    }

    [RequireComponent(typeof(Throwable))]

    public class Equippable : MonoBehaviour
    {

        [Tooltip("Array of children you do not want to be mirrored. Text, logos, etc.")]
        public Transform[] antiFlip;

        public WhichHand defaultHand = WhichHand.Right;

        private Vector3 initialScale;
        private Interactable interactable;

        [HideInInspector]
        public SteamVR_Input_Sources attachedHandType
        {
            get
            {
                if (interactable.attachedToHand)
                    return interactable.attachedToHand.handType;
                else
                    return SteamVR_Input_Sources.Any;
            }
        }

        private void Start()
        {
            initialScale = transform.localScale;
            interactable = GetComponent<Interactable>();
        }

        private void Update()
        {
            if (interactable.attachedToHand)
            {
                Vector3 flipScale = initialScale;
                if ((attachedHandType == SteamVR_Input_Sources.RightHand && defaultHand == WhichHand.Right) || (attachedHandType == SteamVR_Input_Sources.LeftHand && defaultHand == WhichHand.Left))
                {
                    flipScale.x *= 1;
                    for (int transformIndex = 0; transformIndex < antiFlip.Length; transformIndex++)
                    {
                        antiFlip[transformIndex].localScale = new Vector3(1, 1, 1);
                    }
                }
                else
                {
                    flipScale.x *= -1;
                    for (int transformIndex = 0; transformIndex < antiFlip.Length; transformIndex++)
                    {
                        antiFlip[transformIndex].localScale = new Vector3(-1, 1, 1);
                    }
                }
                transform.localScale = flipScale;
            }
        }
    }
}
