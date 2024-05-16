using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class validacion_usuario : MonoBehaviour
{
    public Servidor nombreServidor;
    public InputField InpUsuario;
    public Text mensajeTexto; 
    public Button boton_sim;

    public void Validar()
    {
        string[] datos = new string[1] { InpUsuario.text };
        StartCoroutine(nombreServidor.consumir_servicio("verificar usuario", datos, OnServicioConsumido));
    }


    private void OnServicioConsumido(Respuesta respuesta)
    {
        
        if (respuesta.codigo == 202)
        {
            mensajeTexto.text = "Usuario ocupado";
            mensajeTexto.color = Color.red; // Cambia el color del texto a verde
            Invoke("LimpiarMensaje", 2); // Espera 2 segundos y luego limpia el mensaje
        
        }
        else if (respuesta.codigo == 203)
        {
            mensajeTexto.text = "Usuario Creado";
            mensajeTexto.color = Color.green; // Cambia el color del texto a verde
            //GUARDAR EL USUARIO PARA USARLO ENTRE ESCENAS
            UserManager.Instance.usuario = InpUsuario.text;
            //HABILITAR EL BOTÓN
            RectTransform rectTransform = boton_sim.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(57, rectTransform.anchoredPosition.y);
        }
        else
        {
            mensajeTexto.text = "Error al verificar el usuario";
            mensajeTexto.color = Color.black; // Cambia el color del texto a verde
        }
    }

    // Este método limpia el mensaje
    void LimpiarMensaje()
    {
        mensajeTexto.text = ""; // Limpia el mensaje
    }


}