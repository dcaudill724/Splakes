using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayButtonController : MonoBehaviour
{
    public GameObject MainMenuContainer;
    public GameObject PlayGameMenuContainer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            MainMenuContainer.SetActive(false);
            PlayGameMenuContainer.SetActive(true);
        }
    }
}
