using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exit : MonoBehaviour
{
    public GameObject PanelSalir;
    bool active = false;
    // Start is called before the first frame update
    void Start()
    {
        PanelSalir.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        active = !active;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
           PanelSalir.SetActive(active);
        }    
    }


    public void Cancel()
    {
        PanelSalir.SetActive(false);
    }

    public void Salir()
    {
        Application.Quit();
    }


}
