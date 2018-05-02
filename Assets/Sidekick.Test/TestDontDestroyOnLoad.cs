using UnityEngine;

#pragma warning disable 0414

public class TestDontDestroyOnLoad : MonoBehaviour
{
    [SerializeField]
    float testFloat = 123.45f;

	void Start()
	{
        DontDestroyOnLoad(this.gameObject);
	}
}
