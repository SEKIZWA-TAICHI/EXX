using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WwisePost : MonoBehaviour
{
    public AK.Wwise.Event Event;//宣言

    void Start()
    {
        Event.Post(gameObject);//Eventの再生
    }
}
