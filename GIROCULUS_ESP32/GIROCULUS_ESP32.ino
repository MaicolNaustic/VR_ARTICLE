#include <Arduino.h>
#include <WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <MechaQMC5883.h>    // incluye librería para magnetómetro QMC5883L

WiFiClient esp32Client;
PubSubClient mqttClient(esp32Client);
const char* ssid = "TENA DIGITAL";
const char* password ="guayusaycanela";
const char* server = "0.tcp.sa.ngrok.io"; // Cambiar URL SEGÚN LAN
int port = 11103;
const unsigned long TIMEOUT = 2500;
unsigned long startTime = 0;
unsigned long lastTimeSent = 0;
unsigned long lastDataReceivedTime = 0;
bool contadorIniciado = false;
int ctiempo = 0;
const int sensorPin = 32;
const float radioLlanta = 0.21;
const float pi = 3.1416;
int previousState = HIGH;
unsigned long previousTime = 0;
int cont = 0;
float velocidadAngular = 0;
unsigned long tiempoOFF = 0;
bool velocidadCeroEnviada = false;
int data;
const int pulPin = 25;
const int dirPin = 14;
const int enPin = 26;
String pendientee;
int pendiente;
int pendiente_anterior = 0;
int pasos;
//variables para conteo de tiempo
unsigned long tiempoInicio = 0;
int tiempoContador = 0;
bool timesendzero = false;
bool pendientesendzero=false;


//VARIABLES PARA EL GIRO DEL MANUBRIO
MechaQMC5883 qmc;        // crea objeto

float acimutReferencia = -1; // Variable para guardar la orientación de referencia

float filteredAngle = 0;
const int filterWindowSize = 5; // Tamaño de la ventana del filtro
float angleValues[filterWindowSize];
int currentIndex = 0;


void wifiInit() {
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    Serial.print(".");
    delay(500);
  }
}

void callback(char* topic, byte* payload, unsigned int length) {
  String messageTemp;
  for (int i = 0; i < length; i++) {
    messageTemp += (char)payload[i];
  }

  if (String(topic) == "inclinación") {
    float pendiente = messageTemp.toFloat() * -1;
    if (pendiente < -30 || pendiente > 45) {
      // Valores fuera de rango, manejar el error
    } else {
      if (pendiente != pendiente_anterior) {
        if (pendiente > pendiente_anterior) {
          digitalWrite(dirPin, HIGH);
        } else {
          digitalWrite(dirPin, LOW);
        }
        pasos = map(abs(pendiente - pendiente_anterior), 0, 30, 0, 2000);
        for (int x = 0; x < pasos; x++) {
          digitalWrite(pulPin, HIGH);
          delayMicroseconds(125);
          digitalWrite(pulPin, LOW);
          delayMicroseconds(125);
        }
        pendiente_anterior = pendiente;
        String datos = String(pendiente);
        mqttClient.publish("pendiente", datos.c_str());
      }
    }
  }
}

void reconnect() {
  while (!mqttClient.connected()) {
    if (mqttClient.connect("arduinoClient")) {
      mqttClient.subscribe("inclinación");
    } else {
      delay(5000);
    }
  }
}

void updateFilter(float newValue) {
  angleValues[currentIndex] = newValue;
  currentIndex = (currentIndex + 1) % filterWindowSize;

  // Calcula el promedio de los valores en la ventana del filtro
  float sum = 0;
  for (int i = 0; i < filterWindowSize; i++) {
    sum += angleValues[i];
  }
  filteredAngle = sum / filterWindowSize;
}

void setup() {
  pinMode(sensorPin, INPUT_PULLUP);
  pinMode(pulPin, OUTPUT);
  pinMode(dirPin, OUTPUT);
  pinMode(enPin, OUTPUT);
  digitalWrite(enPin, LOW);
  Serial.begin(115200);
  wifiInit();
  mqttClient.setServer(server, port);
  mqttClient.setCallback(callback);
  mqttClient.subscribe("inclinación");
  Wire.begin();            // inicializa bus I2C
  qmc.init();            // inicializa objeto
  int x, y, z;
  qmc.read(&x, &y, &z, &acimutReferencia); // Lee la orientación actual y la establece como 
}

void loop() {

//////////////////////////////////////////////////
  int x, y, z;
  float acimutActual;
  qmc.read(&x, &y, &z, &acimutActual);

  float anguloGiro = acimutActual - acimutReferencia;
  if (anguloGiro > 180) {
    anguloGiro -= 360;
  } else if (anguloGiro < -180) {
    anguloGiro += 360;
  }

  updateFilter(anguloGiro);
  
  // Aplica el filtro de media móvil y anula las perturbaciones menores a ±1
  if (filteredAngle > -3 && filteredAngle < 3) {
    filteredAngle = 0;
  }

  Serial.println(int(filteredAngle*0.5)); // muestra el valor de ángulo de giro suavizado
  
  // Publica el ángulo de giro suavizado en el tópico "giro" MQTT
  mqttClient.publish("giro", String(int(filteredAngle)*-3).c_str());

/////////////////////////////////////////////////////////////////////

  if (!mqttClient.connected()) {
    reconnect();
  }
  mqttClient.loop();

  int sensorState = digitalRead(sensorPin);
  unsigned long currentTime = millis();
  if (sensorState != previousState) {
    if (sensorState == LOW) {
      cont++;
      unsigned long timeDiff = currentTime - previousTime;
      velocidadAngular = 60000.0 / timeDiff / 60.0 * 2 * pi;
      if (velocidadAngular <= 50) {
        Serial.println(velocidadAngular);
        mqttClient.publish("velocidad", String(velocidadAngular).c_str());
        lastTimeSent = currentTime;
        velocidadCeroEnviada = false;
      }
    }
    previousTime = currentTime;
  }

  if (sensorState == HIGH && !velocidadCeroEnviada && currentTime - lastTimeSent > TIMEOUT) {
    Serial.println(0);
    mqttClient.publish("velocidad", "0");
    velocidadCeroEnviada = true;
  }

  previousState = sensorState;
  delay(10);

  //tiempo
  if (velocidadAngular != 0 && !contadorIniciado) {
  tiempoInicio = millis(); // Guarda el tiempo de inicio
  contadorIniciado = true; // Indica que el contador ha iniciado
}

if (contadorIniciado) {
  unsigned long tiempoActual = millis();
  if (tiempoActual - tiempoInicio >= 1000) {
    tiempoContador++; // Incrementa el contador cada segundo
    mqttClient.publish("tiempo", String(tiempoContador).c_str()); // Publica el tiempo en el tópico MQTT
    tiempoInicio = tiempoActual; // Reinicia el tiempo de inicio
  }
}

//setear el tiempo en cero una vez
if(timesendzero == false){
  tiempoContador=0;
  mqttClient.publish("tiempo", String(tiempoContador).c_str()); // Publica el tiempo en el tópico MQTT
  timesendzero = true;
}
//setear la pendiente en cero una vez
if(pendientesendzero == false){
  int primerapendienteenviada = 0;
  mqttClient.publish("pendiente", String(primerapendienteenviada).c_str()); // Publica el tiempo en el tópico MQTT
  pendientesendzero = true;
}

}
