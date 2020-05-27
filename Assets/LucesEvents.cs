using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LucesEvents : MonoBehaviour
{
    // Start is called before the first frame update
    Light luz;
    void Start()
    {
        GameEvents.current.onLightOff += Apagar;
        GameEvents.current.onLightOn += Encender;

        luz = GetComponent<Light>();
    }

    void Apagar()
    {
        luz.enabled = false;
    }

    void Encender(string nombre)
    {
        
       
        if (transform.name == nombre)
        {
            //GameEvents.current.ApagarLuces();
            luz.enabled = !luz.enabled;
        }
        else
        {
            luz.enabled = false;
            
        }
        
    }
}
