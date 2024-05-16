using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq; // Necesario para usar métodos de LINQ como .Average()

public class mqttController : MonoBehaviour
{
    //VARIABLES DEL SERVIDOR HTTP
    public Servidor nombreServidor;
    //VARIABLES DEL SERVIDOR MQTT
    public string nameController = "Controller 1";
    public string tagOfTheMQTTReceiver = "MQTT_ENTRADA";
    public mqttReceiver _eventSender;
    // DECLARACIÓN DE VARIABLES UI
    public Text textogiro;
    public GameObject arrow;
    public Button boton_cal;
    public Text textovelocidad;
    public Text textopendiente;
    public Text textotiempo;
    public GameObject BG_loading;
    // DECLARACIÓN DE VARIABLES PARA CÁLCULOS
    private float angulo_inicial = 0;
    private int conteo_angulo_inicial = 0;
    private int startpublish=10;
    private Queue<float> angulosRecientes = new Queue<float>(); // Cola para almacenar los últimos ángulos
    private int tamanoCola = 80; // Tamaño de la cola para el promedio móvil
    private float ultimoAnguloPublicado = float.NaN; // Almacena el último ángulo que fue publicado
    private string ultimaVelocidad = "";
    private string ultimaPendiente = "";
    

    //FUNCIÓN PRINCIPAL QUE SE EJECUTA SOLO UNA VEZ AL COMENZAR EL SCRIPT
    void Start()
    {
        GameObject[] receivers = GameObject.FindGameObjectsWithTag(tagOfTheMQTTReceiver);
        if (receivers.Length > 0)
        {
            _eventSender = receivers[0].GetComponent<mqttReceiver>();
            if (_eventSender != null)
            {
                _eventSender.OnVelocityMessageArrived += (msg) => OnMessageArrivedHandler("velocidad", msg);
                _eventSender.OnSteeringMessageArrived += (msg) => OnMessageArrivedHandler("pendiente", msg);
            }
            else
            {
                Debug.LogError("No se encontró el componente mqttReceiver en el objeto con tag '" + tagOfTheMQTTReceiver + "'.");
            }
        }
        else
        {
            Debug.LogError("No se encontró ningún objeto con el tag '" + tagOfTheMQTTReceiver + "'.");
        }

        Input.compass.enabled = true; // Habilita el sensor de campo magnético
        // Configura el botón de calibración para llamar a la función calibrar()
        boton_cal.onClick.AddListener(calibrar);
        BG_loading.SetActive(true);
        Invoke("DesactivarBG_loading", 2f);
        StartCoroutine(ContadorDeTiempo());
    }

    void Update()
    {
        // Obtiene el ángulo hacia el norte magnético
        float angle = Input.compass.magneticHeading; 
        // Tomar el primer valor del ángulo
        if(conteo_angulo_inicial == 0){
            angulo_inicial = angle;
            conteo_angulo_inicial++;
        } else {
            // Ajustar el ángulo relativo
            float anguloRelativo = Mathf.DeltaAngle(angulo_inicial, angle);
            if (angulosRecientes.Count >= tamanoCola)
        {
            angulosRecientes.Dequeue(); // Elimina el elemento más antiguo si alcanzamos el tamaño máximo
        }
        angulosRecientes.Enqueue(anguloRelativo);

        // Calcular el promedio de los ángulos en la cola
        float promedioAngulos = angulosRecientes.Average();

        // Actualizar el texto y la rotación con el valor promediado
        textogiro.text = "Giro: " + Mathf.RoundToInt(promedioAngulos).ToString() + "°";
        arrow.transform.rotation = Quaternion.Euler(0, 0, -promedioAngulos); // Asegúrate de ajustar la rotación correctamente
        if (_eventSender != null && Mathf.RoundToInt(promedioAngulos) != Mathf.RoundToInt(ultimoAnguloPublicado))
        {
            _eventSender.PublishCompassHeading("giroapp", Mathf.RoundToInt(promedioAngulos));
            ultimoAnguloPublicado = promedioAngulos; // Actualiza el último valor publicado
        }
        }
    }

    //Función para calibrar el sensor con un botón
    public void calibrar(){
        BG_loading.SetActive(true);
        Invoke("DesactivarBG_loading", 2f);
        // Reinicia el conteo de ángulos iniciales para tomar el actual como referencia
        conteo_angulo_inicial = 0;
        // Limpia la cola de ángulos recientes para empezar de nuevo con el nuevo ángulo inicial
    }
    
    //FUNCIÓN PARA SUSCRIBIRSE A LOS TÓPICOS MQTT

    private void OnMessageArrivedHandler(string topic, string message)
    {
        bool datosActualizados = false;
        if (topic == "velocidad" && message != ultimaVelocidad)
        {
            ultimaVelocidad = message;
            datosActualizados = true;
        }
        else if (topic == "pendiente" && message != ultimaPendiente)
        {
            ultimaPendiente = message;
            datosActualizados = true;
        }

        if (datosActualizados)
        {
            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Formato de fecha y hora
            string[] datos = new string[4] { UserManager.Instance.usuario,fechaActual, ultimaVelocidad, ultimaPendiente };
            StartCoroutine(nombreServidor.consumir_servicio("Publicar Valores", datos, ManejarRespuestaServidor));
        }
    }

    // ACCIÓN PARA EL SERVIDOR HTTP
    private void ManejarRespuestaServidor(Respuesta respuesta)
    {
        // Aquí procesas la respuesta del servidor
        Debug.Log("Respuesta recibida: ");
    }
    
    // FUNCIÓN COMPLEMENTARIA PARA MANTENER LA PANTALLA DURANTE 3 SEGUNDOS

    void DesactivarBG_loading()
    {
    BG_loading.SetActive(false);
    }

    //FUNCIÓN PARA SALIR DEL JUEGO DESPUÉS DE HABER PUBLICADO EN EL TÓPICO START EL VALOR DE 0 PARA SALIR DEL JUEGO EN OCULUS

    public void ExitGame()
    {
        int i=0;
        while(i<=startpublish){
            _eventSender.PublishStart("start", "0");
            i++;
        }
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    //FUNCIÓN QUE ACTUALIZA EL TXT TIEMPO COMO UN CONTADOR 
    IEnumerator ContadorDeTiempo()
    {
        int contador = 0; // Inicializa el contador a 0

        while (true) // Crea un bucle infinito
        {
            textotiempo.text = "Tiempo: " + contador.ToString(); // Actualiza el texto con el valor actual del contador
            yield return new WaitForSeconds(1); // Espera un segundo
            contador++; // Incrementa el contador después de cada segundo
        }
    }

}