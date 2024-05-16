using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class login_user : MonoBehaviour
{
    public Servidor nombreServidor;
    public InputField InpUsuario;
    public Text mensajeTexto; // Asegúrate de tener esta referencia asignada en el Inspector de Unity
    public Button boton_sim;

    public void Login()
    {
        string[] datos = new string[1] { InpUsuario.text };
        StartCoroutine(nombreServidor.consumir_servicio("Login User", datos, OnServicioConsumido));
    }


        private void OnServicioConsumido(Respuesta respuesta)
    {
        
        if (respuesta.codigo == 404)
        {
            mensajeTexto.text = "Inicio de sesión exitoso";
            mensajeTexto.color = Color.green; // Cambia el color del texto a verde
            //ALMACENAR EL USUARIO PARA USARLO ENTRE ESCENAS
            UserManager.Instance.usuario = InpUsuario.text;
            //HABILITAR EL BOTÓN
            RectTransform rectTransform = boton_sim.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(57, rectTransform.anchoredPosition.y);
        }
        else if (respuesta.codigo == 204)
        {
            mensajeTexto.text = "Usuario no existente";
            mensajeTexto.color = Color.red; // Cambia el color del texto a verde
            Invoke("LimpiarMensaje", 2); // Espera 2 segundos y luego limpia el mensaje
            
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
