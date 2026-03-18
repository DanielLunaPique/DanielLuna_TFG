using UnityEngine;

public class AgujeroBala : MonoBehaviour
{
    private float timer = 15f; 

    // Update is called once per frame
    void Update()
    {
        if(timer > 0)
        {
            timer -= Time.deltaTime;
        }

        else
        {
            Destroy(gameObject);
        }
    }
}
