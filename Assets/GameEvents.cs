using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEvents : MonoBehaviour
{
    // Start is called before the first frame update
    public static GameEvents current;
    void Awake()
    {
        current = this;
    }

    public event System.Action onLightOff;
    public event System.Action<string> onLightOn;

    public void ApagarLuces()
    {
        onLightOff();
    }

    public void EncenderLuz(string nombre)
    {
        onLightOn(nombre);
    }

}
