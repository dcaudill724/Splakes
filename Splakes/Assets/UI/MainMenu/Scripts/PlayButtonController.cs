using UnityEngine;

public class PlayButtonController : MonoBehaviour
{
    public GameObject PlayMenuContainer;
    public GameObject FindGameMenuContainer;

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PlayMenuContainer.SetActive(false);
            FindGameMenuContainer.SetActive(true);
        }
    }

}
