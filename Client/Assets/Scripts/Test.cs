using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    int count = 0;
    int num = 0;
    // Start is called before the first frame update
    void Start()
    {
       var time= Scheduler.Instance.Repeat(1, () => {
            Helper.Log("Count:" + count++);
        });
        Scheduler.Instance.Repeat(2, () => {
            Helper.Log("num:" + num++);
        });
        Scheduler.Instance.Wait(2, () => { Helper.Log("延迟2s"); });
        Scheduler.Instance.Wait(15, () => {
            Scheduler.Instance.Stop(time);
        });
        Scheduler.Instance.Wait(20, () => {
            Helper.Log("定时器重启：");
            Scheduler.Instance.ReStart(time);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
