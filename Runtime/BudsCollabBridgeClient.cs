using System;
using System.Collections.Generic;
using UnityEngine;

namespace BudsCollab.Unity
{
    public readonly struct BudsCollabSelectionReport
    {
        public BudsCollabSelectionReport(bool ok, string summary)
        {
            Ok = ok;
            Summary = summary;
        }

        public bool Ok { get; }
        public string Summary { get; }
    }

    public static class BudsCollabSelectionValidator
    {
        public static BudsCollabSelectionReport Validate(IReadOnlyList<GameObject> selectedObjects)
        {
            if (selectedObjects == null || selectedObjects.Count == 0)
            {
                return new BudsCollabSelectionReport(
                    false,
                    "Select one or more scene objects before checking the asset."
                );
            }

            var meshCount = 0;
            var rendererCount = 0;
            var vertexCount = 0;
            var triangleCount = 0;
            var materialSlots = 0;
            var missingMaterials = 0;

            foreach (var gameObject in selectedObjects)
            {
                if (gameObject == null)
                {
                    continue;
                }

                foreach (var meshFilter in gameObject.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = meshFilter.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }

                    meshCount++;
                    vertexCount += mesh.vertexCount;
                    triangleCount += mesh.triangles.Length / 3;
                }

                foreach (var skinnedMesh in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh = skinnedMesh.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }

                    meshCount++;
                    vertexCount += mesh.vertexCount;
                    triangleCount += mesh.triangles.Length / 3;
                }

                foreach (var renderer in gameObject.GetComponentsInChildren<Renderer>(true))
                {
                    rendererCount++;
                    var materials = renderer.sharedMaterials ?? Array.Empty<Material>();
                    materialSlots += materials.Length;
                    foreach (var material in materials)
                    {
                        if (material == null)
                        {
                            missingMaterials++;
                        }
                    }
                }
            }

            var warnings = new List<string>();
            if (meshCount == 0 || vertexCount == 0 || triangleCount == 0)
            {
                warnings.Add("no renderable mesh geometry");
            }

            if (vertexCount > 250000)
            {
                warnings.Add("high vertex count");
            }

            if (rendererCount == 0)
            {
                warnings.Add("no renderers");
            }

            if (missingMaterials > 0)
            {
                warnings.Add($"{missingMaterials} missing material slot(s)");
            }

            var summary =
                $"{meshCount} mesh(es), {rendererCount} renderer(s), {vertexCount:n0} vertices, {triangleCount:n0} triangles, {materialSlots} material slot(s)";

            if (warnings.Count > 0)
            {
                return new BudsCollabSelectionReport(
                    false,
                    $"{summary}. Check: {string.Join(", ", warnings)}."
                );
            }

            return new BudsCollabSelectionReport(true, $"{summary}. Ready for GLB export or upload.");
        }
    }
}
