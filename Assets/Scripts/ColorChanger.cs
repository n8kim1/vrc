using UnityEngine;

public class ColorChanger : MonoBehaviour
{

    private Renderer colorChangerRenderer;

    // Color(0.4f, 0.9f, 0.7f, 1.0f) is the
    // brown-gray color that approximates the color of the
    // other meshes in the project. Derived empirically, with an eyedropper tool
    Color defaultColor = new(0.4f, 0.9f, 0.7f, 1.0f);

    void Start()
    {
        colorChangerRenderer = GetComponent<Renderer>();
    }

    public void SetRed() 
    {
        colorChangerRenderer.material.color = Color.red;
    }

    public void SetBlue() 
    {
        colorChangerRenderer.material.color = Color.blue;
    }

    public void SetGray() 
    {
        colorChangerRenderer.material.color = Color.gray;
    }

    public void ResetColor()
    {
        colorChangerRenderer.material.color = defaultColor;
    }

    // Helpful for testing
    private void OnMouseEnter()
    {
        SetRed();
    }

    // Helpful for testing
    private void OnMouseExit()
    {
        SetBlue();
    }
}
