using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChanger : MonoBehaviour
{

    private Renderer renderer;

    // The brown color that makes default meshes, etc, eyedropped
    Color defaultBrown = new Color(0.4f, 0.9f, 0.7f, 1.0f);

    void Start()
    {
        renderer = GetComponent<Renderer>();
    }

    public void SetRed() 
    {
        renderer.material.color = Color.red;
    }

    public void SetBlue() 
    {
        renderer.material.color = Color.blue;
    }

    public void SetGray() 
    {
        renderer.material.color = Color.gray;
    }

    private void OnMouseEnter()
    {
        SetRed();
    }

    private void OnMouseExit()
    {
        SetBlue();
    }
}
