using UnityEngine;

public class Wall : MonoBehaviour
{
    private void Start()
    {
        GridManager.Instance.Register(gameObject);
    }
}
