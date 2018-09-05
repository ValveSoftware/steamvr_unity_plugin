//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Demonstrates how to Teleport onto NavMesh Walkable
//
//=============================================================================

using UnityEngine;
using UnityEngine.AI;
using Valve.VR.InteractionSystem;

namespace Valve.VR.InteractionSystem
{
  public class TeleportNavMeshExample : TeleportMarkerBase
  {
      [Tooltip("Adjust NavMesh Sensitivity")]
      public float range = 0.25f;

      public override void Highlight(bool highlight)
      {

      }

      public override void SetAlpha(float tintAlpha, float alphaPercent)
      {

      }

      bool RandomPoint(Vector3 center, float range, out Vector3 result)
      {
          Vector3 randomPoint = center;
          NavMeshHit hit;
          if (NavMesh.SamplePosition(randomPoint, out hit, range, 1 << NavMesh.GetAreaFromName("Walkable")))
          {
              result = hit.position;
              return true;
          }
          result = Vector3.zero;
          return false;
      }

      public override bool ShouldActivate(Vector3 playerPosition)
      {
          return true;
      }

      public override bool ShouldMovePlayer()
      {
          return !locked;
      }

      public override void UpdateVisuals()
      {
      }

      public override bool ValidateLocation(Vector3 pointerPosition)
      {
          var location = Vector3.zero;
          SetLocked(!RandomPoint(pointerPosition, range, out location));
          return locked;
      }
  }
}
