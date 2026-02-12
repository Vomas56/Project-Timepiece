using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldTransparent : MonoBehaviour
{
    [Range(0f, 1f)]
    public float alpha = 0.5f; // 0 = invisible, 1 = opaque

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        Material mat = renderer.material;

        // Enable transparency settings
        mat.SetFloat("_Surface", 1); // URP: Transparent
        mat.SetFloat("_Mode", 3);    // Standard Shader: Transparent

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        // Apply alpha
        Color color = mat.color;
        color.a = alpha;
        mat.color = color;
    }
}
