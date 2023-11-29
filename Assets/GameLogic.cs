using UnityEngine;

public class GameLogic : MonoBehaviour
{
    private static GameLogic _singleton;

    public static GameLogic Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(GameLogic)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;

    public GameObject PlayerPrefab => playerPrefab;

    private void Awake()
    {
        Singleton = this;
    }
}
