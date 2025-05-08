using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    public GameObject menuPanel;
    public MonoBehaviour[] scriptsToDisable; // Drag your movement or camera scripts here

    private bool isMenuVisible = false;

    void Start()
    {
        menuPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isMenuVisible = !isMenuVisible;
            menuPanel.SetActive(isMenuVisible);

            Time.timeScale = isMenuVisible ? 0f : 1f;
            Cursor.lockState = isMenuVisible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isMenuVisible;

            foreach (var script in scriptsToDisable)
            {
                if (script != null)
                    script.enabled = !isMenuVisible;
            }
        }
    }
}