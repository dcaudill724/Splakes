using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugLog : MonoBehaviour
{
    public TextMeshProUGUI DebugText;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowMessage(string message)
    {
        DebugText.text += message + "\n";
    }
}
