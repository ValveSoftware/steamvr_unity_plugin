using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Valve.VR.InteractionSystem.Sample
{
    public class WheelDust : MonoBehaviour
    {
        private WheelCollider col;

        public ParticleSystem p;

        public float EmissionMul;

        public float velocityMul = 2;

        public float maxEmission;

        public float minSlip;

        [HideInInspector]
        public float amt;

        [HideInInspector]
        public Vector3 slip;

        private float emitTimer;


        private void Start()
        {
            col = GetComponent<WheelCollider>();
            StartCoroutine(emitter());
        }

        private void Update()
        {
            slip = Vector3.zero;
            if (col.isGrounded)
            {
                WheelHit hit;
                col.GetGroundHit(out hit);

                slip += Vector3.right * hit.sidewaysSlip;
                slip += Vector3.forward * -hit.forwardSlip;
                //print(slip);
            }
            amt = slip.magnitude;
            //print(amt);
        }

        private IEnumerator emitter()
        {
            while (true)
            {
                while (emitTimer < 1)
                {
                    yield return null;
                    if (amt > minSlip)
                    {
                        emitTimer += Mathf.Clamp((EmissionMul * amt), 0.01f, maxEmission);
                    }
                }
                emitTimer = 0;
                DoEmit();
            }
        }

        private void DoEmit()
        {
            p.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(slip));

#if UNITY_2017_1_OR_NEWER
            ParticleSystem.MainModule mainModule = p.main;
            mainModule.startSpeed = velocityMul * amt;
#else
            p.startSpeed = velocityMul * amt;
#endif

            p.Emit(1);
        }
    }
}