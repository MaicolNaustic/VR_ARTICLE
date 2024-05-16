using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class cambiar_escena : MonoBehaviour
{
    public string nameController = "Controller 1";
    public string tagOfTheMQTTReceiver = "MQTT_ENTRADA";
    public mqttReceiver _eventSender;

    void Start()
    {
        GameObject[] receivers = GameObject.FindGameObjectsWithTag(tagOfTheMQTTReceiver);
        if (receivers.Length > 0)
        {
            _eventSender = receivers[0].GetComponent<mqttReceiver>();
            if (_eventSender != null)
            {
                _eventSender.OnVelocityMessageArrived += (msg) => OnMessageArrivedHandler("velocidad", msg);
                _eventSender.OnSteeringMessageArrived += (msg) => OnMessageArrivedHandler("giro", msg);
                //_eventSender.OnStartMessageArrived    += (msg) => OnMessageArrivedHandler("start", msg);
            }

        }
    }

public void CambiarEscena(string nombre)
{
    SceneManager.LoadScene(nombre);
    // Suponiendo que quieres publicar cuando cambias de escena
    _eventSender.PublishStart("start", "1");
}


private void OnMessageArrivedHandler(string topic, string message)
{
    Debug.Log($"Mensaje recibido. Tópico: {topic}, Mensaje: {message}");
    // Aquí puedes agregar la lógica específica que desees ejecutar cuando llegue un mensaje.
}


}