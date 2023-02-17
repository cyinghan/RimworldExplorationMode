using System.Collections;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace RimworldExploration.Layer
{
    [StaticConstructorOnStartup]
    public class WorldLayer_UngeneratedFog : WorldLayer
    {
        private Material material;
        private Material oldMaterial;
        

        public override IEnumerable Regenerate()
        {
            if (material == null)
            {
                material = new Material(ShaderDatabase.DefaultShader);
                oldMaterial = material;
                material.color  = Color.black;
            }

            foreach (object item in base.Regenerate())
            {
                yield return item;
            }

            Vector3 viewCenter = Find.WorldGrid.viewCenter;
            float viewAngle = Find.WorldGrid.viewAngle;
            if (viewAngle < 180f)
            {
                SphereGenerator.Generate(4, 100f, -viewCenter, 180f - Mathf.Min(viewAngle, 180f) + 10f,
                    out var outVerts, out var outIndices);
                
                LayerSubMesh subMesh = GetSubMesh(material);
                subMesh.verts.AddRange(outVerts);
                subMesh.tris.AddRange(outIndices);
            }

            FinalizeMesh(MeshParts.All);
        }
    }
}