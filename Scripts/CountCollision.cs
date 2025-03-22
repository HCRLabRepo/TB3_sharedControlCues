using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("wall") || collision.gameObject.CompareTag("obstacle"))
        {
            DataManager.Instance.collisionCount++;
            Debug.Log("Collision detected" ); 
        }
    }
}
