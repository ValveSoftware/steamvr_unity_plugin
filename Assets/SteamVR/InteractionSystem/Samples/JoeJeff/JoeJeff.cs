using UnityEngine;
using System.Collections;
using Valve.VR.InteractionSystem;

namespace Valve.VR.InteractionSystem.Sample
{
    public class JoeJeff : MonoBehaviour
    {
        public float animationSpeed;

        public float jumpVelocity;

        [SerializeField]
        private float m_MovingTurnSpeed = 360;
        [SerializeField]
        private float m_StationaryTurnSpeed = 180;

        public float airControl;

        [Tooltip("The time it takes after landing a jump to slow down")]
        public float frictionTime = 0.2f;

        [SerializeField]
        private float footHeight = 0.1f;
        [SerializeField]
        private float footRadius = 0.03f;

        private RaycastHit footHit;

        private bool isGrounded;

        private float turnAmount;
        private float forwardAmount;

        private float groundedTime;

        private Animator animator;

        private Vector3 input;

        private bool held;

        private new Rigidbody rigidbody;
        private Interactable interactable;

        public FireSource fire;


        private void Start()
        {
            animator = GetComponent<Animator>();
            rigidbody = GetComponent<Rigidbody>();
            interactable = GetComponent<Interactable>();
            animator.speed = animationSpeed;
        }

        private void Update()
        {
            held = interactable.attachedToHand != null;

            jumpTimer -= Time.deltaTime;

            CheckGrounded();

            rigidbody.freezeRotation = !held;

            if (held == false)
                FixRotation();
        }

        private void FixRotation()
        {
            Vector3 eulers = transform.eulerAngles;
            eulers.x = 0;
            eulers.z = 0;
            Quaternion targetRotation = Quaternion.Euler(eulers);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * (isGrounded ? 20 : 3));
        }


        public void OnAnimatorMove()
        {
            // we implement this function to override the default root motion.
            // this allows us to modify the positional speed before it's applied.
            if (Time.deltaTime > 0)
            {
                Vector3 animationDelta = (animator.deltaPosition) / Time.deltaTime;

                animationDelta = Vector3.ProjectOnPlane(animationDelta, footHit.normal);

                if (isGrounded && jumpTimer < 0)
                {
                    if (groundedTime < frictionTime) //slow down when first hitting the floor after a jump
                    {
                        float moveFac = Mathf.InverseLerp(0, frictionTime, groundedTime);
                        //print(moveFac);
                        Vector3 lerpV = Vector3.Lerp(rigidbody.velocity, animationDelta, moveFac * Time.deltaTime * 30);
                        animationDelta.x = lerpV.x;
                        animationDelta.z = lerpV.z;
                    }

                    // adding a little downward force to keep him on the floor
                    animationDelta.y += -0.2f;// rb.velocity.y;
                    rigidbody.velocity = animationDelta;
                }
                else
                {
                    rigidbody.velocity += input * Time.deltaTime * airControl;
                }
            }
        }

        public void Move(Vector3 move, bool jump)
        {
            input = move;
            if (move.magnitude > 1f)
                move.Normalize();

            move = transform.InverseTransformDirection(move);

            turnAmount = Mathf.Atan2(move.x, move.z);
            forwardAmount = move.z;

            ApplyExtraTurnRotation();

            // control and velocity handling is different when grounded and airborne:
            if (isGrounded)
            {
                HandleGroundedMovement(jump);
            }


            // send input and other state parameters to the animator
            UpdateAnimator(move);
        }

        private void UpdateAnimator(Vector3 move)
        {
            animator.speed = fire.isBurning ? animationSpeed * 2 : animationSpeed;
            // update the animator parameters
            animator.SetFloat("Forward", fire.isBurning ? 2 : forwardAmount, 0.1f, Time.deltaTime);
            animator.SetFloat("Turn", turnAmount, 0.1f, Time.deltaTime);
            animator.SetBool("OnGround", isGrounded);
            animator.SetBool("Holding", held);

            if (!isGrounded)
            {
                animator.SetFloat("FallSpeed", Mathf.Abs(rigidbody.velocity.y));
                animator.SetFloat("Jump", rigidbody.velocity.y);
            }
        }

        private void ApplyExtraTurnRotation()
        {
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, forwardAmount);
            transform.Rotate(0, turnAmount * turnSpeed * Time.deltaTime, 0);
        }

        private void CheckGrounded()
        {
            isGrounded = false;
            if (jumpTimer < 0 & !held) // make sure we didn't just jump
            {
                isGrounded = (Physics.SphereCast(new Ray(transform.position + Vector3.up * footHeight, Vector3.down), footRadius, out footHit, footHeight - footRadius));
                if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(footHit.point.x, footHit.point.z)) > footRadius / 2)
                {
                    isGrounded = false;
                    //on slope, hit point is on edge of sphere cast
                }
            }
        }


        private void FixedUpdate()
        {
            groundedTime += Time.fixedDeltaTime;
            if (!isGrounded) groundedTime = 0; // reset timer

            if (isGrounded & !held)
            {
                Debug.DrawLine(transform.position, footHit.point);

                rigidbody.position = new Vector3(rigidbody.position.x, footHit.point.y, rigidbody.position.z);
            }
        }



        private void HandleGroundedMovement(bool jump)
        {
            // check whether conditions are right to allow a jump:
            if (jump && isGrounded)
            {
                Jump();
            }
        }

        private float jumpTimer;
        public void Jump()
        {
            isGrounded = false;
            jumpTimer = 0.1f;
            animator.applyRootMotion = false;
            rigidbody.position += Vector3.up * 0.03f;
            Vector3 velocity = rigidbody.velocity;
            velocity.y = jumpVelocity;
            rigidbody.velocity = velocity;
        }
    }
}