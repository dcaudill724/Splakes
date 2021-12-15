using UnityEngine;

public class BackButtonController : MonoBehaviour
{
    public GameObject MainMenuContainer;
    public GameObject FindGameMenuContainer;

    public void GoBack()
    {
        FindGameMenuContainer.SetActive(false);
        MainMenuContainer.SetActive(true);
    }
}
