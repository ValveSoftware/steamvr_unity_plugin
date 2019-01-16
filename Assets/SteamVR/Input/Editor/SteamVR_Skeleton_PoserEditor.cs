using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

namespace Valve.VR
{

    [CustomEditor(typeof(SteamVR_Skeleton_Poser))]
    public class SteamVR_Skeleton_PoserEditor : Editor
    {
        private const string leftDefaultAssetName = "vr_glove_left_model_slim";
        private const string rightDefaultAssetName = "vr_glove_right_model_slim";

        private SerializedProperty skeletonPoseProperty;

        private SerializedProperty showLeftPreviewProperty;
        private SerializedProperty showRightPreviewProperty;

        private SerializedProperty previewLeftInstanceProperty;
        private SerializedProperty previewRightInstanceProperty;

        private SerializedProperty previewLeftHandPrefab;
        private SerializedProperty previewRightHandPrefab;


        private SteamVR_Skeleton_Poser poser;

        protected void OnEnable()
        {
            skeletonPoseProperty = serializedObject.FindProperty("skeletonPose");

            showLeftPreviewProperty = serializedObject.FindProperty("showLeftPreview");
            showRightPreviewProperty = serializedObject.FindProperty("showRightPreview");

            previewLeftInstanceProperty = serializedObject.FindProperty("previewLeftInstance");
            previewRightInstanceProperty = serializedObject.FindProperty("previewRightInstance");

            previewLeftHandPrefab = serializedObject.FindProperty("previewLeftHandPrefab");
            previewRightHandPrefab = serializedObject.FindProperty("previewRightHandPrefab");

            poser = (SteamVR_Skeleton_Poser)target;
        }

        protected void LoadDefaultPreviewHands()
        {
            if (previewLeftHandPrefab.objectReferenceValue == null)
            {
                string[] defaultLeftPaths = AssetDatabase.FindAssets(string.Format("t:Prefab {0}", leftDefaultAssetName));
                if (defaultLeftPaths != null && defaultLeftPaths.Length > 0)
                {
                    string defaultLeftGUID = defaultLeftPaths[0];
                    string defaultLeftPath = AssetDatabase.GUIDToAssetPath(defaultLeftGUID);
                    previewLeftHandPrefab.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(defaultLeftPath);

                    if (previewLeftHandPrefab.objectReferenceValue == null)
                        Debug.LogError("[SteamVR] Could not load prefab: " + leftDefaultAssetName + ". Found path: " + defaultLeftPath);
                }
                else
                    Debug.LogError("[SteamVR] Could not load prefab: " + leftDefaultAssetName);
            }

            if (previewRightHandPrefab.objectReferenceValue == null)
            {
                string[] defaultRightPaths = AssetDatabase.FindAssets(string.Format("t:Prefab {0}", rightDefaultAssetName));
                if (defaultRightPaths != null && defaultRightPaths.Length > 0)
                {
                    string defaultRightGUID = defaultRightPaths[0];
                    string defaultRightPath = AssetDatabase.GUIDToAssetPath(defaultRightGUID);

                    previewRightHandPrefab.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(defaultRightPath);

                    if (previewLeftHandPrefab.objectReferenceValue == null)
                        Debug.LogError("[SteamVR] Could not load prefab: " + rightDefaultAssetName + ". Found path: " + defaultRightPath);
                }
                else
                    Debug.LogError("[SteamVR] Could not load prefab: " + rightDefaultAssetName);
            }
        }

        protected void UpdatePreviewHand(SerializedProperty instanceProperty, SerializedProperty showPreviewProperty, SerializedProperty prefabProperty, SteamVR_Skeleton_Pose_Hand handData)
        {
            GameObject preview = instanceProperty.objectReferenceValue as GameObject;
            EditorGUILayout.PropertyField(showPreviewProperty);
            if (showPreviewProperty.boolValue)
            {
                if (preview == null)
                {
                    preview = GameObject.Instantiate<GameObject>((GameObject)prefabProperty.objectReferenceValue);
                    preview.transform.parent = poser.transform;
                    preview.transform.localPosition = Vector3.zero;
                    preview.transform.localRotation = Quaternion.identity;

                    SteamVR_Behaviour_Skeleton previewSkeleton = null;

                    if (preview != null)
                        previewSkeleton = preview.GetComponent<SteamVR_Behaviour_Skeleton>();

                    if (previewSkeleton != null)
                    {
                        if (handData.bonePositions == null || handData.bonePositions.Length == 0)
                        {
                            previewSkeleton.ForceToReferencePose(defaultReferencePose);
                            SaveHandData(handData, previewSkeleton);
                        }

                        preview.transform.localPosition = Vector3.zero;
                        preview.transform.localRotation = Quaternion.identity;

                        preview.transform.localRotation = Quaternion.Inverse(handData.rotation);
                        preview.transform.position = preview.transform.TransformPoint(-handData.position);

                        for (int boneIndex = 0; boneIndex < handData.bonePositions.Length; boneIndex++)
                        {
                            Transform bone = previewSkeleton.GetBone(boneIndex);
                            bone.localPosition = handData.bonePositions[boneIndex];
                            bone.localRotation = handData.boneRotations[boneIndex];
                        }
                    }

                    instanceProperty.objectReferenceValue = preview;
                }
            }
            else
            {
                if (preview != null)
                {
                    DestroyImmediate(preview);
                }
            }
        }

        protected void ZeroTransformParents(Transform toZero, Transform stopAt)
        {
            if (toZero == null)
                return;

            toZero.localPosition = Vector3.zero;
            toZero.localRotation = Quaternion.identity;

            if (toZero == stopAt)
                return;

            ZeroTransformParents(toZero.parent, stopAt);
        }

        protected EVRSkeletalReferencePose defaultReferencePose = EVRSkeletalReferencePose.OpenHand;
        protected EVRSkeletalReferencePose forceToReferencePose = EVRSkeletalReferencePose.OpenHand;

        protected void SaveHandData(SteamVR_Skeleton_Pose_Hand handData, SteamVR_Behaviour_Skeleton thisSkeleton)
        {
            handData.position = thisSkeleton.transform.InverseTransformPoint(poser.transform.position);
            //handData.position = thisSkeleton.transform.localPosition;
            handData.rotation = Quaternion.Inverse(thisSkeleton.transform.localRotation);

            handData.bonePositions = new Vector3[SteamVR_Action_Skeleton.numBones];
            handData.boneRotations = new Quaternion[SteamVR_Action_Skeleton.numBones];

            for (int boneIndex = 0; boneIndex < SteamVR_Action_Skeleton.numBones; boneIndex++)
            {
                Transform bone = thisSkeleton.GetBone(boneIndex);
                handData.bonePositions[boneIndex] = bone.localPosition;
                handData.boneRotations[boneIndex] = bone.localRotation;
            }

            EditorUtility.SetDirty(poser.skeletonPose);
        }

        protected void DrawHand(bool showHand, SteamVR_Skeleton_Pose_Hand handData, SteamVR_Behaviour_Skeleton leftSkeleton, SteamVR_Behaviour_Skeleton rightSkeleton)
        {
            SteamVR_Behaviour_Skeleton thisSkeleton;
            SteamVR_Behaviour_Skeleton oppositeSkeleton;
            string thisSourceString;
            string oppositeSourceString;

            if (handData.inputSource == SteamVR_Input_Sources.LeftHand)
            {
                thisSkeleton = leftSkeleton;
                thisSourceString = "Left Hand";
                oppositeSourceString = "Right Hand";
                oppositeSkeleton = rightSkeleton;
            }
            else
            {
                thisSkeleton = rightSkeleton;
                thisSourceString = "Right Hand";
                oppositeSourceString = "Left Hand";
                oppositeSkeleton = leftSkeleton;
            }


            if (showHand)
            {
                bool save = GUILayout.Button(string.Format("Save {0}", thisSourceString));
                if (save)
                {
                    SaveHandData(handData, thisSkeleton);
                }

                bool getFromOpposite = GUILayout.Button(string.Format("Mirror {0} pose onto {1} skeleton", oppositeSourceString, thisSourceString));
                if (getFromOpposite)
                {
                    bool confirm = EditorUtility.DisplayDialog("SteamVR", string.Format("This will overwrite your current {0} skeleton data. (with data from the {1} skeleton)", thisSourceString, oppositeSourceString), "Overwrite", "Cancel");
                    if (confirm)
                    {
                        Vector3 reflectedPosition = new Vector3(-oppositeSkeleton.transform.localPosition.x, oppositeSkeleton.transform.localPosition.y, oppositeSkeleton.transform.localPosition.z);
                        thisSkeleton.transform.localPosition = reflectedPosition;

                        Quaternion oppositeRotation = oppositeSkeleton.transform.localRotation;
                        Quaternion reflectedRotation = new Quaternion(-oppositeRotation.x, oppositeRotation.y, oppositeRotation.z, -oppositeRotation.w);
                        thisSkeleton.transform.localRotation = reflectedRotation;

                        handData.position = reflectedPosition;
                        handData.rotation = reflectedRotation;

                        handData.bonePositions = new Vector3[SteamVR_Action_Skeleton.numBones];
                        handData.boneRotations = new Quaternion[SteamVR_Action_Skeleton.numBones];

                        for (int boneIndex = 0; boneIndex < SteamVR_Action_Skeleton.numBones; boneIndex++)
                        {
                            Transform boneThis = thisSkeleton.GetBone(boneIndex);
                            Transform boneOpposite = oppositeSkeleton.GetBone(boneIndex);

                            boneThis.localPosition = boneOpposite.localPosition;
                            boneThis.localRotation = boneOpposite.localRotation;

                            handData.bonePositions[boneIndex] = boneThis.localPosition;
                            handData.boneRotations[boneIndex] = boneThis.localRotation;
                        }

                        EditorUtility.SetDirty(poser.skeletonPose);
                    }
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label("Force to reference pose");
                GUILayout.FlexibleSpace();

                forceToReferencePose = (EVRSkeletalReferencePose)EditorGUILayout.EnumPopup(forceToReferencePose);

                GUILayout.FlexibleSpace();
                bool forcePose = GUILayout.Button("set");
                GUILayout.EndHorizontal();
                if (forcePose)
                {
                    bool confirm = EditorUtility.DisplayDialog("SteamVR", string.Format("This will overwrite your current {0} skeleton data. (with data from the {1} reference pose)", thisSourceString, forceToReferencePose.ToString()), "Overwrite", "Cancel");
                    if (confirm)
                    {
                        thisSkeleton.ForceToReferencePose(forceToReferencePose);
                    }
                }

                SteamVR_Skeleton_FingerExtensionTypes newThumb = (SteamVR_Skeleton_FingerExtensionTypes)EditorGUILayout.EnumPopup("Thumb movement", handData.thumbFingerMovementType);
                SteamVR_Skeleton_FingerExtensionTypes newIndex = (SteamVR_Skeleton_FingerExtensionTypes)EditorGUILayout.EnumPopup("Index movement", handData.indexFingerMovementType);
                SteamVR_Skeleton_FingerExtensionTypes newMiddle = (SteamVR_Skeleton_FingerExtensionTypes)EditorGUILayout.EnumPopup("Middle movement", handData.middleFingerMovementType);
                SteamVR_Skeleton_FingerExtensionTypes newRing = (SteamVR_Skeleton_FingerExtensionTypes)EditorGUILayout.EnumPopup("Ring movement", handData.ringFingerMovementType);
                SteamVR_Skeleton_FingerExtensionTypes newPinky = (SteamVR_Skeleton_FingerExtensionTypes)EditorGUILayout.EnumPopup("Pinky movement", handData.pinkyFingerMovementType);

                if (newThumb != handData.thumbFingerMovementType || newIndex != handData.indexFingerMovementType ||
                    newMiddle != handData.middleFingerMovementType || newRing != handData.ringFingerMovementType ||
                    newPinky != handData.pinkyFingerMovementType)
                {
                    if ((int)newThumb >= 2 || (int)newIndex >= 2 || (int)newMiddle >= 2 || (int)newRing >= 2 || (int)newPinky >= 2)
                    {
                        Debug.LogError("<b>[SteamVR Input]</b> Unfortunately only Static and Free modes are supported in this beta.");
                        return;
                    }

                    handData.thumbFingerMovementType = newThumb;
                    handData.indexFingerMovementType = newIndex;
                    handData.middleFingerMovementType = newMiddle;
                    handData.ringFingerMovementType = newRing;
                    handData.pinkyFingerMovementType = newPinky;

                    EditorUtility.SetDirty(poser.skeletonPose);
                }
            }
        }

        protected void DrawSaveButtons()
        {
            bool showLeft = showLeftPreviewProperty.boolValue;
            GameObject leftInstance = previewLeftInstanceProperty.objectReferenceValue as GameObject;
            SteamVR_Behaviour_Skeleton leftSkeleton = null;

            if (leftInstance != null)
                leftSkeleton = leftInstance.GetComponent<SteamVR_Behaviour_Skeleton>();

            bool showRight = showRightPreviewProperty.boolValue;
            GameObject rightInstance = previewRightInstanceProperty.objectReferenceValue as GameObject;
            SteamVR_Behaviour_Skeleton rightSkeleton = null;

            if (rightInstance != null)
                rightSkeleton = rightInstance.GetComponent<SteamVR_Behaviour_Skeleton>();


            EditorGUILayout.Space();

            DrawHand(showLeft, poser.skeletonPose.leftHand, leftSkeleton, rightSkeleton);

            EditorGUILayout.Space();

            DrawHand(showRight, poser.skeletonPose.rightHand, leftSkeleton, rightSkeleton);
        }

        public override void OnInspectorGUI()
        {
            bool createNew = false;

            serializedObject.Update();

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Cannot modify pose while in play mode.");
                return;
            }

            LoadDefaultPreviewHands();

            if (skeletonPoseProperty.objectReferenceValue == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(skeletonPoseProperty);
                createNew = GUILayout.Button("Create");
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.PropertyField(skeletonPoseProperty);
                
                EditorGUILayout.Space();

                UpdatePreviewHand(previewLeftInstanceProperty, showLeftPreviewProperty, previewLeftHandPrefab, poser.skeletonPose.leftHand);
                UpdatePreviewHand(previewRightInstanceProperty, showRightPreviewProperty, previewRightHandPrefab, poser.skeletonPose.rightHand);

                DrawSaveButtons();
            }

            serializedObject.ApplyModifiedProperties();

            if (createNew)
            {
                string fullPath = EditorUtility.SaveFilePanelInProject("Create New Skeleton Pose", "newPose", "asset", "Save file");

                if (string.IsNullOrEmpty(fullPath) == false)
                {
                    SteamVR_Skeleton_Pose newPose = ScriptableObject.CreateInstance<SteamVR_Skeleton_Pose>();
                    AssetDatabase.CreateAsset(newPose, fullPath);
                    AssetDatabase.SaveAssets();

                    skeletonPoseProperty.objectReferenceValue = newPose;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
