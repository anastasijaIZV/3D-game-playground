using UnityEngine;

public class CursorLocker : MonoBehaviour
{
    public GameObject menuPanel;

    void Start()
    {
        ApplyCursorState();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !menuPanel.activeSelf)
        {
            ApplyCursorState();
        }
    }

    void ApplyCursorState()
    {
        if (menuPanel.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // or CursorLockMode.Confined
            Cursor.visible = false;
        }
    }
}
