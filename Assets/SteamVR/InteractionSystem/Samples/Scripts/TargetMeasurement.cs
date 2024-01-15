//======= Copyright (c) Valve Corporation, All rights reserved. ===============

#if UNITY_UGUI_UI || !UNITY_2019_2_OR_NEWER
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Valve.VR.InteractionSystem.Sample
{
    public class TargetMeasurement : MonoBehaviour
    {
        public GameObject visualWrapper;
        public Transform measurementTape;

        public Transform endPoint;
        public Text measurementTextM;
        public Text measurementTextFT;

        public float maxDistanceToDraw = 6f;

        public bool drawTape = false;

        private float lastDistance;
        private void Update()
        {
            if (Camera.main != null)
            {
                Vector3 fromPoint = Camera.main.transform.position;
                fromPoint.y = endPoint.position.y;

                float distance = Vector3.Distance(fromPoint, endPoint.position);

                Vector3 center = Vector3.Lerp(fromPoint, endPoint.position, 0.5f);

                this.transform.position = center;
                this.transform.forward = endPoint.position - fromPoint;
                measurementTape.localScale = new Vector3(0.05f, distance, 0.05f);

                if (Mathf.Abs(distance - lastDistance) > 0.01f)
                {
                    measurementTextM.text = distance.ToString("00.0m");
                    measurementTextFT.text = (distance * 3.28084).ToString("00.0ft");

                    lastDistance = distance;
                }

                if (drawTape)
                    visualWrapper.SetActive(distance < maxDistanceToDraw);
                else
                    visualWrapper.SetActive(false);
            }
        }
    }
}
#else
using UnityEngine;
namespace Valve.VR.InteractionSystem.Sample { public class TargetMeasurement : MonoBehaviour { } }
#endif