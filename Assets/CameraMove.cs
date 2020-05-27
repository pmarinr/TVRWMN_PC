using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using HutongGames.PlayMaker.Actions;

public class CameraMove : MonoBehaviour
{
    public Transform[] PositionList;
    public int pos = 0;
   
    // Start is called before the first frame update
    void Start()
    {
        this.transform.DOMove(PositionList[pos].position, 1);
        this.transform.DORotate(PositionList[pos].rotation.eulerAngles, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            nextCam();

        }
    }

    public void nextCam()
    {
        pos = (pos >= PositionList.Length-1) ? 0 : pos + 1;
        this.transform.DOMove(PositionList[pos].position, 1);
        this.transform.DORotate(PositionList[pos].rotation.eulerAngles, 1);
        
    }
}
