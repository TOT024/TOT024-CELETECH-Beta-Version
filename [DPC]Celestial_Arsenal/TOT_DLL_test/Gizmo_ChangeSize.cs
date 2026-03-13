using System;
using UnityEngine;
using Verse;

namespace TOT_DLL_test
{
    // Token: 0x02000047 RID: 71
    public class Gizmo_ChangeSize : Gizmo
    {
        // Token: 0x06000172 RID: 370 RVA: 0x0000BBEF File Offset: 0x00009DEF
        public Gizmo_ChangeSize()
        {
            this.Order = -90f;
        }

        // Token: 0x06000173 RID: 371 RVA: 0x0000BC08 File Offset: 0x00009E08
        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        // Token: 0x06000174 RID: 372 RVA: 0x0000BC20 File Offset: 0x00009E20
        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms p)
        {
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
