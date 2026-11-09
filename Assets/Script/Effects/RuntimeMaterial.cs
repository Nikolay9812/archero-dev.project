using UnityEngine;

// Colors objects that scripts make while the game runs (arrows, coins, bullets, the bow, the wheel...).
//
// Why not just "renderer.material.color = color"? GameObject.CreatePrimitive gives new objects Unity's old
// built-in material. In the Editor that happens to work, but in a phone build URP can't draw it: it turns PINK.
// So we give them our own URP material first: Assets/Resources/RuntimeMaterial.mat.
// Everything in a "Resources" folder always goes into the build, and Resources.Load finds it by name.
public static class RuntimeMaterial
{
    private static Material baseMaterial;

    public static void Paint(Renderer renderer, Color color)
    {
        if (baseMaterial == null)
        {
            baseMaterial = Resources.Load<Material>("RuntimeMaterial");
            if (baseMaterial == null) Debug.LogWarning("Assets/Resources/RuntimeMaterial.mat is missing: objects may be pink in a build.");
        }

        if (baseMaterial != null) renderer.sharedMaterial = baseMaterial;
        renderer.material.color = color; // ".material" makes this object its own copy, so each can have its own color
    }
}
