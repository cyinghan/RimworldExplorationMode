using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace RimworldExploration.Layer
{
	[StaticConstructorOnStartup]

	public class WorldLayer_Fog: WorldLayer
	{
		private List<Vector3> elevationValues = new List<Vector3>();

		private Material TileMaterial_Fog;
		
		private List<Vector3> verts = new List<Vector3>();
		private List<int> tileIDToVerts_offsets = new List<int>();
		private List<int> tileIDToNeighbors_offsets = new List<int>();
		private List<int> tileIDToNeighbors_values = new List<int>();
		private List<List<int>> triangleIndexToTileID = new List<List<int>>();
		
		private Dictionary<int, List<int>> trackVerts = new Dictionary<int, List<int>>();
		private Dictionary<int, int> tileToMesh = new Dictionary<int, int>();

		public override IEnumerable Regenerate()
		{
			WorldGrid grid = Find.World.grid;
			if (!TileMaterial_Fog)
			{
				TileMaterial_Fog = new Material(ShaderDatabase.MetaOverlay);
				Color tempColor = Color.black;
				tempColor.a = 0.5f;
				TileMaterial_Fog.color = tempColor;
				Vector3 viewCenter = grid.viewCenter;
				float viewAngle = grid.viewAngle;
				PlanetShapeGenerator.Generate(10, out verts, out tileIDToVerts_offsets, out tileIDToNeighbors_offsets, out tileIDToNeighbors_values, 100.3f, viewCenter, viewAngle);
				foreach (object item2 in CalculateInterpolatedVerticesParams())
				{
					yield return item2;
				}
			}
			
			if (!VisibilityManager.fogInitialized)
			{
				foreach (object item in base.Regenerate())
				{
					yield return item;
				}
				tileToMesh.Clear();
				trackVerts.Clear();
				subMeshes.Clear();
				VisibilityManager.fogInitialized = true;
			}
			
			int num = 0;
			foreach (int i in VisibilityManager.Precheck_TileID_Fog)
			{
				if (!trackVerts.ContainsKey(i))
				{
					int subMeshIndex;
					LayerSubMesh subMesh = GetSubMesh(TileMaterial_Fog, out subMeshIndex);
					trackVerts[i] = new List<int>();
					tileToMesh[i] = subMeshIndex;
					int count = subMesh.verts.Count;
					int num2 = 0;
					int num3 = ((i + 1 < tileIDToVerts_offsets.Count) ? tileIDToVerts_offsets[i + 1] : verts.Count);
					for (int j = tileIDToVerts_offsets[i]; j < num3; j++)
					{
						trackVerts[i].Add(subMesh.verts.Count);
						subMesh.verts.Add(verts[j]);
						subMesh.uvs.Add(elevationValues[num]);
						num++;
						if (j < num3 - 2)
						{
							subMesh.tris.Add(count + num2 + 2);
							subMesh.tris.Add(count + num2 + 1);
							subMesh.tris.Add(count);
						}
						num2++;
					}
				}
				
				LayerSubMesh matchedMesh = subMeshes[tileToMesh[i]];
				if (VisibilityManager.TileVisible(i) || !VisibilityManager.TileExplored(i))
				{
					foreach (int k in trackVerts[i])
					{
						matchedMesh.verts[k] = Vector3.zero;
					}
				}
				else
				{
					int j = tileIDToVerts_offsets[i];
					foreach (int k in trackVerts[i])
					{
						matchedMesh.verts[k] = verts[j];
						j++;
					}
				}
				matchedMesh.finalized = false;
			}
			FinalizeMesh(MeshParts.All);
			VisibilityManager.Precheck_TileID_Fog.Clear();
		}
		
		protected new void FinalizeMesh(MeshParts tags)
		{
			for (int index = 0; index < subMeshes.Count; ++index)
			{
				if (!subMeshes[index].finalized && subMeshes[index].verts.Count > 0)
					subMeshes[index].FinalizeMesh(tags);
			}
		}

		private IEnumerable CalculateInterpolatedVerticesParams()
		{
			elevationValues.Clear();
			WorldGrid grid = Find.World.grid;
			int tilesCount = grid.TilesCount;
			List<Tile> tiles = grid.tiles;
			for (int i = 0; i < tilesCount; i++)
			{
				Tile tile = tiles[i];
				float elevation = tile.elevation;
				int num = ((i + 1 < tileIDToNeighbors_offsets.Count)
					? tileIDToNeighbors_offsets[i + 1]
					: tileIDToNeighbors_values.Count);
				int num2 = ((i + 1 < tilesCount) ? tileIDToVerts_offsets[i + 1] : verts.Count);
				for (int j = tileIDToVerts_offsets[i]; j < num2; j++)
				{
					Vector3 item = default(Vector3);
					item.x = elevation + 1;
					bool flag = false;
					for (int k = tileIDToNeighbors_offsets[i]; k < num; k++)
					{
						int num3 = ((tileIDToNeighbors_values[k] + 1 < tileIDToVerts_offsets.Count)
							? tileIDToVerts_offsets[tileIDToNeighbors_values[k] + 1]
							: verts.Count);
						for (int l = tileIDToVerts_offsets[tileIDToNeighbors_values[k]]; l < num3; l++)
						{
							if (!(verts[l] == verts[j]))
							{
								continue;
							}

							Tile tile2 = tiles[tileIDToNeighbors_values[k]];
							if (!flag)
							{
								if ((tile2.elevation >= 0f && elevation <= 0f) ||
								    (tile2.elevation <= 0f && elevation >= 0f))
								{
									flag = true;
								}
								else if (tile2.elevation > item.x)
								{
									item.x = tile2.elevation;
								}
							}

							break;
						}
					}
					elevationValues.Add(item);
				}

				if (i % 1000 == 0)
				{
					yield return null;
				}
			}
		}
	}
}