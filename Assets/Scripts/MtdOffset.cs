using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MtdOffset : MonoBehaviour
{
    [SerializeField] private MapToDesigner MapToDesigner;
    [SerializeField] private Vector3 OffsetVelocity;
    [SerializeField] private Vector3 OffsetVelocityWhenShift;

    [SerializeField] private Material MatStandard;
    [SerializeField] private Material MatMouseOver;
    [SerializeField] private Material MatMouseDown;

    private MeshRenderer MeshRenderer;

    // True when it is controlling the offset
    private bool OffsetAction = false;

    // True when the mouse is over the collider
    private bool HighlightAction = false;

    private void Awake()
    {
        MeshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (OffsetAction)
        {
            if (Input.GetKey(KeyCode.LeftShift))
                MapToDesigner.BackdropOffset += OffsetVelocityWhenShift * Time.deltaTime;
            else
                MapToDesigner.BackdropOffset += OffsetVelocity * Time.deltaTime;

            MeshRenderer.material = MatMouseDown;
        }
        else if (HighlightAction)
        {
            MeshRenderer.material = MatMouseOver;
        }
        else
        {
            MeshRenderer.material = MatStandard;
        }
    }

    private void OnMouseDown()
    {
        OffsetAction = true;
    }

    private void OnMouseUp()
    {
        OffsetAction = false;
    }

    private void OnMouseEnter()
    {
        HighlightAction = true;
    }

    private void OnMouseExit()
    {
        HighlightAction = false;
    }

    private void OnDisable()
    {
        OffsetAction = false;
        HighlightAction = false;
    }
}
