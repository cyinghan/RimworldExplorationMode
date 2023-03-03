using System.Collections;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace RimworldExploration.Layer
{
    [StaticConstructorOnStartup]
    public class WorldLayer_UngeneratedFog : WorldLayer
    {
        private Material TileMaterial_Fog;
        private Material material;

        public void Restore()
        {
            if (!TileMaterial_Fog)
            {
                TileMaterial_Fog = new Material(ShaderDatabase.MetaOverlay);
                Color tempColor = Color.black;
                tempColor.a = 0.5f;
                TileMaterial_Fog.color = tempColor;
                material = TileMaterial_Fog;
            }
        }

        public override IEnumerable Regenerate()
        {
            if (material == null)
            {
                material = new Material(ShaderDatabase.DefaultShader);
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