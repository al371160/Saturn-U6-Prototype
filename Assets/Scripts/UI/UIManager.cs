using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject[] objs;

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ClearUI() 
    {
        for (int i=0; i < objs.Length; i++) {
            GameObject obj = objs[i];
            obj.SetActive(false);
            Debug.Log(obj.name + "is now inactive.");
        }
    }
}
