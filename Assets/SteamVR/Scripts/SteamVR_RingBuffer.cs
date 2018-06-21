using UnityEngine;
using System.Collections;

    public class SteamVR_RingBuffer<T>
    {
        protected T[] buffer;
        protected int currentIndex;
        protected T lastElement;

        public SteamVR_RingBuffer(int size)
        {
            buffer = new T[size];
            currentIndex = 0;
        }

        public void Add(T newElement)
        {
            buffer[currentIndex] = newElement;

            StepForward();
        }

        public virtual void StepForward()
        {
            lastElement = buffer[currentIndex];

            currentIndex++;
            if (currentIndex >= buffer.Length)
                currentIndex = 0;

            cleared = false;
        }

        public virtual T GetAtIndex(int atIndex)
        {
            if (atIndex < 0)
                atIndex += buffer.Length;

            return buffer[atIndex];
        }

        public virtual T GetLast()
        {
            return lastElement;
        }

        public virtual int GetLastIndex()
        {
            int lastIndex = currentIndex - 1;
            if (lastIndex < 0)
                lastIndex += buffer.Length;

            return lastIndex;
        }

        private bool cleared = false;
        public void Clear()
        {
            if (cleared == true)
                return;

            if (buffer == null)
                return;

            for (int index = 0; index < buffer.Length; index++)
            {
                buffer[index] = default(T);
            }

            lastElement = default(T);

            currentIndex = 0;

            cleared = true;
        }
    }

    public class SteamVR_HistoryBuffer : SteamVR_RingBuffer<SteamVR_HistoryStep>
    {
        public SteamVR_HistoryBuffer(int size) : base(size)
        {

        }

        public void Update(Transform transform)
        {
            if (buffer[currentIndex] == null)
                buffer[currentIndex] = new SteamVR_HistoryStep();

            buffer[currentIndex].position = transform.position;
            buffer[currentIndex].rotation = transform.rotation;
            buffer[currentIndex].time = Time.time;

            UpdateTimes(buffer[currentIndex], lastElement);
            UpdateVelocities(buffer[currentIndex], lastElement);

            //only verify the frame if it's been a real amount of time since the last one.
            if (buffer[currentIndex].timeSinceLastFrame > 0.0001 || lastElement == null)
            {
                if (lastElement == null || buffer[currentIndex].velocitySinceLastFrame != Vector3.zero)
                {
                    StepForward();
                }
            }
        }

        public void Update(Rigidbody rigidbody)
        {
            if (buffer[currentIndex] == null)
                buffer[currentIndex] = new SteamVR_HistoryStep();

            buffer[currentIndex].position = rigidbody.position;
            buffer[currentIndex].rotation = rigidbody.rotation;
            buffer[currentIndex].time = Time.time;

            UpdateTimes(buffer[currentIndex], lastElement);
            UpdateVelocities(buffer[currentIndex], lastElement);

            //only verify the frame if it's been a real amount of time since the last one.
            if (buffer[currentIndex].timeSinceLastFrame > 0.0001 || lastElement == null)
            {
                if (lastElement == null || buffer[currentIndex].velocitySinceLastFrame != Vector3.zero)
                {
                    StepForward();
                }
            }
        }

        private const float fixedUpdateTime = 0.011f;

        private void UpdateTimes(SteamVR_HistoryStep current, SteamVR_HistoryStep last)
        {
            if (last != null)
            {
                current.timeSinceLastFrame = (current.time - last.time);
                current.timeNormalizedSinceLastFrame = fixedUpdateTime / current.timeSinceLastFrame;
            }
            else
            {
                current.timeSinceLastFrame = -1;
                current.timeNormalizedSinceLastFrame = -1;
            }
        }

        private void UpdateVelocities(SteamVR_HistoryStep current, SteamVR_HistoryStep last)
        {
            if (last != null && current.timeSinceLastFrame != 0)
            {
                current.velocitySinceLastFrame = (current.position - last.position) / current.timeSinceLastFrame;

                Quaternion rotationDelta = current.rotation * Quaternion.Inverse(last.rotation);
                float angle;
                Vector3 axis;
                rotationDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;

                Vector3 angularTarget = angle * axis;

                current.angularVelocitySinceLastFrame = angularTarget / 100 / current.timeSinceLastFrame;

                current.velocityNormalizedSinceLastFrame = (current.velocitySinceLastFrame * current.timeNormalizedSinceLastFrame);
                current.angularVelocityNormalizedSinceLastFrame = (current.angularVelocitySinceLastFrame * current.timeNormalizedSinceLastFrame);
            }
        }

        public float GetVelocityMagnitudeTrend(int toIndex = -1, int fromIndex = -1)
        {
            if (toIndex == -1)
                toIndex = currentIndex - 1;

            if (toIndex < 0)
                toIndex += buffer.Length;

            if (fromIndex == -1)
                fromIndex = toIndex - 1;

            if (fromIndex < 0)
                fromIndex += buffer.Length;

            SteamVR_HistoryStep toStep = buffer[toIndex];
            SteamVR_HistoryStep fromStep = buffer[fromIndex];

            if (IsValid(toStep) && IsValid(fromStep))
            {
                return toStep.velocityNormalizedSinceLastFrame.sqrMagnitude - fromStep.velocityNormalizedSinceLastFrame.sqrMagnitude;
            }

            return 0;
        }

        public bool IsValid(SteamVR_HistoryStep step)
        {
            return step != null && step.timeSinceLastFrame != -1 && step.timeSinceLastFrame != 0;
        }

        public int GetTopVelocity(int forFrames, int addFrames = 0)
        {
            int topFrame = currentIndex;
            float topVelocitySqr = 0;

            int currentFrame = currentIndex;

            while (forFrames > 0)
            {
                forFrames--;
                currentFrame--;

                if (currentFrame < 0)
                    currentFrame = buffer.Length - 1;

                SteamVR_HistoryStep currentStep = buffer[currentFrame];

                if (IsValid(currentStep) == false)
                    break;

                float currentSqr = buffer[currentFrame].velocityNormalizedSinceLastFrame.sqrMagnitude;
                if (currentSqr > topVelocitySqr)
                {
                    topFrame = currentFrame;
                    topVelocitySqr = currentSqr;
                }
            }

            topFrame += addFrames;

            if (topFrame >= buffer.Length)
                topFrame -= buffer.Length;

            return topFrame;
        }

        public void GetAverageVelocities(out Vector3 velocity, out Vector3 angularVelocity, int forFrames, int startFrame = -1)
        {
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;

            if (startFrame == -1)
                startFrame = currentIndex - 1;

            if (startFrame < 0)
                startFrame = buffer.Length - 1;

            int endFrame = startFrame - forFrames;

            if (endFrame < 0)
                endFrame += buffer.Length;

            Vector3 totalVelocity = Vector3.zero;
            Vector3 totalAngularVelocity = Vector3.zero;
            float totalFrames = 0;
            int currentFrame = startFrame;
            while (forFrames > 0)
            {
                forFrames--;
                currentFrame--;

                if (currentFrame < 0)
                    currentFrame = buffer.Length - 1;

                SteamVR_HistoryStep currentStep = buffer[currentFrame];

                if (IsValid(currentStep) == false)
                    break;

                totalFrames++;

                totalVelocity += currentStep.velocityNormalizedSinceLastFrame;
                totalAngularVelocity += currentStep.angularVelocityNormalizedSinceLastFrame;
            }

            velocity = totalVelocity / totalFrames;
            angularVelocity = totalAngularVelocity / totalFrames;
        }
    }

    public class SteamVR_FloatHistoryBuffer : SteamVR_RingBuffer<SteamVR_HistoryStepFloat>
    {
        public SteamVR_FloatHistoryBuffer(int size) : base(size)
        {

        }

        public void Update(float newValue)
        {
            if (buffer[currentIndex] == null)
                buffer[currentIndex] = new SteamVR_HistoryStepFloat();

            buffer[currentIndex].state = newValue;
            buffer[currentIndex].time = Time.time;

            UpdateTimes(buffer[currentIndex], lastElement);
            UpdateDeltas(buffer[currentIndex], lastElement);

            //only verify the frame if it's been a real amount of time since the last one.
            if (buffer[currentIndex].timeSinceLastFrame > 0.00001 || lastElement == null)
            {
                StepForward();
            }
            else
            {
                Debug.Log("Fake frame " + Time.time);
            }
        }

        private const float fixedUpdateTime = 0.011f;

        private void UpdateTimes(SteamVR_HistoryStepFloat current, SteamVR_HistoryStepFloat last)
        {
            if (last != null)
            {
                current.timeSinceLastFrame = (current.time - last.time);
                current.timeNormalizedSinceLastFrame = fixedUpdateTime / current.timeSinceLastFrame;
            }
            else
            {
                current.timeSinceLastFrame = -1;
                current.timeNormalizedSinceLastFrame = -1;
            }
        }

        private void UpdateDeltas(SteamVR_HistoryStepFloat current, SteamVR_HistoryStepFloat last)
        {
            if (last != null && current.timeSinceLastFrame != 0)
            {
                current.deltaSinceLastFrame = (current.state - last.state);
                current.deltaNormalizedSinceLastFrame = current.deltaSinceLastFrame * current.timeNormalizedSinceLastFrame;
            }
        }

        public float GetTrend(int toIndex = -1, int fromIndex = -1)
        {
            if (toIndex == -1)
                toIndex = currentIndex - 1;

            if (toIndex < 0)
                toIndex += buffer.Length;

            if (fromIndex == -1)
                fromIndex = toIndex - 1;

            if (fromIndex < 0)
                fromIndex += buffer.Length;

            SteamVR_HistoryStepFloat toStep = buffer[toIndex];
            SteamVR_HistoryStepFloat fromStep = buffer[fromIndex];

            if (IsValid(toStep) && IsValid(fromStep))
            {
                return toStep.deltaNormalizedSinceLastFrame - fromStep.deltaNormalizedSinceLastFrame;
            }

            return 0;
        }

        public bool IsValid(SteamVR_HistoryStepFloat step)
        {
            return step != null && step.timeSinceLastFrame != -1 && step.timeSinceLastFrame != 0;
        }

        public int GetLargestDrop(int forFrames, int addFrames = 0)
        {
            int currentFrame = currentIndex - 1;
            if (currentFrame < 0)
                currentFrame = buffer.Length - 1;

            int largestDropFrame = currentFrame;
            float largestDrop = 0;


            while (forFrames > 0)
            {
                forFrames--;
                currentFrame--;

                if (currentFrame < 0)
                    currentFrame = buffer.Length - 1;

                SteamVR_HistoryStepFloat currentStep = buffer[currentFrame];

                if (IsValid(currentStep) == false)
                    break;

                float currentDrop = buffer[currentFrame].deltaNormalizedSinceLastFrame;
                if (currentDrop < largestDrop)
                {
                    largestDropFrame = currentFrame;
                    largestDrop = currentDrop;
                }
            }

            largestDropFrame += addFrames;

            if (largestDropFrame >= buffer.Length)
                largestDropFrame -= buffer.Length;

            return largestDropFrame;
        }
        
        public bool IsDropSignificant(int atFrame = -1)
        {
            if (atFrame == -1)
                atFrame = currentIndex - 1;

            if (atFrame < 0)
                atFrame = buffer.Length - 1;

            int previousFrame = atFrame - 1;
            if (previousFrame < 0)
                previousFrame = buffer.Length - 1;

            SteamVR_HistoryStepFloat currentStep = buffer[atFrame];
            SteamVR_HistoryStepFloat previousStep = buffer[previousFrame];

            if (IsValid(currentStep) == false || IsValid(previousStep) == false)
                return false;

            bool drop = currentStep.state < (previousStep.state / 2f);

            if (drop)
                return true;

            return false;
        }
    }

    public class SteamVR_HistoryStep
    {
        public Vector3 position;
        public Quaternion rotation;

        public Vector3 velocitySinceLastFrame;
        public Vector3 velocityNormalizedSinceLastFrame;

        public Vector3 angularVelocitySinceLastFrame;
        public Vector3 angularVelocityNormalizedSinceLastFrame;

        public float time;
        public float timeSinceLastFrame;
        public float timeNormalizedSinceLastFrame;
    }

    public class SteamVR_HistoryStepFloat
    {
        public float state;
        public float deltaSinceLastFrame;
        public float deltaNormalizedSinceLastFrame;

        public float time;
        public float timeSinceLastFrame;
        public float timeNormalizedSinceLastFrame;
    }