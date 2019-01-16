using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace Valve.VR.InteractionSystem.Sample
{
    public class JoeJeffGestures : MonoBehaviour
    {
        private const float openFingerAmount = 0.1f;
        private const float closedFingerAmount = 0.9f;
        private const float closedThumbAmount = 0.4f;

        private JoeJeff joeJeff;

        private void Awake()
        {
            joeJeff = this.GetComponent<JoeJeff>();
        }

        private void Update()
        {
            if (Player.instance == null)
                return;

            Transform cam = Camera.main.transform;
            bool lookingAt = (Vector3.Angle(cam.forward, transform.position - cam.position) < 90);

            if (lookingAt == false)
                return;

            for (int handIndex = 0; handIndex < Player.instance.hands.Length; handIndex++)
            {
                if (Player.instance.hands[handIndex] != null)
                {
                    SteamVR_Behaviour_Skeleton skeleton = Player.instance.hands[handIndex].skeleton;
                    if (skeleton != null)
                    {
                        //Debug.LogFormat("{0:0.00}, {1:0.00}, {2:0.00}, {3:0.00}, {4:0.00}", skeleton.thumbCurl, skeleton.indexCurl, skeleton.middleCurl, skeleton.ringCurl, skeleton.pinkyCurl);

                        if ((skeleton.indexCurl <= openFingerAmount && skeleton.middleCurl <= openFingerAmount) &&
                            (skeleton.thumbCurl >= closedThumbAmount && skeleton.ringCurl >= closedFingerAmount && skeleton.pinkyCurl >= closedFingerAmount))
                        {
                            PeaceSignRecognized(true);
                        }
                        else
                        {
                            PeaceSignRecognized(false);
                        }
                    }
                }
            }
        }

        private bool lastPeaceSignState = false;
        private void PeaceSignRecognized(bool currentPeaceSignState)
        {
            if (lastPeaceSignState == false && currentPeaceSignState == true)
            {
                joeJeff.Jump();
            }

            lastPeaceSignState = currentPeaceSignState;
        }
    }
}
