// AcousticSurfaceProperties.cs - Component attached to room boundary meshes
// Project: IVRAR Group 01 (Home Cinema Acoustics CEDIA/CTA-RP22)

using UnityEngine;

[DisallowMultipleComponent]
public class AcousticSurfaceProperties : MonoBehaviour
{
    [Header("Surface Identification")]
    public string surfaceName = "Wall Surface";
    public float surfaceAreaM2 = 15.0f;

    [Header("Assigned Acoustic Material Data")]
    public AcousticPropertyData materialData;

    [Header("Mesh Renderer Binding")]
    public MeshRenderer targetRenderer;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<MeshRenderer>();
        }

        // Auto calculate area if mesh filter is present
        CalculateSurfaceArea();
    }

    /// <summary>
    /// Computes approximate surface area in square meters from mesh filter lossy scale or geometry.
    /// </summary>
    public void CalculateSurfaceArea()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Vector3 size = Vector3.Scale(meshFilter.sharedMesh.bounds.size, transform.lossyScale);
            // If it's a quad or flat box, surface area is the largest face
            float[] faces = new float[3] { size.x * size.y, size.y * size.z, size.x * size.z };
            System.Array.Sort(faces);
            if (faces[2] > 0.01f)
            {
                surfaceAreaM2 = faces[2];
            }
        }
    }

    /// <summary>
    /// Applies a new acoustic material data asset to this surface, updating both physics data and visual look.
    /// </summary>
    public void SetAcousticMaterial(AcousticPropertyData newMaterial)
    {
        materialData = newMaterial;
        if (materialData != null && targetRenderer != null)
        {
            if (materialData.visualMaterial != null)
            {
                if (Application.isPlaying) targetRenderer.material = materialData.visualMaterial;
                else targetRenderer.sharedMaterial = materialData.visualMaterial;
            }
            else
            {
                // Configure base color and smoothness
                Material mat = Application.isPlaying ? targetRenderer.material : targetRenderer.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", materialData.visualColor);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", materialData.visualColor);
                    }

                    if (mat.HasProperty("_Smoothness"))
                    {
                        mat.SetFloat("_Smoothness", 1.0f - materialData.visualRoughness);
                    }
                    if (mat.HasProperty("_Metallic"))
                    {
                        mat.SetFloat("_Metallic", materialData.visualMetallic);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the absorption coefficient for this surface at the specified octave band (0 = 125Hz ... 5 = 4kHz).
    /// </summary>
    public float GetAbsorptionAtBand(int bandIndex)
    {
        if (materialData != null)
        {
            return materialData.GetAbsorptionAtBand(bandIndex);
        }
        return 0.05f; // Fallback untreated drywall
    }
}
