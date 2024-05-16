using UnityEngine;

public class UserManager : MonoBehaviour
{
    public static UserManager Instance; // Referencia estática a sí misma

    public string usuario; // Variable para almacenar el nombre del usuario

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Evita que se destruya al cambiar de escena
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Si ya existe una instancia, destruye este objeto
        }
    }
}