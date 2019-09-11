using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Valve.VR.InteractionSystem
{
    public class HandCollider : MonoBehaviour
    {
        private new Rigidbody rigidbody;
        [HideInInspector]
        public HandPhysics hand;

        public LayerMask collisionMask;

        Collider[] colliders;


        public FingerColliders fingerColliders;

        [System.Serializable]
        public class FingerColliders
        {
            [Tooltip("Starting at tip and going down. Max 2.")]
            public Transform[] thumbColliders = new Transform[1];
            [Tooltip("Starting at tip and going down. Max 3.")]
            public Transform[] indexColliders = new Transform[2];
            [Tooltip("Starting at tip and going down. Max 3.")]
            public Transform[] middleColliders = new Transform[2];
            [Tooltip("Starting at tip and going down. Max 3.")]
            public Transform[] ringColliders = new Transform[2];
            [Tooltip("Starting at tip and going down. Max 3.")]
            public Transform[] pinkyColliders = new Transform[2];

            public Transform[] this[int finger]
            {
                get
                {
                    switch (finger)
                    {
                        case 0:
                            return thumbColliders;
                        case 1:
                            return indexColliders;
                        case 2:
                            return middleColliders;
                        case 3:
                            return ringColliders;
                        case 4:
                            return pinkyColliders;
                        default:
                            return null;
                    }
                }
                set
                {
                    switch (finger)
                    {
                        case 0:
                            thumbColliders = value; break;
                        case 1:
                            indexColliders = value; break;
                        case 2:
                            middleColliders = value; break;
                        case 3:
                            ringColliders = value; break;
                        case 4:
                            pinkyColliders = value; break;
                    }
                }
            }

        }

        private static PhysicMaterial physicMaterial_lowfriction;
        private static PhysicMaterial physicMaterial_highfriction;

        private void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
            rigidbody.maxAngularVelocity = 50;
        }

        private void Start()
        {
            colliders = GetComponentsInChildren<Collider>();

            if (physicMaterial_lowfriction == null)
            {
                physicMaterial_lowfriction = new PhysicMaterial("hand_lowFriction");
                physicMaterial_lowfriction.dynamicFriction = 0;
                physicMaterial_lowfriction.staticFriction = 0;
                physicMaterial_lowfriction.bounciness = 0;
                physicMaterial_lowfriction.bounceCombine = PhysicMaterialCombine.Minimum;
                physicMaterial_lowfriction.frictionCombine = PhysicMaterialCombine.Minimum;
            }

            if (physicMaterial_highfriction == null)
            {
                physicMaterial_highfriction = new PhysicMaterial("hand_highFriction");
                physicMaterial_highfriction.dynamicFriction = 1f;
                physicMaterial_highfriction.staticFriction = 1f;
                physicMaterial_highfriction.bounciness = 0;
                physicMaterial_highfriction.bounceCombine = PhysicMaterialCombine.Minimum;
                physicMaterial_highfriction.frictionCombine = PhysicMaterialCombine.Average;
            }

            SetPhysicMaterial(physicMaterial_lowfriction);

            scale = SteamVR_Utils.GetLossyScale(hand.transform);
        }

        void SetPhysicMaterial(PhysicMaterial mat)
        {
            if (colliders == null) colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].sharedMaterial = mat;
            }
        }

        float scale;

        public void SetCollisionDetectionEnabled(bool value)
        {
            rigidbody.detectCollisions = value;
        }

        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            targetPosition = position;
            targetRotation = rotation;
            //rigidbody.MovePosition(position);
            //rigidbody.MoveRotation(rotation);

            ExecuteFixedUpdate();
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            targetPosition = position;
            targetRotation = rotation;

            MoveTo(position, rotation);

            rigidbody.position = position;

            if (rotation.x != 0 || rotation.y != 0 || rotation.z != 0 || rotation.w != 0)
                rigidbody.rotation = rotation;

            //also update transform in case physics is disabled
            transform.position = position;
            transform.rotation = rotation;
        }

        public void Reset()
        {
            TeleportTo(targetPosition, targetRotation);
        }

        public void SetCenterPoint(Vector3 newCenter)
        {
            center = newCenter;
        }

        private Vector3 center;

        private Vector3 targetPosition = Vector3.zero;
        private Quaternion targetRotation = Quaternion.identity;

        protected const float MaxVelocityChange = 10f;
        protected const float VelocityMagic = 6000f;
        protected const float AngularVelocityMagic = 50f;
        protected const float MaxAngularVelocityChange = 20f;

        public bool collidersInRadius;
        protected void ExecuteFixedUpdate()
        {
            collidersInRadius = Physics.CheckSphere(center, 0.2f, collisionMask);
            if (collidersInRadius == false)
            {
                //keep updating velocity, just in case. Otherwise you get jitter
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
                /*
                rigidbody.velocity = (targetPosition - rigidbody.position) / Time.fixedDeltaTime;
                float angle; Vector3 axis;
                (targetRotation * Quaternion.Inverse(rigidbody.rotation)).ToAngleAxis(out angle, out axis);
                rigidbody.angularVelocity = axis.normalized * angle / Time.fixedDeltaTime;
                */

                rigidbody.MovePosition(targetPosition);
                rigidbody.MoveRotation(targetRotation);
            }
            else
            {
                Vector3 velocityTarget, angularTarget;
                bool success = GetTargetVelocities(out velocityTarget, out angularTarget);
                if (success)
                {
                    float maxAngularVelocityChange = MaxAngularVelocityChange * scale;
                    float maxVelocityChange = MaxVelocityChange * scale;

                    rigidbody.velocity = Vector3.MoveTowards(rigidbody.velocity, velocityTarget, maxVelocityChange);
                    rigidbody.angularVelocity = Vector3.MoveTowards(rigidbody.angularVelocity, angularTarget, maxAngularVelocityChange);
                }
            }
        }



        protected bool GetTargetVelocities(out Vector3 velocityTarget, out Vector3 angularTarget)
        {
            bool realNumbers = false;

            float velocityMagic = VelocityMagic;
            float angularVelocityMagic = AngularVelocityMagic;

            Vector3 positionDelta = (targetPosition - rigidbody.position);
            velocityTarget = (positionDelta * velocityMagic * Time.deltaTime);

            if (float.IsNaN(velocityTarget.x) == false && float.IsInfinity(velocityTarget.x) == false)
            {
                realNumbers = true;
            }
            else
                velocityTarget = Vector3.zero;


            Quaternion rotationDelta = targetRotation * Quaternion.Inverse(rigidbody.rotation);


            float angle;
            Vector3 axis;
            rotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (angle != 0 && float.IsNaN(axis.x) == false && float.IsInfinity(axis.x) == false)
            {
                angularTarget = angle * axis * angularVelocityMagic * Time.deltaTime;

                realNumbers &= true;
            }
            else
                angularTarget = Vector3.zero;

            return realNumbers;
        }


        const float minCollisionEnergy = 0.1f;
        const float maxCollisionEnergy = 1.0f;

        const float minCollisionHapticsTime = 0.2f;
        private float lastCollisionHapticsTime;
        private void OnCollisionEnter(Collision collision)
        {
            bool touchingDynamic = false;
            if (collision.rigidbody != null)
            {
                if (collision.rigidbody.isKinematic == false) touchingDynamic = true;
            }

            // low friction if touching static object, high friction if touching dynamic
            SetPhysicMaterial(touchingDynamic ? physicMaterial_highfriction : physicMaterial_lowfriction);



            float energy = collision.relativeVelocity.magnitude;

            if(energy > minCollisionEnergy && Time.time - lastCollisionHapticsTime > minCollisionHapticsTime)
            {
                lastCollisionHapticsTime = Time.time;

                float intensity = Util.RemapNumber(energy, minCollisionEnergy, maxCollisionEnergy, 0.3f, 1.0f);
                float length = Util.RemapNumber(energy, minCollisionEnergy, maxCollisionEnergy, 0.0f, 0.06f);

                hand.hand.TriggerHapticPulse(length, 100, intensity);
            }
        }

    }
}