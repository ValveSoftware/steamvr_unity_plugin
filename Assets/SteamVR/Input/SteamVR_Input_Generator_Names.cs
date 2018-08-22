//======= Copyright (c) Valve Corporation, All rights reserved. ===============


namespace Valve.VR
{
    public class SteamVR_Input_Generator_Names
    {
        public const string initializeActionSetsMethodName = "Dynamic_InitializeActionSets";
        public const string initializeActionsMethodName = "Dynamic_InitializeActions";
        public const string updateActionsMethodName = "Dynamic_UpdateActions";
        public const string updateNonPoseNonSkeletonActionsMethodName = "Dynamic_UpdateNonPoseNonSkeletonActions";
        public const string updatePoseActionsMethodName = "Dynamic_UpdatePoseActions";
        public const string updateSkeletonActionsMethodName = "Dynamic_UpdateSkeletalActions";

        public const string initializeInstanceActionsMethodName = "Dynamic_InitializeInstanceActions";
        public const string initializeInstanceActionSetsMethodName = "Dynamic_InitializeInstanceActionSets";

        public const string actionsFieldName = "actions";
        public const string actionsInFieldName = "actionsIn";
        public const string actionsOutFieldName = "actionsOut";
        public const string actionsVibrationFieldName = "actionsVibration";
        public const string actionsPoseFieldName = "actionsPose";
        public const string actionsBooleanFieldName = "actionsBoolean";
        public const string actionsSingleFieldName = "actionsSingle";
        public const string actionsVector2FieldName = "actionsVector2";
        public const string actionsVector3FieldName = "actionsVector3";
        public const string actionsSkeletonFieldName = "actionsSkeleton";
        public const string actionsNonPoseNonSkeletonIn = "actionsNonPoseNonSkeletonIn";
        public const string actionSetsFieldName = "actionSets";
    }
}