using System;
using System.Collections.Generic;
using UnityEngine;

namespace BudsCollab.Unity
{
    public enum BudsCollabTargetProfile
    {
        RoomObject,
        MobileLight,
        HighDetail,
        PrintCleanup
    }

    public readonly struct BudsCollabTargetBudget
    {
        public BudsCollabTargetBudget(
            string label,
            int goodTriangles,
            int maxTriangles,
            int goodMaterialSlots,
            int maxMaterialSlots,
            float maxBoundsMeters
        )
        {
            Label = label;
            GoodTriangles = goodTriangles;
            MaxTriangles = maxTriangles;
            GoodMaterialSlots = goodMaterialSlots;
            MaxMaterialSlots = maxMaterialSlots;
            MaxBoundsMeters = maxBoundsMeters;
        }

        public string Label { get; }
        public int GoodTriangles { get; }
        public int MaxTriangles { get; }
        public int GoodMaterialSlots { get; }
        public int MaxMaterialSlots { get; }
        public float MaxBoundsMeters { get; }
    }

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
        public static BudsCollabTargetBudget BudgetFor(BudsCollabTargetProfile profile)
        {
            switch (profile)
            {
                case BudsCollabTargetProfile.MobileLight:
                    return new BudsCollabTargetBudget("Mobile / lightweight", 20000, 70000, 4, 12, 4f);
                case BudsCollabTargetProfile.HighDetail:
                    return new BudsCollabTargetBudget("High detail", 250000, 1000000, 16, 64, 12f);
                case BudsCollabTargetProfile.PrintCleanup:
                    return new BudsCollabTargetBudget("Print cleanup", 150000, 500000, 4, 16, 0.5f);
                case BudsCollabTargetProfile.RoomObject:
                default:
                    return new BudsCollabTargetBudget("Room object", 70000, 250000, 8, 32, 6f);
            }
        }

        public static BudsCollabSelectionReport Validate(
            IReadOnlyList<GameObject> selectedObjects,
            BudsCollabTargetProfile profile
        )
        {
            var budget = BudgetFor(profile);
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
            var lightCount = 0;
            var particleSystemCount = 0;
            Bounds? combinedBounds = null;

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
                    combinedBounds = combinedBounds.HasValue
                        ? Encapsulate(combinedBounds.Value, renderer.bounds)
                        : renderer.bounds;
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

                lightCount += gameObject.GetComponentsInChildren<Light>(true).Length;
                particleSystemCount += gameObject.GetComponentsInChildren<ParticleSystem>(true).Length;
            }

            var warnings = new List<string>();
            var notes = new List<string>();
            if (meshCount == 0 || vertexCount == 0 || triangleCount == 0)
            {
                warnings.Add("no renderable mesh geometry");
            }

            if (triangleCount > budget.MaxTriangles)
            {
                warnings.Add("very high triangle count");
            }
            else if (triangleCount > budget.GoodTriangles)
            {
                notes.Add("triangle count above lightweight target");
            }

            if (rendererCount == 0)
            {
                warnings.Add("no renderers");
            }

            if (missingMaterials > 0)
            {
                warnings.Add($"{missingMaterials} missing material slot(s)");
            }

            if (materialSlots > budget.MaxMaterialSlots)
            {
                warnings.Add("too many material slots");
            }
            else if (materialSlots > budget.GoodMaterialSlots)
            {
                notes.Add("material slots above lightweight target");
            }

            if (combinedBounds.HasValue)
            {
                var size = combinedBounds.Value.size;
                if (Mathf.Max(size.x, size.y, size.z) > budget.MaxBoundsMeters)
                {
                    warnings.Add("large object bounds");
                }
            }

            if (lightCount > 0)
            {
                notes.Add($"{lightCount} light component(s)");
            }

            if (particleSystemCount > 0)
            {
                notes.Add($"{particleSystemCount} particle system(s)");
            }

            var readiness = warnings.Count == 0 && notes.Count == 0
                ? "Good"
                : warnings.Count == 0
                    ? "Review"
                    : "Needs fixes";
            var boundsLabel = combinedBounds.HasValue
                ? $"{Mathf.Max(combinedBounds.Value.size.x, combinedBounds.Value.size.y, combinedBounds.Value.size.z):0.0}m max bounds"
                : "no bounds";
            var summary =
                $"{readiness} for {budget.Label}: {meshCount} mesh(es), {rendererCount} renderer(s), {vertexCount:n0} vertices, {triangleCount:n0} triangles, {materialSlots} material slot(s), {boundsLabel}";

            if (warnings.Count > 0)
            {
                return new BudsCollabSelectionReport(
                    false,
                    $"{summary}. Check: {string.Join(", ", warnings)}."
                );
            }

            if (notes.Count > 0)
            {
                return new BudsCollabSelectionReport(
                    true,
                    $"{summary}. Optimize: {string.Join(", ", notes)}."
                );
            }

            return new BudsCollabSelectionReport(true, $"{summary}. Ready for GLB export or upload.");
        }

        private static Bounds Encapsulate(Bounds current, Bounds next)
        {
            current.Encapsulate(next);
            return current;
        }
    }
}
